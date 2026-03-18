namespace TaskFlow.Shared.Messages;

public record TaskResultMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public Guid TaskId { get; init; }
    public Guid ExecutionId { get; init; }
    public bool IsSuccess { get; init; }
    public int? StatusCode { get; init; }
    public string? Response { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorDetails { get; init; }
    public long DurationMs { get; init; }
    public int AttemptNo { get; init; }
    public bool WillRetry { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}