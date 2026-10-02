using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs.Tasks;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskFlowDbContext _db;

    public TasksController(TaskFlowDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _db.Tasks.ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description
        };

        _db.Tasks.Add(task);

        await _db.SaveChangesAsync();

        return Ok(task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(
    int id,
    UpdateTaskRequest request)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null)
        {
            return NotFound();
        }

        task.Title = request.Title;
        task.Description = request.Description;

        await _db.SaveChangesAsync();

        return Ok(task);
    }
}