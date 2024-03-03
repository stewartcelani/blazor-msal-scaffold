using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Blazor.Msal.Maui.Authentication;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Msal.Maui.Settings;

namespace Blazor.Msal.Maui.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly AppUser _appUser;
    private readonly AzureAdSettings _azureAdSettings;

    private IPublicClientApplication _clientApp = null!;

    public AuthenticationService(AppUser appUser, AzureAdSettings azureAdSettings)
    {
        _appUser = appUser;
        _azureAdSettings = azureAdSettings;
    }

    private IPublicClientApplication IdentityClient => _clientApp ??= CreatePublicClientApplication();

    public async Task<AuthenticationResult> AuthenticateAsync()
    {
        var accounts = await IdentityClient.GetAccountsAsync();
        AuthenticationResult result = null!;
        var tryInteractiveLogin = false;


        try
        {
            result = await IdentityClient
                .AcquireTokenSilent(_azureAdSettings.Scopes, accounts.FirstOrDefault())
                .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            tryInteractiveLogin = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MSAL Silent Error: {ex.Message}");
        }

        if (tryInteractiveLogin)
            try
            {
                result = await IdentityClient
                    .AcquireTokenInteractive(_azureAdSettings.Scopes)
                    .ExecuteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MSAL Interactive Error: {ex.Message}");
            }

        return result;
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string idToken)
    {
        var jwtHandler = new JwtSecurityTokenHandler();

        if (!jwtHandler.CanReadToken(idToken)) return null;
        var jwtToken = jwtHandler.ReadJwtToken(idToken);

        var kid = jwtToken.Header.Kid; // Key Identifier
        var issuer = jwtToken.Issuer; // Issuer

        if (string.IsNullOrEmpty(kid) || string.IsNullOrEmpty(issuer) ||
            !issuer.Equals(_azureAdSettings.Issuer, StringComparison.OrdinalIgnoreCase)) return null;

        using var httpClient = new HttpClient();
        var discoveryDocumentResponse =
            await httpClient.GetStringAsync($"{issuer}/.well-known/openid-configuration"); // JSON Web Key Sets
        var discoveryDocument = JsonDocument.Parse(discoveryDocumentResponse);
        var jsonWebKeySetsUri = discoveryDocument.RootElement.GetProperty("jwks_uri").GetString();
        if (string.IsNullOrEmpty(jsonWebKeySetsUri)) return null;

        var jsonWebKeySetsResponse = await httpClient.GetStringAsync(jsonWebKeySetsUri);
        var jsonWebKeySets = JsonDocument.Parse(jsonWebKeySetsResponse);

        SecurityKey? signingKey = null;

        foreach (var key in jsonWebKeySets.RootElement.GetProperty("keys").EnumerateArray())
        {
            if (key.GetProperty("kid").GetString() != kid) continue;

            var modulus = key.GetProperty("n").GetString();
            var exponent = key.GetProperty("e").GetString();

            if (string.IsNullOrEmpty(modulus) || string.IsNullOrEmpty(exponent)) return null;

            var rsa = RSA.Create();
            rsa.ImportParameters(
                new RSAParameters
                {
                    Modulus = Base64UrlDecode(modulus),
                    Exponent = Base64UrlDecode(exponent)
                });
            signingKey = new RsaSecurityKey(rsa);
            break;
        }

        if (signingKey == null) throw new SecurityTokenException("Signing key not found.");

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = _azureAdSettings.ClientId,
            IssuerSigningKey = signingKey,
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        SecurityToken validatedToken;
        try
        {
            var claimsPrincipal = jwtHandler.ValidateToken(idToken, validationParameters, out validatedToken);
            return claimsPrincipal;
        }
        catch (SecurityTokenValidationException ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SignInAsync()
    {
        var authResult = await AuthenticateAsync();
        var claimsPrincipal = await ValidateTokenAsync(authResult.IdToken);
        if (claimsPrincipal is not null && claimsPrincipal.Identity?.IsAuthenticated == true)
        {
            SetAuthenticatedUser(claimsPrincipal);
            return true;
        }

        SetAuthenticatedUser(new ClaimsPrincipal(new ClaimsIdentity()));
        return false;
    }

    public async Task<bool> SignOutAsync()
    {
        var accounts = await IdentityClient.GetAccountsAsync();
        foreach (var account in accounts) await IdentityClient.RemoveAsync(account);
        SetAuthenticatedUser(new ClaimsPrincipal(new ClaimsIdentity()));
        return true;
    }

    public ClaimsPrincipal AuthenticatedUser => _appUser.Principal;

    private IPublicClientApplication CreatePublicClientApplication()
    {
        var builder = PublicClientApplicationBuilder
            .Create(_azureAdSettings.ClientId)
            .WithTenantId(_azureAdSettings.TenantId)
#if ANDROID
            .WithRedirectUri($"msal{_azureAdSettings.ClientId}://auth")
            .WithParentActivityOrWindow(() => Platform.CurrentActivity)
#endif
#if WINDOWS
            .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
#endif
            .Build();

        return builder;
    }

    private void SetAuthenticatedUser(ClaimsPrincipal claimsPrincipal)
    {
        _appUser.Principal = claimsPrincipal;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input;
        output = output.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 0: break;
            case 2:
                output += "==";
                break;
            case 3:
                output += "=";
                break;
            default: throw new ArgumentException("Illegal base64url string!", nameof(input));
        }

        return Convert.FromBase64String(output);
    }
}