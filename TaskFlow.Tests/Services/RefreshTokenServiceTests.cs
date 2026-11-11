using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Entities;
using TaskFlow.Api.Services;
using System.Security.Cryptography;

namespace TaskFlow.Tests.Services;

public class RefreshTokenServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidUser_CreatesAndStoresRefreshTokenHash()
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
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "irrelevant-for-this-test"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var refreshTokenService = new RefreshTokenService(db);

        // Act
        var rawToken = await refreshTokenService.CreateAsync(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        // Verify that the token was stored in the database.
        var storedToken = await db.RefreshTokens
            .SingleAsync();

        // The database must not contain the original token.
        Assert.NotEqual(rawToken, storedToken.Token);

        // Verify that the stored token belongs to our user.
        Assert.Equal(user.Id, storedToken.UserId);

        // Verify that the token can be validated.
        var validatedUser = await refreshTokenService.ValidateAsync(rawToken);

        Assert.NotNull(validatedUser);
        Assert.Equal(user.Id, validatedUser.Id);
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownToken_ReturnsNull()
    {
        // Arrange
        var (connection, db) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var dbScope = db;

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "irrelevant"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new RefreshTokenService(db);

        var unknownToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        // Act
        var result = await service.ValidateAsync(unknownToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var (connection, db) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var dbScope = db;

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "irrelevant"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var rawToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        await SeedRefreshTokenAsync(
            db,
            user,
            rawToken,
            DateTime.UtcNow.AddMinutes(-10),
            false);

        var service = new RefreshTokenService(db);

        // Act
        var result = await service.ValidateAsync(rawToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WithRevokedToken_ReturnsNull()
    {
        // Arrange
        var (connection, db) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var dbScope = db;

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "irrelevant"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var rawToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        await SeedRefreshTokenAsync(
            db,
            user,
            rawToken,
            DateTime.UtcNow.AddDays(30),
            true);

        var service = new RefreshTokenService(db);

        // Act
        var result = await service.ValidateAsync(rawToken);

        // Assert
        Assert.Null(result);
    }

    private static async Task<(SqliteConnection Connection, TaskFlowDbContext Db)> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new TaskFlowDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return (connection, db);
    }

    private static async Task SeedRefreshTokenAsync(
    TaskFlowDbContext db,
    User user,
    string rawToken,
    DateTime expiresAt,
    bool isRevoked)
    {
        var tokenBytes = Convert.FromBase64String(rawToken);

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(tokenBytes));

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = tokenHash,
            UserId = user.Id,
            ExpiresAt = expiresAt,
            IsRevoked = isRevoked
        });

        await db.SaveChangesAsync();
    }
}

