using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.CQRS.Queries;

public record GetTasksQuery(string? TagFilter = null) : IRequest<List<ScheduledTask>>;

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, List<ScheduledTask>>
{
    private readonly TaskFlowDbContext _db;

    public GetTasksQueryHandler(TaskFlowDbContext db) => _db = db;

    public async Task<List<ScheduledTask>> Handle(GetTasksQuery query, CancellationToken ct)
    {
        var q = _db.Tasks
            .Include(t => t.TaskTags)
                .ThenInclude(tt => tt.Tag)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(query.TagFilter))
            q = q.Where(t => t.TaskTags.Any(tt => tt.Tag!.Name == query.TagFilter));

        return await q.OrderBy(t => t.Priority).ThenBy(t => t.Name).ToListAsync(ct);
    }
}