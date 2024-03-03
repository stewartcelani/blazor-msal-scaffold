using FluentValidation;

namespace Msal.Maui.Settings;

public class AzureAdSettingsValidator : AbstractValidator<AzureAdSettings>
{
    public AzureAdSettingsValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Scopes).NotEmpty();
    }
}