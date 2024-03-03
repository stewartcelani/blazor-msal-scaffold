using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace Blazor.Msal.Maui.Helpers;

public static class SettingsBinder
{
    public static TSettings Bind<TSettings>(IConfiguration configuration) where TSettings : class
    {
        var name = Activator.CreateInstance<TSettings>().GetType().Name;
        return BindKey<TSettings>(name, configuration);
    }

    public static TSettings BindKey<TSettings>(string key, IConfiguration configuration) where TSettings : class
    {
        var settings = Activator.CreateInstance<TSettings>();
        configuration.GetSection(key).Bind(settings);
        return settings;
    }

    public static TSettings BindAndValidate<TSettings, TValidator>(IConfiguration configuration) where TSettings : class
        where TValidator : AbstractValidator<TSettings>
    {
        var name = Activator.CreateInstance<TSettings>().GetType().Name;
        return BindKeyAndValidate<TSettings, TValidator>(name, configuration);
    }

    public static TSettings BindKeyAndValidate<TSettings, TValidator>(string key, IConfiguration configuration)
        where TSettings : class
        where TValidator : AbstractValidator<TSettings>
    {
        var settings = BindKey<TSettings>(key, configuration);
        var validator = Activator.CreateInstance<TValidator>();
        var validationResult = validator.Validate(settings);
        if (!validationResult.IsValid)
            throw new ValidationException(
                $"Failed binding {settings.GetType().Name} using ConfigurationBinder.Bind: ${validationResult}");
        return settings;
    }
}