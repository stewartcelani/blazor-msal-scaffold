namespace Msal.Maui.Settings;

public class AzureAdSettings
{
    public string ClientId { get; set; }
    public string TenantId { get; set; }
    public List<string> Scopes { get; set; } = new();
    public string Issuer => $"https://login.microsoftonline.com/{TenantId}/v2.0";
}