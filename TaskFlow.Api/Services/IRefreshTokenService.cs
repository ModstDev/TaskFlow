using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(User user);

    Task<User?> ValidateAsync(string refreshToken);

    Task RevokeAsync(string refreshToken);
}