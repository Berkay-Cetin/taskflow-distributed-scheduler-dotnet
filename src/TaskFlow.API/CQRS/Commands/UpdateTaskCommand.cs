using MediatR;
using TaskFlow.API.Data;
using TaskFlow.API.Services;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Commands;

public record UpdateTaskCommand : IRequest<ScheduledTask?>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string WebhookUrl { get; init; } = default!;
    public string HttpMethod { get; init; } = "POST";
    public string? WebhookHeaders { get; init; }
    public string? WebhookBody { get; init; }
    public string? CronExpression { get; init; }
    public int? IntervalMinutes { get; init; }
    public int RetryCount { get; init; }
    public int RetryDelaySeconds { get; init; }
    public int TimeoutSeconds { get; init; }
}

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ScheduledTask?>
{
    private readonly TaskFlowDbContext _db;
    private readonly ScheduleCalculator _calculator;

    public UpdateTaskCommandHandler(TaskFlowDbContext db, ScheduleCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    public async Task<ScheduledTask?> Handle(UpdateTaskCommand cmd, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([cmd.Id], ct);
        if (task is null) return null;

        task.Name = cmd.Name;
        task.Description = cmd.Description;
        task.WebhookUrl = cmd.WebhookUrl;
        task.HttpMethod = cmd.HttpMethod;
        task.WebhookHeaders = cmd.WebhookHeaders;
        task.WebhookBody = cmd.WebhookBody;
        task.CronExpression = cmd.CronExpression;
        task.IntervalMinutes = cmd.IntervalMinutes;
        task.RetryCount = cmd.RetryCount;
        task.RetryDelaySeconds = cmd.RetryDelaySeconds;
        task.TimeoutSeconds = cmd.TimeoutSeconds;
        task.NextRunAt = _calculator.GetNextRun(task.ScheduleType, cmd.CronExpression, cmd.IntervalMinutes);
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return task;
    }
}