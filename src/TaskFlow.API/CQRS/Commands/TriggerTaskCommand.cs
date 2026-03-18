using MediatR;
using TaskFlow.API.Data;
using TaskFlow.API.Services;

namespace TaskFlow.API.CQRS.Commands;

public record TriggerTaskCommand(Guid Id) : IRequest<bool>;

public class TriggerTaskCommandHandler : IRequestHandler<TriggerTaskCommand, bool>
{
    private readonly TaskFlowDbContext _db;
    private readonly KafkaProducerService _kafka;
    private readonly AuditService _audit;
    private readonly ILogger<TriggerTaskCommandHandler> _logger;

    public TriggerTaskCommandHandler(
        TaskFlowDbContext db,
        KafkaProducerService kafka,
        AuditService audit,
        ILogger<TriggerTaskCommandHandler> logger)
    {
        _db = db;
        _kafka = kafka;
        _audit = audit;
        _logger = logger;
    }

    public async Task<bool> Handle(TriggerTaskCommand cmd, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([cmd.Id], ct);
        if (task is null || !task.IsEnabled) return false;

        await _kafka.PublishTriggerAsync(task);
        await _audit.LogAsync("TaskManuallyTriggered", "Task", task.Id, task.Name);
        _logger.LogInformation("[MANUAL TRIGGER] {Name}", task.Name);
        return true;
    }
}