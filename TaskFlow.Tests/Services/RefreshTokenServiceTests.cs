using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Entities;
using TaskFlow.Api.Services;

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
}
