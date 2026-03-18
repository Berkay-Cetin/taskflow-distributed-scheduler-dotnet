namespace TaskFlow.Shared.Models;

public class ExecutionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string Message { get; set; } = default!;
    public string? Details { get; set; }  // response body, hata stack trace vb.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TaskExecution? Execution { get; set; }
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug
}