using System.Globalization;
using System.Security.Claims;
using my_project.Core.Enums;
using my_project.Core.Interfaces.Infrastructure;

namespace my_project.API.Auth;

// The authenticated user of the current request, read from the JWT claims
// (the same claims JwtTokenService writes into the token)
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId => int.Parse(GetRequiredClaim(ClaimTypes.NameIdentifier), CultureInfo.InvariantCulture);

    public string Email => GetRequiredClaim(ClaimTypes.Email);

    public bool IsSystemAdmin => Principal?.HasClaim(ClaimTypes.Role, nameof(SystemRole.SystemAdmin)) == true;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    // Used only behind [Authorize], so a missing claim is a programming error, not a client error
    private string GetRequiredClaim(string claimType)
    {
        return Principal?.FindFirstValue(claimType)
            ?? throw new InvalidOperationException($"The current request has no authenticated user (missing claim '{claimType}').");
    }
}
