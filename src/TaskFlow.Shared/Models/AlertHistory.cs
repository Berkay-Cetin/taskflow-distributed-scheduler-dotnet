namespace TaskFlow.Shared.Models;

public class AlertHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AlertRuleId { get; set; }
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool Delivered { get; set; } = false;
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AlertRule? AlertRule { get; set; }
}