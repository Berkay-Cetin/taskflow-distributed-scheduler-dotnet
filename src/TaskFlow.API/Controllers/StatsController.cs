using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;
using TaskStatus = TaskFlow.Shared.Models.TaskStatus;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly TaskFlowDbContext _db;

    public StatsController(TaskFlowDbContext db) => _db = db;

    // Genel özet
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var tasks = await _db.Tasks.AsNoTracking().ToListAsync();
        var executions = await _db.Executions.AsNoTracking().ToListAsync();

        var successCount = executions.Count(e => e.Status == ExecutionStatus.Success);
        var failedCount = executions.Count(e => e.Status == ExecutionStatus.Dead || e.Status == ExecutionStatus.Failed);
        var totalCount = executions.Count;

        return Ok(new
        {
            tasks = new
            {
                total = tasks.Count,
                enabled = tasks.Count(t => t.IsEnabled),
                disabled = tasks.Count(t => !t.IsEnabled),
                running = tasks.Count(t => t.LastStatus == TaskStatus.Running),
                failed = tasks.Count(t => t.LastStatus == TaskStatus.Failed),
            },
            executions = new
            {
                total = totalCount,
                success = successCount,
                failed = failedCount,
                successRate = totalCount > 0 ? Math.Round((double)successCount / totalCount * 100, 1) : 0,
                avgDurationMs = executions.Any()
                    ? Math.Round(executions.Average(e => (double)e.DurationMs), 0)
                    : 0,
            }
        });
    }

    // Son 24 saatlik execution timeline (saatlik gruplar)
    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline([FromQuery] int hours = 24)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var executions = await _db.Executions
            .Where(e => e.StartedAt >= since)
            .AsNoTracking()
            .ToListAsync();

        var timeline = executions
            .GroupBy(e => new DateTime(
                e.StartedAt.Year,
                e.StartedAt.Month,
                e.StartedAt.Day,
                e.StartedAt.Hour, 0, 0))
            .Select(g => new
            {
                hour = g.Key,
                total = g.Count(),
                success = g.Count(e => e.Status == ExecutionStatus.Success),
                failed = g.Count(e => e.Status == ExecutionStatus.Failed || e.Status == ExecutionStatus.Dead),
            })
            .OrderBy(x => x.hour)
            .ToList();

        return Ok(timeline);
    }

    // Task bazlı istatistikler
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTaskStats()
    {
        var stats = await _db.Tasks
            .AsNoTracking()
            .Select(t => new
            {
                taskId = t.Id,
                taskName = t.Name,
                total = _db.Executions.Count(e => e.TaskId == t.Id),
                success = _db.Executions.Count(e => e.TaskId == t.Id && e.Status == ExecutionStatus.Success),
                failed = _db.Executions.Count(e => e.TaskId == t.Id &&
                           (e.Status == ExecutionStatus.Failed || e.Status == ExecutionStatus.Dead)),
                avgDurationMs = _db.Executions
                    .Where(e => e.TaskId == t.Id)
                    .Average(e => (double?)e.DurationMs) ?? 0,
                lastRunAt = t.LastRunAt,
                lastStatus = t.LastStatus.ToString(),
            })
            .ToListAsync();

        return Ok(stats);
    }

    // Son 7 günlük günlük özet
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily()
    {
        var since = DateTime.UtcNow.AddDays(-7);

        var executions = await _db.Executions
            .Where(e => e.StartedAt >= since)
            .AsNoTracking()
            .ToListAsync();

        var daily = executions
            .GroupBy(e => e.StartedAt.Date)
            .Select(g => new
            {
                date = g.Key,
                total = g.Count(),
                success = g.Count(e => e.Status == ExecutionStatus.Success),
                failed = g.Count(e => e.Status == ExecutionStatus.Failed || e.Status == ExecutionStatus.Dead),
                avgDurationMs = g.Average(e => (double)e.DurationMs),
            })
            .OrderBy(x => x.date)
            .ToList();

        return Ok(daily);
    }
}