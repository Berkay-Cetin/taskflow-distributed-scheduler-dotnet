namespace TaskFlow.Shared.Models;

public class AlertRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string Name { get; set; } = default!;
    public AlertTriggerType TriggerType { get; set; }
    public int Threshold { get; set; } = 3;   // kaç başarısızlıktan sonra
    public string WebhookUrl { get; set; } = default!;
    public string? WebhookTemplate { get; set; }        // JSON template
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ScheduledTask? Task { get; set; }
}

public enum AlertTriggerType
{
    ConsecutiveFailures,  // art arda X başarısız
    AnyFailure,           // her başarısız olduğunda
    TaskDead,             // max retry aşıldığında
}