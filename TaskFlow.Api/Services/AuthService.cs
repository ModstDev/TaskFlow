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

    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(
    TaskFlowDbContext db,
    PasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
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

        var refreshToken =
            await _refreshTokenService.CreateAsync(user);

        return new LoginResponse
        {
            AccessToken = accessToken.AccessToken,
            RefreshToken = refreshToken,
            ExpiresIn = accessToken.ExpiresIn
        };
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken)
    {
        var user = await _refreshTokenService
            .ValidateAsync(refreshToken);

        if (user is null)
        {
            return null;
        }

        await _refreshTokenService.RevokeAsync(refreshToken);

        var token = _tokenService.GenerateAccessToken(user);

        var newRefreshToken =
            await _refreshTokenService.CreateAsync(user);

        return new LoginResponse
        {
            AccessToken = token.AccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = token.ExpiresIn
        };
    }

    public async Task LogoutAsync(string refreshToken)
    {
        await _refreshTokenService.RevokeAsync(refreshToken);
    }
}