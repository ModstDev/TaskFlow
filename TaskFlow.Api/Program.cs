using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Entities;
using TaskFlow.Api.DTOs.Tasks;
using TaskFlow.Api.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PasswordHasher<User>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();