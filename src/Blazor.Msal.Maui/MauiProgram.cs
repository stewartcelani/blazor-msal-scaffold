using System.Reflection;
using Blazor.Msal.Maui.Authentication;
using Blazor.Msal.Maui.Helpers;
using Blazor.Msal.Maui.Services;
using Blazor.Msal.Maui.Views;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Maui.LifecycleEvents;
using Msal.Maui.Settings;

namespace Blazor.Msal.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(platform =>
                {
                    platform.OnActivityResult((activity, rc, result, data) =>
                    {
                        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(rc, result,
                            data);
                    });
                });
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        
        // Load settings from appsettings.json
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;
        using var appSettingsStream = assembly.GetManifestResourceStream($"{assemblyName}.appsettings.json");
        if (appSettingsStream == null) throw new Exception($"{assemblyName}.appsettings.json not found");
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(appSettingsStream)
            .Build();
        builder.Configuration.AddConfiguration(configuration);
        var azureAdSettings =
            SettingsBinder.BindAndValidate<AzureAdSettings, AzureAdSettingsValidator>(builder.Configuration);
        builder.Services.AddSingleton(azureAdSettings);

        // Auth
        builder.Services.AddAuthorizationCore();
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<AppUser>();
        builder.Services.AddSingleton<AuthenticationStateProvider, HybridAuthenticationStateProvider>();
        
        // Views & Navigation
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddTransient<UnauthenticatedView>();
        builder.Services.AddTransient<AuthenticatedView>();
        builder.Services.AddTransient<INavigationService, NavigationService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}