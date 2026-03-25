using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly TaskFlowDbContext _db;

    public AlertsController(TaskFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.AlertRules
            .Include(r => r.Task)
            .AsNoTracking()
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AlertRule rule)
    {
        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), rule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var rule = await _db.AlertRules.FindAsync(id);
        if (rule is null) return NotFound();
        _db.AlertRules.Remove(rule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var total = await _db.AlertHistories.CountAsync();
        var items = await _db.AlertHistories
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }
}