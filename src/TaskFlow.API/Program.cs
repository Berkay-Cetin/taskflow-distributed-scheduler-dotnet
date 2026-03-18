using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using TaskFlow.API.Data;
using TaskFlow.API.Hubs;
using TaskFlow.API.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

// PostgreSQL
builder.Services.AddDbContext<TaskFlowDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Services
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<ScheduleCalculator>();
builder.Services.AddHostedService<KafkaResultConsumer>();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(opts => opts.AddPolicy("react", p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:5200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DB oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Urls.Add("http://localhost:5200");
app.UseCors("react");
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHub<TaskHub>("/hubs/tasks");

app.Run();