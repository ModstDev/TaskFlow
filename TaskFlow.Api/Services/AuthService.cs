using Microsoft.AspNetCore.Identity;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs.Auth;
using TaskFlow.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Api.Services;

public class AuthService : IAuthService
{
    private readonly TaskFlowDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
    TaskFlowDbContext db,
    PasswordHasher<User> passwordHasher,
    ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<User?> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Username == request.Username ||
                u.Email == request.Email);

        if (existingUser is not null)
        {
            return null;
        }

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
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var accessToken = _tokenService.GenerateAccessToken(user);

        return new LoginResponse
        {
            AccessToken = accessToken.AccessToken,
            ExpiresIn = accessToken.ExpiresIn
        };
    }
}