using MassTransit;
using Microsoft.Extensions.Hosting;
using Serilog;
using TaskFlow.Executor.Consumers;
using TaskFlow.Executor.Services;
using TaskFlow.Executor.Workers;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureServices((ctx, services) =>
    {
        // HTTP Client
        services.AddHttpClient();

        // Services
        services.AddSingleton<WebhookInvoker>();
        services.AddSingleton<KafkaResultPublisher>();

        // MassTransit + RabbitMQ
        services.AddMassTransit(x =>
        {
            x.AddConsumer<RetryConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(ctx.Configuration["RabbitMQ:Host"], ushort.Parse(ctx.Configuration["RabbitMQ:Port"]!), "/", h =>
                {
                    h.Username(ctx.Configuration["RabbitMQ:Username"]!);
                    h.Password(ctx.Configuration["RabbitMQ:Password"]!);
                });

                cfg.ReceiveEndpoint("taskflow.retry", e =>
                {
                    e.ConfigureConsumer<RetryConsumer>(context);

                    // MassTransit retry policy
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30)));
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // Workers
        services.AddHostedService<ExecutorWorker>();
    })
    .Build();

await host.RunAsync();