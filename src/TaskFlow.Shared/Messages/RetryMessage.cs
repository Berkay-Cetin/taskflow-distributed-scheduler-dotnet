namespace TaskFlow.Shared.Messages;

// RabbitMQ retry queue mesajı
public record RetryMessage
{
    public Guid TaskId { get; init; }
    public Guid ExecutionId { get; init; }
    public string TaskName { get; init; } = default!;
    public string WebhookUrl { get; init; } = default!;
    public string HttpMethod { get; init; } = "POST";
    public string? Headers { get; init; }
    public string? Body { get; init; }
    public int AttemptNo { get; init; }
    public int MaxRetry { get; init; }
    public int DelaySeconds { get; init; }
    public string ErrorMessage { get; init; } = default!;
}