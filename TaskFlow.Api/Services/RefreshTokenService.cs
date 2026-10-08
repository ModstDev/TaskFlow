using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly TaskFlowDbContext _db;

    public RefreshTokenService(TaskFlowDbContext db)
    {
        _db = db;
    }

    public async Task<string> CreateAsync(User user)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);

        var refreshToken = Convert.ToBase64String(tokenBytes);

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(tokenBytes));

        var entity = new RefreshToken
        {
            Token = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            UserId = user.Id
        };

        _db.RefreshTokens.Add(entity);

        await _db.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<User?> ValidateAsync(string refreshToken)
    {
        byte[] tokenBytes;

        try
        {
            tokenBytes = Convert.FromBase64String(refreshToken);
        }
        catch (FormatException)
        {
            return null;
        }

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(tokenBytes));

        var entity = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == tokenHash);

        if (entity is null)
        {
            return null;
        }

        if (entity.IsRevoked)
        {
            return null;
        }

        if (entity.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        return entity.User;
    }

    public async Task RevokeAsync(string refreshToken)
    {
        byte[] tokenBytes;

        try
        {
            tokenBytes = Convert.FromBase64String(refreshToken);
        }
        catch (FormatException)
        {
            return;
        }

        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(tokenBytes));

        var entity = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == tokenHash);

        if (entity is null)
        {
            return;
        }

        entity.IsRevoked = true;

        await _db.SaveChangesAsync();
    }
}