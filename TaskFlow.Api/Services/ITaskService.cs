using TaskFlow.Api.DTOs.Tasks;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetByIdAsync(int id);
    Task<TaskItem> CreateAsync(CreateTaskRequest request);
    Task<TaskItem?> UpdateAsync(int id, UpdateTaskRequest request);
    Task<bool> DeleteAsync(int id);
}