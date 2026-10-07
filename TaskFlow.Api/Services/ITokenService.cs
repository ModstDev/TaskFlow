using TaskFlow.Api.Entities;

namespace TaskFlow.Api.Services;

public interface ITokenService
{
    (string AccessToken, int ExpiresIn) GenerateAccessToken(User user);
}