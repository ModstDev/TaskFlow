using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs.Tasks;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _taskService.GetAllAsync();

        var response = tasks.Select(task => new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var task = await _taskService.GetByIdAsync(id);

        if (task is null)
        {
            return NotFound();
        }

        var response = new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(CreateTaskRequest request)
    {
        var task = await _taskService.CreateAsync(request);

        var response = new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description
        };

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(
        int id,
        UpdateTaskRequest request)
    {
        var task = await _taskService.UpdateAsync(id, request);

        if (task is null)
        {
            return NotFound();
        }

        var response = new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description
        };

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var deleted = await _taskService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}