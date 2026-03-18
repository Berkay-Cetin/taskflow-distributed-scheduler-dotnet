using MediatR;
using TaskFlow.API.Data;
using TaskFlow.API.Services;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Commands;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, ScheduledTask>
{
    private readonly TaskFlowDbContext _db;
    private readonly ScheduleCalculator _calculator;
    private readonly ILogger<CreateTaskCommandHandler> _logger;

    public CreateTaskCommandHandler(
        TaskFlowDbContext db,
        ScheduleCalculator calculator,
        ILogger<CreateTaskCommandHandler> logger)
    {
        _db = db;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task<ScheduledTask> Handle(CreateTaskCommand cmd, CancellationToken ct)
    {
        var task = new ScheduledTask
        {
            Name = cmd.Name,
            Description = cmd.Description,
            ScheduleType = cmd.ScheduleType,
            CronExpression = cmd.CronExpression,
            IntervalMinutes = cmd.IntervalMinutes,
            WebhookUrl = cmd.WebhookUrl,
            HttpMethod = cmd.HttpMethod,
            WebhookHeaders = cmd.WebhookHeaders,
            WebhookBody = cmd.WebhookBody,
            RetryCount = cmd.RetryCount,
            RetryDelaySeconds = cmd.RetryDelaySeconds,
            TimeoutSeconds = cmd.TimeoutSeconds,
            NextRunAt = _calculator.GetNextRun(cmd.ScheduleType, cmd.CronExpression, cmd.IntervalMinutes),
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[TASK CREATED] {Name} | Next: {NextRun}", task.Name, task.NextRunAt);
        return task;
    }
}