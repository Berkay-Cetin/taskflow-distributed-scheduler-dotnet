using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using StackExchange.Redis;
using TaskFlow.Scheduler.Data;
using TaskFlow.Scheduler.Services;
using TaskFlow.Scheduler.Workers;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureServices((ctx, services) =>
    {
        // PostgreSQL
        services.AddDbContext<SchedulerDbContext>(opts =>
            opts.UseNpgsql(ctx.Configuration.GetConnectionString("Postgres")));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(ctx.Configuration["Redis:ConnectionString"]!));

        // Services
        services.AddSingleton<KafkaProducerService>();
        services.AddSingleton<RedisLockService>();
        services.AddHostedService<SchedulerWorker>();
    })
    .Build();

await host.RunAsync();