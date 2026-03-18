namespace TaskFlow.Shared.Models;

public class TaskExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = default!;

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;
    public int AttemptNo { get; set; } = 1;  // kaçıncı deneme
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }  // HTTP response code
    public string? Response { get; set; }  // webhook response body
    public long DurationMs { get; set; }  // çalışma süresi

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    // Navigation
    public ScheduledTask? Task { get; set; }
}

public enum ExecutionStatus
{
    Running,
    Success,
    Failed,
    Retrying,
    Dead        // max retry aşıldı
}