using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using TaskFlow.API.Data;
using TaskFlow.API.Hubs;
using TaskFlow.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();

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

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Postgres")!,
        name: "postgres",
        tags: new[] { "db", "ready" })
    .AddRedis(
        builder.Configuration["Redis:ConnectionString"]!,
        name: "redis",
        tags: new[] { "cache", "ready" })
    .AddKafka(
        new Confluent.Kafka.ProducerConfig
        {
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
            MessageTimeoutMs = 5000,
        },
        topic: "task.trigger",
        name: "kafka",
        tags: new[] { "messaging", "ready" });

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // SignalR için token query string'den oku
        opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("OperatorPlus", p => p.RequireRole("Admin", "Operator"));
});

builder.Services.AddSingleton<JwtService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// DB oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Urls.Add("http://localhost:5200");
app.UseCors("react");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checked_at = DateTime.UtcNow,
            services = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds + "ms",
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false  // Sadece uygulama ayakta mı? DB kontrolü yok
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();
app.MapHub<TaskHub>("/hubs/tasks");

app.Run();