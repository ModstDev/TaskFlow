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
        Assert.Equal(1, refreshTokenService.CreateCallCount);
    }


    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsNull()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com"
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            "CorrectPassword123!");

        db.Users.Add(user);
        await db.SaveChangesAsync();

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
            Password = "WrongPassword123!"
        };

        // Act
        var response = await authService.LoginAsync(request);

        // Assert
        Assert.Null(response);

        Assert.Equal(0, tokenService.CallCount);
        Assert.Equal(0, refreshTokenService.CreateCallCount);
    }


    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsNull()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var passwordHasher = new PasswordHasher<User>();

        var tokenService = new FakeTokenService();
        var refreshTokenService = new FakeRefreshTokenService();

        var authService = new AuthService(
            db,
            passwordHasher,
            tokenService,
            refreshTokenService);

        var request = new LoginRequest
        {
            Email = "unknown@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await authService.LoginAsync(request);

        // Assert
        Assert.Null(response);

        Assert.Equal(0, tokenService.CallCount);
        Assert.Equal(0, refreshTokenService.CreateCallCount);
    }


    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserWithHashedPassword()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var passwordHasher = new PasswordHasher<User>();

        var authService = new AuthService(
            db,
            passwordHasher,
            new FakeTokenService(),
            new FakeRefreshTokenService());

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        // Act
        var user = await authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(user);

        Assert.True(user.Id > 0);
        Assert.Equal("newuser", user.Username);
        Assert.Equal("newuser@example.com", user.Email);

        // Verify that the original password wasn't stored.
        Assert.NotEqual(request.Password, user.PasswordHash);

        // Verify that the stored hash matches the original password.
        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        Assert.Equal(
            PasswordVerificationResult.Success,
            verificationResult);

        // Verify that the user was saved to the database.
        Assert.Equal(1, await db.Users.CountAsync());
    }


    [Theory]
    [InlineData("testuser", "different@example.com")]
    [InlineData("differentuser", "test@example.com")]
    public async Task RegisterAsync_WithDuplicateUsernameOrEmail_ReturnsNull(
        string username,
        string email)
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var passwordHasher = new PasswordHasher<User>();

        var existingUser = new User
        {
            Username = "testuser",
            Email = "test@example.com"
        };

        existingUser.PasswordHash = passwordHasher.HashPassword(
            existingUser,
            "Password123!");

        db.Users.Add(existingUser);
        await db.SaveChangesAsync();

        var authService = new AuthService(
            db,
            passwordHasher,
            new FakeTokenService(),
            new FakeRefreshTokenService());

        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = "AnotherPassword123!"
        };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.Null(result);

        // The existing user should be the only user in the database.
        Assert.Equal(1, await db.Users.CountAsync());
    }


    [Fact]
    public async Task RefreshAsync_WithValidToken_ReturnsNewTokensAndRevokesOldToken()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = 42,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "irrelevant-for-this-test"
        };

        var tokenService = new FakeTokenService();

        var refreshTokenService = new FakeRefreshTokenService
        {
            UserToReturn = user
        };

        var authService = new AuthService(
            db,
            new PasswordHasher<User>(),
            tokenService,
            refreshTokenService);

        var oldRefreshToken = "old-refresh-token";

        // Act
        var response = await authService.RefreshAsync(oldRefreshToken);

        // Assert
        Assert.NotNull(response);

        Assert.Equal("fake-access-token", response.AccessToken);
        Assert.Equal("fake-refresh-token", response.RefreshToken);
        Assert.Equal(900, response.ExpiresIn);

        // Verify that the old token was validated and revoked.
        Assert.Equal(1, refreshTokenService.ValidateCallCount);
        Assert.Equal(1, refreshTokenService.RevokeCallCount);
        Assert.Equal(oldRefreshToken, refreshTokenService.RevokedToken);

        // Verify that replacement tokens were generated.
        Assert.Equal(1, tokenService.CallCount);
        Assert.Equal(1, refreshTokenService.CreateCallCount);
    }


    [Fact]
    public async Task RefreshAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var tokenService = new FakeTokenService();
        var refreshTokenService = new FakeRefreshTokenService
        {
            UserToReturn = null
        };

        var authService = new AuthService(
            db,
            new PasswordHasher<User>(),
            tokenService,
            refreshTokenService);

        // Act
        var response = await authService.RefreshAsync("invalid-refresh-token");

        // Assert
        Assert.Null(response);

        Assert.Equal(1, refreshTokenService.ValidateCallCount);

        // No tokens should be generated or revoked.
        Assert.Equal(0, refreshTokenService.RevokeCallCount);
        Assert.Equal(0, refreshTokenService.CreateCallCount);
        Assert.Equal(0, tokenService.CallCount);
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
        public User? UserToReturn { get; set; }

        public int CreateCallCount { get; private set; }
        public int ValidateCallCount { get; private set; }
        public int RevokeCallCount { get; private set; }

        public string? RevokedToken { get; private set; }

        public Task<string> CreateAsync(User user)
        {
            CreateCallCount++;

            return Task.FromResult("fake-refresh-token");
        }

        public Task<User?> ValidateAsync(string refreshToken)
        {
            ValidateCallCount++;

            return Task.FromResult(UserToReturn);
        }

        public Task RevokeAsync(string refreshToken)
        {
            RevokeCallCount++;
            RevokedToken = refreshToken;

            return Task.CompletedTask;
        }
    }
}