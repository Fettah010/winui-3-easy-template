using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Windows.Globalization;
using Serilog;

[assembly: InternalsVisibleTo("DevTemWinUi3.Tests")]

namespace DevTemWinUi3.Services;

/// <summary>
/// Provides access to localized strings. Uses dictionary-based resources
/// for reliable unpackaged app support.
/// </summary>
public sealed class LocalizationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private string _currentLanguage = "en-US";

    public static LocalizationService Current { get; } = new();

    /// <summary>
    /// Available languages in the app.
    /// </summary>
    public static IReadOnlyList<LanguageInfo> AvailableLanguages { get; } = new List<LanguageInfo>
    {
        new("en-US", "English", "English"),
        new("es-ES", "Español", "Spanish"),
        new("fr-FR", "Français", "French"),
    };

    static LocalizationService()
    {
        _resources["en-US"] = new Dictionary<string, string>
        {
            ["AppName"] = "DevTem-WinUI 3",
            ["NavHome"] = "Home",
            ["NavAbout"] = "About",
            ["NavSettings"] = "Settings",
            ["HomeTitle"] = "Welcome to DevTem-WinUI 3",
            ["HomeDescription"] = "A modern Windows 11 app built with WinUI 3 (.NET 10). This template includes auto-updates, logging, dependency injection, SQLite database, and more.",
            ["AboutTitle"] = "About",
            ["AboutAppInfo"] = "APP INFO",
            ["AboutVersion"] = "Version",
            ["AboutFramework"] = "Framework",
            ["AboutTechnology"] = "TECHNOLOGY",
            ["AboutLicense"] = "LICENSE",
            ["AboutLinks"] = "LINKS",
            ["SettingsTitle"] = "Settings",
            ["SettingsAppearance"] = "APPEARANCE",
            ["SettingsTheme"] = "Theme",
            ["SettingsThemeSystem"] = "System",
            ["SettingsThemeLight"] = "Light",
            ["SettingsThemeDark"] = "Dark",
            ["SettingsLanguage"] = "Language",
            ["SettingsUpdates"] = "UPDATES",
            ["SettingsAutoCheck"] = "Auto-check for updates",
            ["SettingsAbout"] = "ABOUT",
            ["FirstRunTitle"] = "Welcome to DevTem-WinUI 3",
            ["FirstRunButton"] = "Get Started",
        };

        _resources["es-ES"] = new Dictionary<string, string>
        {
            ["AppName"] = "DevTem-WinUI 3",
            ["NavHome"] = "Inicio",
            ["NavAbout"] = "Acerca de",
            ["NavSettings"] = "Configuración",
            ["HomeTitle"] = "Bienvenido a DevTem-WinUI 3",
            ["HomeDescription"] = "Una aplicación moderna de Windows 11 construida con WinUI 3 (.NET 10). Esta plantilla incluye actualizaciones automáticas, registro, inyección de dependencias, base de datos SQLite y más.",
            ["AboutTitle"] = "Acerca de",
            ["AboutAppInfo"] = "INFORMACIÓN DE LA APLICACIÓN",
            ["AboutVersion"] = "Versión",
            ["AboutFramework"] = "Framework",
            ["AboutTechnology"] = "TECNOLOGÍA",
            ["AboutLicense"] = "LICENCIA",
            ["AboutLinks"] = "ENLACES",
            ["SettingsTitle"] = "Configuración",
            ["SettingsAppearance"] = "APARIENCIA",
            ["SettingsTheme"] = "Tema",
            ["SettingsThemeSystem"] = "Sistema",
            ["SettingsThemeLight"] = "Claro",
            ["SettingsThemeDark"] = "Oscuro",
            ["SettingsLanguage"] = "Idioma",
            ["SettingsUpdates"] = "ACTUALIZACIONES",
            ["SettingsAutoCheck"] = "Verificar actualizaciones automáticamente",
            ["SettingsAbout"] = "ACERCA DE",
            ["FirstRunTitle"] = "Bienvenido a DevTem-WinUI 3",
            ["FirstRunButton"] = "Empezar",
        };

        _resources["fr-FR"] = new Dictionary<string, string>
        {
            ["AppName"] = "DevTem-WinUI 3",
            ["NavHome"] = "Accueil",
            ["NavAbout"] = "À propos",
            ["NavSettings"] = "Paramètres",
            ["HomeTitle"] = "Bienvenue dans DevTem-WinUI 3",
            ["HomeDescription"] = "Une application moderne Windows 11 construite avec WinUI 3 (.NET 10). Ce modèle inclut les mises à jour automatiques, la journalisation, l'injection de dépendances, la base de données SQLite et plus encore.",
            ["AboutTitle"] = "À propos",
            ["AboutAppInfo"] = "INFORMATIONS SUR L'APPLICATION",
            ["AboutVersion"] = "Version",
            ["AboutFramework"] = "Framework",
            ["AboutTechnology"] = "TECHNOLOGIE",
            ["AboutLicense"] = "LICENCE",
            ["AboutLinks"] = "LIENS",
            ["SettingsTitle"] = "Paramètres",
            ["SettingsAppearance"] = "APPARENCE",
            ["SettingsTheme"] = "Thème",
            ["SettingsThemeSystem"] = "Système",
            ["SettingsThemeLight"] = "Clair",
            ["SettingsThemeDark"] = "Sombre",
            ["SettingsLanguage"] = "Langue",
            ["SettingsUpdates"] = "MISES À JOUR",
            ["SettingsAutoCheck"] = "Vérifier les mises à jour automatiquement",
            ["SettingsAbout"] = "À PROPOS",
            ["FirstRunTitle"] = "Bienvenue dans DevTem-WinUI 3",
            ["FirstRunButton"] = "Commencer",
        };
    }

    private LocalizationService()
    {
    }

    /// <summary>
    /// Current UI language code (e.g., "en-US").
    /// </summary>
    public string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Initializes the localization service. Call once at startup.
    /// </summary>
    public void Initialize()
    {
        try
        {
            var lang = ApplicationLanguages.PrimaryLanguageOverride;
            if (!string.IsNullOrEmpty(lang) && _resources.ContainsKey(lang))
                _currentLanguage = lang;
            Log.Information("Localization initialized. Language: {Language}", _currentLanguage);
        }
        catch
        {
            _currentLanguage = "en-US";
        }
    }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    public string GetString(string key)
    {
        if (_resources.TryGetValue(_currentLanguage, out var strings) && strings.TryGetValue(key, out var value))
            return value;
        if (_resources["en-US"].TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>
    /// Sets the UI language for the app. Takes effect on next app launch.
    /// </summary>
    public void SetLanguage(string languageTag)
    {
        if (_resources.ContainsKey(languageTag))
        {
            _currentLanguage = languageTag;
            try { ApplicationLanguages.PrimaryLanguageOverride = languageTag; }
            catch { }
            Log.Information("Language changed to: {Language}", languageTag);
        }
    }

    /// <summary>
    /// Resets the language to the system default.
    /// </summary>
    public void ResetLanguage()
    {
        _currentLanguage = "en-US";
        try { ApplicationLanguages.PrimaryLanguageOverride = string.Empty; }
        catch { }
        Log.Information("Language reset to system default");
    }
}

/// <summary>
/// Represents a supported language.
/// </summary>
public record LanguageInfo(string Tag, string NativeName, string EnglishName)
{
    public override string ToString() => NativeName;
}
