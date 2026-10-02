using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Entities;
using TaskFlow.Api.DTOs.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/tasks", async (TaskFlowDbContext db) =>
{
    var tasks = await db.Tasks.ToListAsync();

    return tasks;
});

app.MapPost("/tasks", async (
    CreateTaskRequest request,
    TaskFlowDbContext db) =>
{
    var task = new TaskItem
    {
        Title = request.Title,
        Description = request.Description
    };

    db.Tasks.Add(task);

    await db.SaveChangesAsync();

    return task;
});

app.Run();