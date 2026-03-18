namespace TaskFlow.Shared.Models;

public class ScheduledTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    // Schedule
    public ScheduleType ScheduleType { get; set; }
    public string? CronExpression { get; set; }
    public int? IntervalMinutes { get; set; }

    // Webhook
    public string WebhookUrl { get; set; } = default!;
    public string HttpMethod { get; set; } = "POST";
    public string? WebhookHeaders { get; set; }
    public string? WebhookBody { get; set; }

    // Retry
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxConcurrent { get; set; } = 1;   // task başına max paralel execution
    public int Priority { get; set; } = 5;   // 1=en yüksek, 10=en düşük
    public bool AllowMissedRuns { get; set; } = true; // atlanılan run'ları kaydet

    // State
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public TaskStatus LastStatus { get; set; } = TaskStatus.Idle;
    public int ActiveExecutions { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum ScheduleType
{
    Cron,
    Interval,
    Manual
}

public enum TaskStatus
{
    Idle,
    Running,
    Success,
    Failed,
    Disabled,
    Queued
}