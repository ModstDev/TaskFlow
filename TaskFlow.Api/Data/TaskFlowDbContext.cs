using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Data;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }
}