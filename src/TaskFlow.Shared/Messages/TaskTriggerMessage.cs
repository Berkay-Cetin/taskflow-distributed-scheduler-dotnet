namespace TaskFlow.Shared.Messages;

public record TaskTriggerMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid TaskId { get; init; }
    public string TaskName { get; init; } = default!;
    public string WebhookUrl { get; init; } = default!;
    public string HttpMethod { get; init; } = "POST";
    public string? Headers { get; init; }
    public string? Body { get; init; }
    public int RetryCount { get; init; }
    public int RetryDelay { get; init; }
    public int Timeout { get; init; }
    public int AttemptNo { get; init; } = 1;
    public Guid? ExecutionId { get; init; }
    public int Priority { get; init; } = 5;
    public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
}