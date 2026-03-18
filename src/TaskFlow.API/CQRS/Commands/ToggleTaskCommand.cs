using MediatR;
using TaskFlow.API.Data;
using TaskFlow.API.Services;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Commands;

public record ToggleTaskCommand(Guid Id, bool Enable) : IRequest<ScheduledTask?>;

public class ToggleTaskCommandHandler : IRequestHandler<ToggleTaskCommand, ScheduledTask?>
{
    private readonly TaskFlowDbContext _db;
    private readonly AuditService _audit;

    public ToggleTaskCommandHandler(TaskFlowDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<ScheduledTask?> Handle(ToggleTaskCommand cmd, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([cmd.Id], ct);
        if (task is null) return null;

        var oldStatus = task.IsEnabled;

        task.IsEnabled = cmd.Enable;
        task.LastStatus = cmd.Enable
            ? TaskFlow.Shared.Models.TaskStatus.Idle
            : TaskFlow.Shared.Models.TaskStatus.Disabled;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            cmd.Enable ? "TaskEnabled" : "TaskDisabled",
            "Task", task.Id, task.Name,
            oldValue: new { IsEnabled = oldStatus },
            newValue: new { IsEnabled = cmd.Enable });

        return task;
    }
}