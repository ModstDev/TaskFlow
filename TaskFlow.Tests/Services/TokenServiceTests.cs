
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using TaskFlow.Api.Configuration;
using TaskFlow.Api.Entities;
using TaskFlow.Api.Services;

namespace TaskFlow.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ShouldCreateValidTokenWithCorrectUserClaims()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Key = "test-secret-key-that-is-long-enough-12345",
            Issuer = "TaskFlow.Tests",
            Audience = "TaskFlow.Tests.Client",
            ExpiresInMinutes = 15
        };

        var service = new TokenService(Options.Create(settings));

        var user = new User
        {
            Id = 42,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var result = service.GenerateAccessToken(user);

        // Assert: validate the token using the expected configuration
        var handler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,

            ValidateAudience = true,
            ValidAudience = settings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(settings.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(
            result.AccessToken,
            validationParameters,
            out var validatedToken);

        Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.Equal("42",
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        Assert.Equal("testuser",
            principal.FindFirst(ClaimTypes.Name)?.Value);

        Assert.Equal("test@example.com",
            principal.FindFirst(ClaimTypes.Email)?.Value);

        Assert.Equal(900, result.ExpiresIn);
    }
}
