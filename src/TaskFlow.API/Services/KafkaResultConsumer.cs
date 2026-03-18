using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.API.Hubs;
using TaskFlow.Shared.Messages;
using TaskFlow.Shared.Models;
using System.Text.Json;

namespace TaskFlow.API.Services;

public class KafkaResultConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TaskHub> _hub;
    private readonly ILogger<KafkaResultConsumer> _logger;

    public KafkaResultConsumer(
        IConfiguration config,
        IServiceScopeFactory scopeFactory,
        IHubContext<TaskHub> hub,
        ILogger<KafkaResultConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;

        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "taskflow-api-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        }).Build();

        _consumer.Subscribe(config["Kafka:Topics:TaskResult"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uygulama tamamen başlayana kadar bekle
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("KafkaResultConsumer başladı");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (result is null) continue;

                var msg = JsonSerializer.Deserialize<TaskResultMessage>(result.Message.Value);
                if (msg is null) continue;

                await ProcessResultAsync(msg);
                _consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Result consumer hatası");
            }
        }

        _consumer.Close();
    }

    private async Task ProcessResultAsync(TaskResultMessage msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();

        // Execution güncelle
        var execution = await db.Executions.FindAsync(msg.ExecutionId);
        if (execution is not null)
        {
            execution.Status = msg.IsSuccess ? ExecutionStatus.Success :
                                    msg.WillRetry ? ExecutionStatus.Retrying : ExecutionStatus.Dead;
            execution.StatusCode = msg.StatusCode;
            execution.Response = msg.Response;
            execution.ErrorMessage = msg.ErrorMessage;
            execution.DurationMs = msg.DurationMs;
            execution.FinishedAt = DateTime.UtcNow;
        }

        // Task güncelle
        var task = await db.Tasks.FindAsync(msg.TaskId);
        if (task is not null)
        {
            task.LastRunAt = DateTime.UtcNow;
            task.ActiveExecutions = Math.Max(0, task.ActiveExecutions - 1);
            task.LastStatus = msg.IsSuccess ? TaskFlow.Shared.Models.TaskStatus.Success :
                                    msg.WillRetry ? TaskFlow.Shared.Models.TaskStatus.Running :
                                                   TaskFlow.Shared.Models.TaskStatus.Failed;
        }

        await db.SaveChangesAsync();

        // SignalR ile dashboard'a push et
        var groupName = $"task-{msg.TaskId}";
        await _hub.Clients.Group(groupName).SendAsync("ExecutionCompleted", new
        {
            msg.TaskId,
            msg.ExecutionId,
            msg.IsSuccess,
            msg.DurationMs,
            msg.ErrorMessage,
            msg.AttemptNo,
            msg.WillRetry,
            CompletedAt = DateTime.UtcNow
        });

        _logger.LogInformation("[RESULT] {TaskId} → {Status} | {Duration}ms",
            msg.TaskId, msg.IsSuccess ? "SUCCESS" : "FAILED", msg.DurationMs);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}