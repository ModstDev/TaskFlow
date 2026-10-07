using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs.Tasks;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public class TaskService : ITaskService
{
    private readonly TaskFlowDbContext _db;

    private readonly ICurrentUser _currentUser;

    public TaskService(
        TaskFlowDbContext db,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _db.Tasks
            .Where(t => t.UserId == _currentUser.UserId)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        return await _db.Tasks
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == _currentUser.UserId);
    }

    public async Task<TaskItem> CreateAsync(CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            UserId = _currentUser.UserId
        };

        _db.Tasks.Add(task);

        await _db.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem?> UpdateAsync(
    int id,
    UpdateTaskRequest request)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == _currentUser.UserId);

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
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == _currentUser.UserId);

        if (task is null)
        {
            return false;
        }

        _db.Tasks.Remove(task);

        await _db.SaveChangesAsync();

        return true;
    }
}