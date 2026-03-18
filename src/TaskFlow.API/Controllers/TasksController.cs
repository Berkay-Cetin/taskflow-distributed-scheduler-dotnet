using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.API.CQRS.Commands;
using TaskFlow.API.CQRS.Queries;
using TaskFlow.API.Data;
using Microsoft.AspNetCore.Authorization;

namespace TaskFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]

public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TaskFlowDbContext _db;

    public TasksController(IMediator mediator, TaskFlowDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? tag = null)
    => Ok(await _mediator.Send(new GetTasksQuery(tag)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand cmd)
    {
        var task = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(GetExecutions), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand cmd)
    {
        var task = await _mediator.Send(cmd with { Id = id });
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // TODO: DeleteTaskCommand
        return NoContent();
    }

    [HttpPost("{id:guid}/trigger")]
    public async Task<IActionResult> Trigger(Guid id)
    {
        var result = await _mediator.Send(new TriggerTaskCommand(id));
        return result ? Ok("Task triggered") : NotFound();
    }

    [HttpPatch("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id)
    {
        var task = await _mediator.Send(new ToggleTaskCommand(id, true));
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPatch("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id)
    {
        var task = await _mediator.Send(new ToggleTaskCommand(id, false));
        return task is null ? NotFound() : Ok(task);
    }

    [HttpGet("{id:guid}/executions")]
    public async Task<IActionResult> GetExecutions(Guid id, [FromQuery] int page = 1)
        => Ok(await _mediator.Send(new GetTaskExecutionsQuery(id, page)));

    [HttpGet("{id:guid}/missed-runs")]
    public async Task<IActionResult> GetMissedRuns(Guid id)
    {
        var runs = await _db.MissedRuns
            .Where(m => m.TaskId == id)
            .OrderByDescending(m => m.ScheduledAt)
            .Take(50)
            .AsNoTracking()
            .ToListAsync();
        return Ok(runs);
    }

    [HttpGet("{id:guid}/executions/{executionId:guid}/logs")]
    public async Task<IActionResult> GetExecutionLogs(Guid id, Guid executionId)
    {
        var logs = await _db.ExecutionLogs
            .Where(l => l.ExecutionId == executionId)
            .OrderBy(l => l.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
        return Ok(logs);
    }
}