using Microsoft.AspNetCore.Identity;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs.Auth;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public class AuthService : IAuthService
{
    private readonly TaskFlowDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        TaskFlowDbContext db,
        PasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return user;
    }
}