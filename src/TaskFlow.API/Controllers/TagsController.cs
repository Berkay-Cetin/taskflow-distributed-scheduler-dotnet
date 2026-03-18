using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.Data;
using TaskFlow.Shared.Models;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly TaskFlowDbContext _db;

    public TagsController(TaskFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskTag tag)
    {
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), tag);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag is null) return NotFound();
        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Task'a tag ekle
    [HttpPost("{tagId:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> AddToTask(Guid tagId, Guid taskId)
    {
        var exists = await _db.TaskTags
            .AnyAsync(x => x.TagId == tagId && x.TaskId == taskId);

        if (exists) return Conflict("Tag already assigned");

        _db.TaskTags.Add(new ScheduledTaskTag { TagId = tagId, TaskId = taskId });
        await _db.SaveChangesAsync();
        return Ok();
    }

    // Task'tan tag kaldır
    [HttpDelete("{tagId:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> RemoveFromTask(Guid tagId, Guid taskId)
    {
        var link = await _db.TaskTags
            .FirstOrDefaultAsync(x => x.TagId == tagId && x.TaskId == taskId);

        if (link is null) return NotFound();

        _db.TaskTags.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}