using Blazor.Msal.Maui.Authentication;
using Blazor.Msal.Maui.Services;
using Microsoft.Identity.Client;

namespace Blazor.Msal.Maui.Views;

public partial class UnauthenticatedView : ContentPage
{
    private readonly IAuthenticationService _authenticationService;
    private readonly INavigationService _navigationService;


    public UnauthenticatedView(IAuthenticationService authenticationService, INavigationService navigationService)
    {
        _authenticationService = authenticationService;
        _navigationService = navigationService;
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var signInResult = await _authenticationService.SignInAsync();
            if (signInResult) await _navigationService.NavigateToAsync<AuthenticatedView>();
        }
        catch (MsalUiRequiredException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
        }
    }
}