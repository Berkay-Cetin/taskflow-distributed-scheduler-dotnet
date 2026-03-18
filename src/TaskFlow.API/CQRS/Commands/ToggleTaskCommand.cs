using MediatR;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Commands;

public record ToggleTaskCommand(Guid Id, bool Enable) : IRequest<ScheduledTask?>;

public class ToggleTaskCommandHandler : IRequestHandler<ToggleTaskCommand, ScheduledTask?>
{
    private readonly TaskFlowDbContext _db;

    public ToggleTaskCommandHandler(TaskFlowDbContext db) => _db = db;

    public async Task<ScheduledTask?> Handle(ToggleTaskCommand cmd, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([cmd.Id], ct);
        if (task is null) return null;

        task.IsEnabled = cmd.Enable;
        task.LastStatus = cmd.Enable ? TaskFlow.Shared.Models.TaskStatus.Idle : TaskFlow.Shared.Models.TaskStatus.Disabled;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return task;
    }
}