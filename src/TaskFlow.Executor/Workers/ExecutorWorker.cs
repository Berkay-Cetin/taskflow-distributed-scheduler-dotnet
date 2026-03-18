using Confluent.Kafka;
using MassTransit;
using TaskFlow.Executor.Services;
using TaskFlow.Shared.Messages;
using System.Text.Json;

namespace TaskFlow.Executor.Workers;

public class ExecutorWorker : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly WebhookInvoker _invoker;
    private readonly KafkaResultPublisher _resultPublisher;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExecutorWorker> _logger;

    public ExecutorWorker(
        IConfiguration config,
        WebhookInvoker invoker,
        KafkaResultPublisher resultPublisher,
        IServiceScopeFactory scopeFactory,
        ILogger<ExecutorWorker> logger)
    {
        _invoker = invoker;
        _resultPublisher = resultPublisher;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = config["Kafka:ConsumerGroup"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        }).Build();

        _consumer.Subscribe(config["Kafka:Topics:TaskTrigger"]);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecutorWorker başladı");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (result is null) continue;

                var trigger = JsonSerializer.Deserialize<TaskTriggerMessage>(result.Message.Value);
                if (trigger is not null)
                    await ProcessTriggerAsync(trigger);

                _consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecutorWorker hatası");
            }
        }

        _consumer.Close();
    }

    private async Task ProcessTriggerAsync(TaskTriggerMessage trigger)
    {
        _logger.LogInformation("[EXECUTE] {TaskName} | Attempt: {Attempt}",
            trigger.TaskName, trigger.AttemptNo);

        var result = await _invoker.InvokeAsync(trigger);
        await _resultPublisher.PublishAsync(result);

        if (!result.IsSuccess && result.WillRetry)
        {
            await Task.Delay(TimeSpan.FromSeconds(trigger.RetryDelay));

            using var scope = _scopeFactory.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await bus.Publish(new RetryMessage
            {
                TaskId = trigger.TaskId,
                ExecutionId = trigger.ExecutionId ?? Guid.NewGuid(),
                TaskName = trigger.TaskName,
                WebhookUrl = trigger.WebhookUrl,
                HttpMethod = trigger.HttpMethod,
                Headers = trigger.Headers,
                Body = trigger.Body,
                AttemptNo = trigger.AttemptNo + 1,
                MaxRetry = trigger.RetryCount,
                DelaySeconds = trigger.RetryDelay,
                ErrorMessage = result.ErrorMessage ?? "Unknown error",
            });

            _logger.LogWarning("[RETRY QUEUED] {TaskName} → Attempt {Next}/{Max}",
                trigger.TaskName, trigger.AttemptNo + 1, trigger.RetryCount);
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}