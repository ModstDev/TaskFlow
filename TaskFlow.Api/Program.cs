using TaskFlow.Api.Entities;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var tasks = new List<TaskItem>
{
    new TaskItem
    {
        Id = 1,
        Title = "Learn C# backend",
        Description = "Understand how ASP.NET Core works"
    },
    new TaskItem
    {
        Id = 2,
        Title = "Learn Entity Framework",
        Description = "Connect the API to a database"
    }
};

app.MapGet("/tasks", () =>
{
    return tasks;
});

app.Run();