namespace TaskFlow.Shared.Models;

public class TaskTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Color { get; set; } = "#3b82f6";  // hex renk kodu
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ScheduledTaskTag> TaskTags { get; set; } = new List<ScheduledTaskTag>();
}

// Many-to-many join tablosu
public class ScheduledTaskTag
{
    public Guid TaskId { get; set; }
    public Guid TagId { get; set; }

    public ScheduledTask? Task { get; set; }
    public TaskTag? Tag { get; set; }
}