using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs.Tasks;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public class TaskService : ITaskService
{
    private readonly TaskFlowDbContext _db;

    public TaskService(TaskFlowDbContext db)
    {
        _db = db;
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _db.Tasks.ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        return await _db.Tasks.FindAsync(id);
    }

    public async Task<TaskItem> CreateAsync(CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description
        };

        _db.Tasks.Add(task);

        await _db.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem?> UpdateAsync(
        int id,
        UpdateTaskRequest request)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null)
        {
            return null;
        }

        task.Title = request.Title;
        task.Description = request.Description;

        await _db.SaveChangesAsync();

        return task;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null)
        {
            return false;
        }

        _db.Tasks.Remove(task);

        await _db.SaveChangesAsync();

        return true;
    }
}