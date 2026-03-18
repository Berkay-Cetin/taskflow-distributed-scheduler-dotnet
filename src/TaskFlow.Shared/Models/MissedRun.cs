namespace TaskFlow.Shared.Models;

// Atlanılan trigger'ları kaydet — sonra çalıştır
public class MissedRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = default!;
    public DateTime ScheduledAt { get; set; }  // ne zaman çalışması gerekiyordu
    public string Reason { get; set; } = default!;  // "MaxConcurrent" | "Locked"
    public bool Recovered { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RecoveredAt { get; set; }

    public ScheduledTask? Task { get; set; }
}