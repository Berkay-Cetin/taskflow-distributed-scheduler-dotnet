using MediatR;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Commands;

public record CreateTaskCommand : IRequest<ScheduledTask>
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public ScheduleType ScheduleType { get; init; }
    public string? CronExpression { get; init; }
    public int? IntervalMinutes { get; init; }
    public string WebhookUrl { get; init; } = default!;
    public string HttpMethod { get; init; } = "POST";
    public string? WebhookHeaders { get; init; }
    public string? WebhookBody { get; init; }
    public int RetryCount { get; init; } = 3;
    public int RetryDelaySeconds { get; init; } = 30;
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxConcurrent { get; init; } = 1;
    public int Priority { get; init; } = 5;
    public bool AllowMissedRuns { get; init; } = true;
}