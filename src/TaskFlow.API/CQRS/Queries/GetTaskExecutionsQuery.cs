using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Queries;

public record GetTaskExecutionsQuery(Guid TaskId, int Page = 1, int PageSize = 20) : IRequest<List<TaskExecution>>;

public class GetTaskExecutionsQueryHandler : IRequestHandler<GetTaskExecutionsQuery, List<TaskExecution>>
{
    private readonly TaskFlowDbContext _db;

    public GetTaskExecutionsQueryHandler(TaskFlowDbContext db) => _db = db;

    public async Task<List<TaskExecution>> Handle(GetTaskExecutionsQuery query, CancellationToken ct)
        => await _db.Executions
            .Where(e => e.TaskId == query.TaskId)
            .OrderByDescending(e => e.StartedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .AsNoTracking()
            .ToListAsync(ct);
}