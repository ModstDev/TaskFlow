using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs.Auth;
using TaskFlow.Api.Entities;
using TaskFlow.Api.Services;

namespace TaskFlow.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange

        // Create an in-memory SQLite database.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        // Create a user with a properly hashed password.
        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com"
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            "Password123!");

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Use fake token services.
        var tokenService = new FakeTokenService();
        var refreshTokenService = new FakeRefreshTokenService();

        var authService = new AuthService(
            db,
            passwordHasher,
            tokenService,
            refreshTokenService);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await authService.LoginAsync(request);

        // Assert
        Assert.NotNull(response);

        Assert.Equal("fake-access-token", response.AccessToken);
        Assert.Equal("fake-refresh-token", response.RefreshToken);
        Assert.Equal(900, response.ExpiresIn);

        Assert.Equal(1, tokenService.CallCount);
        Assert.Equal(1, refreshTokenService.CallCount);
    }

    private class FakeTokenService : ITokenService
    {
        public int CallCount { get; private set; }

        public (string AccessToken, int ExpiresIn) GenerateAccessToken(User user)
        {
            CallCount++;

            return ("fake-access-token", 900);
        }
    }

    private class FakeRefreshTokenService : IRefreshTokenService
    {
        public int CallCount { get; private set; }

        public Task<string> CreateAsync(User user)
        {
            CallCount++;

            return Task.FromResult("fake-refresh-token");
        }

        public Task<User?> ValidateAsync(string refreshToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task RevokeAsync(string refreshToken)
        {
            return Task.CompletedTask;
        }
    }
}