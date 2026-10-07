using System.Security.Claims;

namespace TaskFlow.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null)
            {
                throw new InvalidOperationException(
                    "Authenticated user ID was not found.");
            }

            return int.Parse(userId);
        }
    }
}