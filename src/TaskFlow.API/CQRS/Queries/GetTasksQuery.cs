using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Queries;

public record GetTasksQuery : IRequest<List<ScheduledTask>>;

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<ScheduledTask>>
{
    private readonly TaskFlowDbContext _db;

    public GetTasksQueryHandler(TaskFlowDbContext db) => _db = db;

    public async Task<List<ScheduledTask>> Handle(GetTasksQuery query, CancellationToken ct)
        => await _db.Tasks.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
}