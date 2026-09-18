using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Windows.Globalization;
using DevTemWinUi3.Services.Localization;

[assembly: InternalsVisibleTo("DevTemWinUi3.Tests")]

namespace DevTemWinUi3.Services;

/// <summary>
/// Provides access to localized strings. Per-language dictionaries live in
/// <c>Services/Localization/</c> (one file per language); this class is
/// lookup + language state only. UI binds to <see cref="Item"/> through the
/// <c>Loc</c> markup extension, so language switches apply instantly with
/// no code-behind string mapping. Uses dictionary-based resources for
/// reliable unpackaged app support.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private const string PersistKey = "AppLanguage";
    private const string DefaultLanguage = "en-US";
    private string _currentLanguage = DefaultLanguage;

    public static LocalizationService Current { get; } = new();

    /// <summary>
    /// Raised whenever the UI language changes so open pages, the nav pane,
    /// and dialogs can re-apply strings instantly (no restart needed).
    /// Prefer XAML binding (which listens to <see cref="PropertyChanged"/>
    /// below) over subscribing to this; it exists for composed text
    /// (status lines, native menus) that cannot bind.
    /// </summary>
    public event EventHandler? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

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
        _resources["en-US"] = EnStrings.Strings;
        _resources["es-ES"] = EsStrings.Strings;
        _resources["fr-FR"] = FrStrings.Strings;
    }

    private LocalizationService()
    {
    }

    /// <summary>
    /// Current UI language code (e.g., "en-US").
    /// </summary>
    public string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Localized string by key — the XAML binding surface
    /// (<c>{loc:Loc Key=…}</c> binds here).
    /// </summary>
    public string this[string key] => GetString(key);

    /// <summary>
    /// Initializes the localization service. Call once at startup.
    /// Prefers our own persisted choice (reliable for unpackaged apps, where
    /// the OS language override does not stick across restarts), then the OS
    /// override, then English.
    /// </summary>
    public void Initialize()
    {
        try
        {
            var persisted = LocalSettingsStore.Shared.Get<string?>(PersistKey, null);
            if (!string.IsNullOrEmpty(persisted) && _resources.ContainsKey(persisted))
            {
                _currentLanguage = persisted;
            }
            else
            {
                var lang = ApplicationLanguages.PrimaryLanguageOverride;
                if (!string.IsNullOrEmpty(lang) && _resources.ContainsKey(lang))
                    _currentLanguage = lang;
            }
            AppLog.Information("Localization initialized. Language: {Language}", _currentLanguage);
        }
        catch
        {
            _currentLanguage = DefaultLanguage;
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
    /// Sets the UI language for the app. Applies instantly: bindings refresh
    /// through <see cref="PropertyChanged"/> and <see cref="LanguageChanged"/>
    /// notifies composed text (status lines, native menus, dialogs).
    /// </summary>
    public void SetLanguage(string languageTag)
    {
        if (_resources.ContainsKey(languageTag))
        {
            _currentLanguage = languageTag;
            try { ApplicationLanguages.PrimaryLanguageOverride = languageTag; }
            catch { }
            try { LocalSettingsStore.Shared.Set(PersistKey, languageTag); }
            catch { }
            AppLog.Information("Language changed to: {Language}", languageTag);
            try { CrashReportingService.AddBreadcrumb("Language: " + languageTag, "settings"); } catch { }
            RaiseLanguageChanged();
        }
    }

    /// <summary>
    /// Resets the language to the system default.
    /// </summary>
    public void ResetLanguage()
    {
        _currentLanguage = DefaultLanguage;
        try { ApplicationLanguages.PrimaryLanguageOverride = string.Empty; }
        catch { }
        try { LocalSettingsStore.Shared.Remove(PersistKey); }
        catch { }
        AppLog.Information("Language reset to system default");
        RaiseLanguageChanged();
    }

    private void RaiseLanguageChanged()
    {
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        // "Item[]" is the indexer-change name binding engines listen for:
        // every {loc:Loc Key=…} binding refreshes from one notification.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    /// <summary>
    /// All localized values across languages (test surface for
    /// <c>LocalizationCoverageTests</c>: key parity, no hardcoded versions).
    /// </summary>
    internal static IEnumerable<string> GetAllValues()
    {
        foreach (var dict in _resources.Values)
            foreach (var value in dict.Values)
                yield return value;
    }

    /// <summary>All registered language tags (test surface).</summary>
    internal static IEnumerable<string> GetAllLanguages() => _resources.Keys;
}
