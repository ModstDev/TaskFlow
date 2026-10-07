using TaskFlow.Api.DTOs.Auth;
using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(RegisterRequest request);
}