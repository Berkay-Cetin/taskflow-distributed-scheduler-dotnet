using MassTransit;
using TaskFlow.Executor.Services;
using TaskFlow.Shared.Messages;

namespace TaskFlow.Executor.Consumers;

public class RetryConsumer : IConsumer<RetryMessage>
{
    private readonly WebhookInvoker _invoker;
    private readonly KafkaResultPublisher _resultPublisher;
    private readonly ILogger<RetryConsumer> _logger;

    public RetryConsumer(
        WebhookInvoker invoker,
        KafkaResultPublisher resultPublisher,
        ILogger<RetryConsumer> logger)
    {
        _invoker = invoker;
        _resultPublisher = resultPublisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RetryMessage> context)
    {
        var msg = context.Message;

        _logger.LogInformation("[RETRY] {TaskName} — Attempt {Attempt}/{Max}",
            msg.TaskName, msg.AttemptNo, msg.MaxRetry);

        var result = await _invoker.InvokeAsync(new TaskFlow.Shared.Messages.TaskTriggerMessage
        {
            TaskId = msg.TaskId,
            TaskName = msg.TaskName,
            WebhookUrl = msg.WebhookUrl,
            HttpMethod = msg.HttpMethod,
            Headers = msg.Headers,
            Body = msg.Body,
            RetryCount = msg.MaxRetry,
            RetryDelay = msg.DelaySeconds,
            AttemptNo = msg.AttemptNo,
            ExecutionId = msg.ExecutionId,
        });

        await _resultPublisher.PublishAsync(result);
    }
}