namespace TaskFlow.Shared.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = default!;  // "TaskCreated", "TaskTriggered" vb.
    public string EntityType { get; set; } = default!;  // "Task", "Tag"
    public Guid? EntityId { get; set; }
    public string EntityName { get; set; } = default!;
    public string? OldValue { get; set; }  // JSON — önceki değer
    public string? NewValue { get; set; }  // JSON — yeni değer
    public string? UserId { get; set; }  // Authentication eklenince dolacak
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}