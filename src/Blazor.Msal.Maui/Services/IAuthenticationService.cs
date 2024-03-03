using System.Security.Claims;
using Microsoft.Identity.Client;

namespace Blazor.Msal.Maui.Services;

public interface IAuthenticationService
{
    ClaimsPrincipal AuthenticatedUser { get; }
    Task<AuthenticationResult> AuthenticateAsync();
    Task<ClaimsPrincipal?> ValidateTokenAsync(string idToken);
    Task<bool> SignInAsync();
    Task<bool> SignOutAsync();
}