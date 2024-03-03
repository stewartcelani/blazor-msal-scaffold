using System.Security.Claims;

namespace Blazor.Msal.Maui.Authentication;

public class AppUser
{
    public ClaimsPrincipal Principal { get; set; } = new(new ClaimsIdentity());
}