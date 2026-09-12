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
    /// Raised whenever the UI language changes so open pages, the nav pane,
    /// and dialogs can re-apply strings instantly (no restart needed).
    /// </summary>
    public event EventHandler? LanguageChanged;

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
            ["HomeCheckUpdates"] = "Check for updates",
            ["HomeOpenSettings"] = "Open Settings",
            ["HomeReleasesLink"] = "How releases work",
            ["HomeStatusOk"] = "All systems operational",
            ["HomeQuickActions"] = "QUICK ACTIONS",
            ["HomeIncluded"] = "WHAT'S INCLUDED",
            ["HomeFeatUpdatesTitle"] = "Velopack auto-updates",
            ["HomeFeatUpdatesDesc"] = "Stable / Beta / Dev channels over GitHub Releases with one-click install and restart.",
            ["HomeFeatSettingsTitle"] = "Settings that persist",
            ["HomeFeatSettingsDesc"] = "Theme, language, update channel, tray and autostart — saved locally, restored on launch.",
            ["HomeFeatDiagnosticsTitle"] = "Diagnostics built in",
            ["HomeFeatDiagnosticsDesc"] = "Serilog rolling logs plus notification toasts so update checks never fail silently.",
            ["HomeFeatTrayTitle"] = "Tray + native shell",
            ["HomeFeatTrayDesc"] = "Mica backdrop, custom title bar, minimize-to-tray with quick Settings and update actions.",
            ["HomeShipTitle"] = "SHIP YOUR FIRST RELEASE",
            ["HomeShipBody"] = "1. Bump Version in DevTemWinUi3.csproj  →  2. git tag v0.0.2-beta + push  →  3. CI builds, packs and publishes to GitHub Releases.",
            ["HomeShipNote"] = "Publishing scripts live in /Scripts. Update checks run only in installed builds.",
            ["AboutTitle"] = "About",
            ["AboutSubtitle"] = "Everything behind DevTem — version, stack, license and links.",
            ["AboutGlance"] = "AT A GLANCE",
            ["AboutAppInfo"] = "APP INFO",
            ["AboutVersion"] = "Version",
            ["AboutFramework"] = "Framework",
            ["AboutPlatform"] = "Platform",
            ["AboutTechnology"] = "TECHNOLOGY",
            ["AboutLicense"] = "LICENSE",
            ["AboutLinks"] = "LINKS",
            ["AboutDescription"] = "A production-ready starter template: Mica + native WinUI 3 styling, persisted settings, Velopack auto-updates, Serilog diagnostics and a one-command release pipeline.",
            ["AboutGithub"] = "GitHub Repository",
            ["AboutReportIssue"] = "Report an issue",
            ["AboutCardName"] = "Name",
            ["AboutCardNameDesc"] = "Product name",
            ["AboutCardVersion"] = "Version",
            ["AboutCardVersionDesc"] = "Assembly version",
            ["AboutCardFramework"] = "Framework",
            ["AboutCardFrameworkDesc"] = "Runtime + UI stack",
            ["AboutCardPlatform"] = "Platform",
            ["AboutCardPlatformDesc"] = "Supported targets",
            ["AboutCardUpdates"] = "Auto-updates",
            ["AboutCardUpdatesDesc"] = "Installer + delta updates",
            ["AboutCardLogging"] = "Logging",
            ["AboutCardLoggingDesc"] = "File + debugger sinks",
            ["AboutCardMvvm"] = "MVVM",
            ["AboutCardMvvmDesc"] = "Observable properties + commands",
            ["AboutCardUi"] = "UI Controls",
            ["AboutCardUiDesc"] = "Cards, segmented + helpers",
            ["AboutCardLicense"] = "MIT License",
            ["AboutCardLicenseDesc"] = "Free for personal and commercial use.",
            ["AboutViewLicense"] = "View License",
            ["AboutCardSource"] = "Source Code",
            ["AboutCardSourceDesc"] = "Star or fork the template",
            ["AboutGithubShort"] = "GitHub",
            ["AboutCardReleases"] = "Releases",
            ["AboutCardReleasesDesc"] = "Changelog + installers",
            ["AboutViewReleases"] = "View Releases",
            ["AboutCardIssues"] = "Report Issues",
            ["AboutCardIssuesDesc"] = "Bugs + feature requests",
            ["AboutOpenIssue"] = "Open Issue",
            ["SettingsTitle"] = "Settings",
            ["SettingsDescription"] = "Customize the appearance and behavior of the app.",
            ["SettingsAppearance"] = "APPEARANCE",
            ["SettingsTheme"] = "Theme",
            ["SettingsThemeDesc"] = "Choose between light, dark, or system theme.",
            ["SettingsThemeSystem"] = "System",
            ["SettingsThemeLight"] = "Light",
            ["SettingsThemeDark"] = "Dark",
            ["SettingsLanguage"] = "Language",
            ["SettingsLanguageDesc"] = "Choose the display language for the app. Applies instantly.",
            ["SettingsUpdates"] = "UPDATES",
            ["SettingsChannelHeader"] = "Update channel",
            ["SettingsChannelDesc"] = "The channel determines which release feed the app checks for updates.",
            ["SettingsCheckHeader"] = "Check for updates",
            ["SettingsCheckDesc"] = "Manually check for new versions on the selected channel.",
            ["SettingsAutoCheck"] = "Auto-check for updates",
            ["SettingsAutoCheckHeader"] = "Check for updates on startup",
            ["SettingsAutoCheckDesc"] = "Automatically check for updates when the app starts.",
            ["SettingsStatusIdle"] = "No check performed yet.",
            ["SettingsCheckNow"] = "Check now",
            ["SettingsChecking"] = "Checking…",
            ["SettingsInstall"] = "Install",
            ["SettingsDownloading"] = "Downloading…",
            ["SettingsInstalling"] = "Installing…",
            ["SettingsNoUpdate"] = "You are running the latest version.",
            ["SettingsNotInstalled"] = "Updates are only available for installed apps.",
            ["SettingsCheckFailed"] = "Check failed",
            ["SettingsAbout"] = "ABOUT",
            ["SettingsAppVersion"] = "App version",
            ["SettingsRepoHeader"] = "Repository",
            ["SettingsSystemTray"] = "SYSTEM TRAY",
            ["SettingsMinimizeToTray"] = "Minimize to system tray",
            ["SettingsTrayMinimizeDesc"] = "When closed, the app minimizes to the system tray instead of exiting.",
            ["SettingsAutoStart"] = "Start automatically with Windows",
            ["SettingsTrayAutoStartDesc"] = "Launch the app when you sign in to Windows.",
            ["TrayShow"] = "Show DevTem-WinUI 3",
            ["TrayCheckUpdates"] = "Check for updates",
            ["TraySettings"] = "Settings",
            ["TrayExit"] = "Exit",
            ["NotifUpdates"] = "Updates",
            ["FirstRunTitle"] = "Welcome to DevTem-WinUI 3",
            ["FirstRunButton"] = "Get Started",
            ["FirstRunContent"] = "A ready-to-use template for WinUI 3 desktop apps.\n\nThis template includes:\n• Settings with theme selector\n• Auto-updates via GitHub Releases\n• Logging system\n• Desktop shortcut support\n\nGet started by exploring the app!",
        };

        _resources["es-ES"] = new Dictionary<string, string>
        {
            ["AppName"] = "DevTem-WinUI 3",
            ["NavHome"] = "Inicio",
            ["NavAbout"] = "Acerca de",
            ["NavSettings"] = "Configuración",
            ["HomeTitle"] = "Bienvenido a DevTem-WinUI 3",
            ["HomeDescription"] = "Una aplicación moderna de Windows 11 construida con WinUI 3 (.NET 10). Esta plantilla incluye actualizaciones automáticas, registro, inyección de dependencias, base de datos SQLite y más.",
            ["HomeCheckUpdates"] = "Buscar actualizaciones",
            ["HomeOpenSettings"] = "Abrir configuración",
            ["HomeReleasesLink"] = "Cómo funcionan las versiones",
            ["HomeStatusOk"] = "Todos los sistemas funcionan",
            ["HomeQuickActions"] = "ACCIONES RÁPIDAS",
            ["HomeIncluded"] = "INCLUIDO",
            ["HomeFeatUpdatesTitle"] = "Actualizaciones automáticas con Velopack",
            ["HomeFeatUpdatesDesc"] = "Canales Stable / Beta / Dev mediante GitHub Releases con instalación y reinicio en un clic.",
            ["HomeFeatSettingsTitle"] = "Configuración persistente",
            ["HomeFeatSettingsDesc"] = "Tema, idioma, canal de actualizaciones, bandeja e inicio automático: se guardan localmente y se restauran al iniciar.",
            ["HomeFeatDiagnosticsTitle"] = "Diagnóstico integrado",
            ["HomeFeatDiagnosticsDesc"] = "Registros rotativos de Serilog y notificaciones para que las comprobaciones nunca fallen en silencio.",
            ["HomeFeatTrayTitle"] = "Bandeja + shell nativo",
            ["HomeFeatTrayDesc"] = "Fondo Mica, barra de título personalizada y minimizado a la bandeja con accesos a configuración y actualizaciones.",
            ["HomeShipTitle"] = "PUBLICA TU PRIMERA VERSIÓN",
            ["HomeShipBody"] = "1. Sube Version en DevTemWinUi3.csproj  →  2. git tag v0.0.2-beta + push  →  3. CI compila, empaqueta y publica en GitHub Releases.",
            ["HomeShipNote"] = "Los scripts de publicación están en /Scripts. Las comprobaciones solo funcionan en apps instaladas.",
            ["AboutTitle"] = "Acerca de",
            ["AboutSubtitle"] = "Todo sobre DevTem: versión, tecnologías, licencia y enlaces.",
            ["AboutGlance"] = "DE UN VISTAZO",
            ["AboutAppInfo"] = "INFORMACIÓN DE LA APLICACIÓN",
            ["AboutVersion"] = "Versión",
            ["AboutFramework"] = "Framework",
            ["AboutPlatform"] = "Plataforma",
            ["AboutTechnology"] = "TECNOLOGÍA",
            ["AboutLicense"] = "LICENCIA",
            ["AboutLinks"] = "ENLACES",
            ["AboutDescription"] = "Una plantilla lista para producción: estilo WinUI 3 nativo con Mica, configuración persistente, actualizaciones automáticas con Velopack, diagnósticos Serilog y publicación en un comando.",
            ["AboutGithub"] = "Repositorio GitHub",
            ["AboutReportIssue"] = "Informar de un problema",
            ["AboutCardName"] = "Nombre",
            ["AboutCardNameDesc"] = "Nombre del producto",
            ["AboutCardVersion"] = "Versión",
            ["AboutCardVersionDesc"] = "Versión del ensamblado",
            ["AboutCardFramework"] = "Framework",
            ["AboutCardFrameworkDesc"] = "Entorno de ejecución + interfaz",
            ["AboutCardPlatform"] = "Plataforma",
            ["AboutCardPlatformDesc"] = "Destinos compatibles",
            ["AboutCardUpdates"] = "Actualizaciones automáticas",
            ["AboutCardUpdatesDesc"] = "Instalador + actualizaciones delta",
            ["AboutCardLogging"] = "Registro",
            ["AboutCardLoggingDesc"] = "Archivo + depurador",
            ["AboutCardMvvm"] = "MVVM",
            ["AboutCardMvvmDesc"] = "Propiedades observables + comandos",
            ["AboutCardUi"] = "Controles de interfaz",
            ["AboutCardUiDesc"] = "Tarjetas, segmentado + utilidades",
            ["AboutCardLicense"] = "Licencia MIT",
            ["AboutCardLicenseDesc"] = "Gratis para uso personal y comercial.",
            ["AboutViewLicense"] = "Ver licencia",
            ["AboutCardSource"] = "Código fuente",
            ["AboutCardSourceDesc"] = "Marca con estrella o haz un fork",
            ["AboutGithubShort"] = "GitHub",
            ["AboutCardReleases"] = "Versiones",
            ["AboutCardReleasesDesc"] = "Registro de cambios + instaladores",
            ["AboutViewReleases"] = "Ver versiones",
            ["AboutCardIssues"] = "Informar de problemas",
            ["AboutCardIssuesDesc"] = "Errores + peticiones",
            ["AboutOpenIssue"] = "Abrir issue",
            ["SettingsTitle"] = "Configuración",
            ["SettingsDescription"] = "Personaliza la apariencia y el comportamiento de la app.",
            ["SettingsAppearance"] = "APARIENCIA",
            ["SettingsTheme"] = "Tema",
            ["SettingsThemeDesc"] = "Elige entre tema claro, oscuro o del sistema.",
            ["SettingsThemeSystem"] = "Sistema",
            ["SettingsThemeLight"] = "Claro",
            ["SettingsThemeDark"] = "Oscuro",
            ["SettingsLanguage"] = "Idioma",
            ["SettingsLanguageDesc"] = "Elige el idioma de la app. Se aplica al instante.",
            ["SettingsUpdates"] = "ACTUALIZACIONES",
            ["SettingsChannelHeader"] = "Canal de actualización",
            ["SettingsChannelDesc"] = "El canal determina de qué fuente se buscan actualizaciones.",
            ["SettingsCheckHeader"] = "Buscar actualizaciones",
            ["SettingsCheckDesc"] = "Busca manualmente nuevas versiones en el canal seleccionado.",
            ["SettingsAutoCheck"] = "Verificar actualizaciones automáticamente",
            ["SettingsAutoCheckHeader"] = "Buscar actualizaciones al iniciar",
            ["SettingsAutoCheckDesc"] = "Busca actualizaciones automáticamente al iniciar la app.",
            ["SettingsStatusIdle"] = "Aún no se ha comprobado.",
            ["SettingsCheckNow"] = "Verificar ahora",
            ["SettingsChecking"] = "Verificando…",
            ["SettingsInstall"] = "Instalar",
            ["SettingsDownloading"] = "Descargando…",
            ["SettingsInstalling"] = "Instalando…",
            ["SettingsNoUpdate"] = "Estás usando la última versión.",
            ["SettingsNotInstalled"] = "Las actualizaciones solo están disponibles para apps instaladas.",
            ["SettingsCheckFailed"] = "Error al verificar",
            ["SettingsAbout"] = "ACERCA DE",
            ["SettingsAppVersion"] = "Versión de la app",
            ["SettingsRepoHeader"] = "Repositorio",
            ["SettingsSystemTray"] = "BANDEJA DEL SISTEMA",
            ["SettingsMinimizeToTray"] = "Minimizar a la bandeja del sistema",
            ["SettingsTrayMinimizeDesc"] = "Al cerrarse, la app se minimiza a la bandeja en lugar de salir.",
            ["SettingsAutoStart"] = "Iniciar automáticamente con Windows",
            ["SettingsTrayAutoStartDesc"] = "Inicia la app al iniciar sesión en Windows.",
            ["TrayShow"] = "Mostrar DevTem-WinUI 3",
            ["TrayCheckUpdates"] = "Buscar actualizaciones",
            ["TraySettings"] = "Configuración",
            ["TrayExit"] = "Salir",
            ["NotifUpdates"] = "Actualizaciones",
            ["FirstRunTitle"] = "Bienvenido a DevTem-WinUI 3",
            ["FirstRunButton"] = "Empezar",
            ["FirstRunContent"] = "Una plantilla lista para usar apps de escritorio WinUI 3.\n\nIncluye:\n• Configuración con selector de tema\n• Actualizaciones automáticas vía GitHub Releases\n• Sistema de registro\n• Acceso directo de escritorio\n\n¡Empieza explorando la app!",
        };

        _resources["fr-FR"] = new Dictionary<string, string>
        {
            ["AppName"] = "DevTem-WinUI 3",
            ["NavHome"] = "Accueil",
            ["NavAbout"] = "À propos",
            ["NavSettings"] = "Paramètres",
            ["HomeTitle"] = "Bienvenue dans DevTem-WinUI 3",
            ["HomeDescription"] = "Une application moderne Windows 11 construite avec WinUI 3 (.NET 10). Ce modèle inclut les mises à jour automatiques, la journalisation, l'injection de dépendances, la base de données SQLite et plus encore.",
            ["HomeCheckUpdates"] = "Rechercher les mises à jour",
            ["HomeOpenSettings"] = "Ouvrir les paramètres",
            ["HomeReleasesLink"] = "Fonctionnement des versions",
            ["HomeStatusOk"] = "Tous les systèmes fonctionnent",
            ["HomeQuickActions"] = "ACTIONS RAPIDES",
            ["HomeIncluded"] = "INCLUS",
            ["HomeFeatUpdatesTitle"] = "Mises à jour auto avec Velopack",
            ["HomeFeatUpdatesDesc"] = "Canaux Stable / Beta / Dev via GitHub Releases avec installation et redémarrage en un clic.",
            ["HomeFeatSettingsTitle"] = "Paramètres persistants",
            ["HomeFeatSettingsDesc"] = "Thème, langue, canal, zone de notification et démarrage auto — enregistrés localement, restaurés au lancement.",
            ["HomeFeatDiagnosticsTitle"] = "Diagnostic intégré",
            ["HomeFeatDiagnosticsDesc"] = "Journaux rotatifs Serilog et notifications pour ne jamais échouer en silence.",
            ["HomeFeatTrayTitle"] = "Zone de notification + shell natif",
            ["HomeFeatTrayDesc"] = "Fond Mica, barre de titre personnalisée, réduction dans la zone avec accès paramètres et mises à jour.",
            ["HomeShipTitle"] = "PUBLIEZ VOTRE PREMIÈRE VERSION",
            ["HomeShipBody"] = "1. Incrémentez Version dans DevTemWinUi3.csproj  →  2. git tag v0.0.2-beta + push  →  3. la CI compile, empaquette et publie sur GitHub Releases.",
            ["HomeShipNote"] = "Les scripts de publication sont dans /Scripts. La vérification ne marche que dans les apps installées.",
            ["AboutTitle"] = "À propos",
            ["AboutSubtitle"] = "Tout sur DevTem : version, technologies, licence et liens.",
            ["AboutGlance"] = "EN UN COUP D'ŒIL",
            ["AboutAppInfo"] = "INFORMATIONS SUR L'APPLICATION",
            ["AboutVersion"] = "Version",
            ["AboutFramework"] = "Framework",
            ["AboutPlatform"] = "Plateforme",
            ["AboutTechnology"] = "TECHNOLOGIE",
            ["AboutLicense"] = "LICENCE",
            ["AboutLinks"] = "LIENS",
            ["AboutDescription"] = "Un modèle prêt pour la production : style WinUI 3 natif avec Mica, paramètres persistants, mises à jour auto Velopack, diagnostics Serilog et publication en une commande.",
            ["AboutGithub"] = "Dépôt GitHub",
            ["AboutReportIssue"] = "Signaler un problème",
            ["AboutCardName"] = "Nom",
            ["AboutCardNameDesc"] = "Nom du produit",
            ["AboutCardVersion"] = "Version",
            ["AboutCardVersionDesc"] = "Version de l'assembly",
            ["AboutCardFramework"] = "Framework",
            ["AboutCardFrameworkDesc"] = "Runtime + interface",
            ["AboutCardPlatform"] = "Plateforme",
            ["AboutCardPlatformDesc"] = "Cibles prises en charge",
            ["AboutCardUpdates"] = "Mises à jour auto",
            ["AboutCardUpdatesDesc"] = "Installeur + mises à jour delta",
            ["AboutCardLogging"] = "Journalisation",
            ["AboutCardLoggingDesc"] = "Fichier + débogueur",
            ["AboutCardMvvm"] = "MVVM",
            ["AboutCardMvvmDesc"] = "Propriétés observables + commandes",
            ["AboutCardUi"] = "Contrôles UI",
            ["AboutCardUiDesc"] = "Cartes, segmenté + utilitaires",
            ["AboutCardLicense"] = "Licence MIT",
            ["AboutCardLicenseDesc"] = "Gratuit pour usage personnel et commercial.",
            ["AboutViewLicense"] = "Voir la licence",
            ["AboutCardSource"] = "Code source",
            ["AboutCardSourceDesc"] = "Star ou fork du modèle",
            ["AboutGithubShort"] = "GitHub",
            ["AboutCardReleases"] = "Versions",
            ["AboutCardReleasesDesc"] = "Changelog + installeurs",
            ["AboutViewReleases"] = "Voir les versions",
            ["AboutCardIssues"] = "Signaler des problèmes",
            ["AboutCardIssuesDesc"] = "Bugs + demandes",
            ["AboutOpenIssue"] = "Ouvrir un ticket",
            ["SettingsTitle"] = "Paramètres",
            ["SettingsDescription"] = "Personnalisez l'apparence et le comportement de l'app.",
            ["SettingsAppearance"] = "APPARENCE",
            ["SettingsTheme"] = "Thème",
            ["SettingsThemeDesc"] = "Choisissez entre thème clair, sombre ou système.",
            ["SettingsThemeSystem"] = "Système",
            ["SettingsThemeLight"] = "Clair",
            ["SettingsThemeDark"] = "Sombre",
            ["SettingsLanguage"] = "Langue",
            ["SettingsLanguageDesc"] = "Choisissez la langue de l'app. Appliquée instantanément.",
            ["SettingsUpdates"] = "MISES À JOUR",
            ["SettingsChannelHeader"] = "Canal de mise à jour",
            ["SettingsChannelDesc"] = "Le canal détermine le flux vérifié pour les mises à jour.",
            ["SettingsCheckHeader"] = "Rechercher les mises à jour",
            ["SettingsCheckDesc"] = "Recherchez manuellement les nouvelles versions du canal.",
            ["SettingsAutoCheck"] = "Vérifier les mises à jour automatiquement",
            ["SettingsAutoCheckHeader"] = "Vérifier au démarrage",
            ["SettingsAutoCheckDesc"] = "Vérifie automatiquement les mises à jour au démarrage.",
            ["SettingsStatusIdle"] = "Aucune vérification effectuée.",
            ["SettingsCheckNow"] = "Vérifier maintenant",
            ["SettingsChecking"] = "Vérification…",
            ["SettingsInstall"] = "Installer",
            ["SettingsDownloading"] = "Téléchargement…",
            ["SettingsInstalling"] = "Installation…",
            ["SettingsNoUpdate"] = "Vous utilisez la dernière version.",
            ["SettingsNotInstalled"] = "Les mises à jour ne sont disponibles que pour les apps installées.",
            ["SettingsCheckFailed"] = "Échec de la vérification",
            ["SettingsAbout"] = "À PROPOS",
            ["SettingsAppVersion"] = "Version de l'app",
            ["SettingsRepoHeader"] = "Dépôt",
            ["SettingsSystemTray"] = "ZONE DE NOTIFICATION",
            ["SettingsMinimizeToTray"] = "Réduire dans la zone de notification",
            ["SettingsTrayMinimizeDesc"] = "À la fermeture, l'app se réduit dans la zone au lieu de quitter.",
            ["SettingsAutoStart"] = "Démarrer automatiquement avec Windows",
            ["SettingsTrayAutoStartDesc"] = "Lance l'app à la connexion Windows.",
            ["TrayShow"] = "Afficher DevTem-WinUI 3",
            ["TrayCheckUpdates"] = "Rechercher les mises à jour",
            ["TraySettings"] = "Paramètres",
            ["TrayExit"] = "Quitter",
            ["NotifUpdates"] = "Mises à jour",
            ["FirstRunTitle"] = "Bienvenue dans DevTem-WinUI 3",
            ["FirstRunButton"] = "Commencer",
            ["FirstRunContent"] = "Un modèle prêt à l'emploi pour apps de bureau WinUI 3.\n\nInclus :\n• Paramètres avec sélecteur de thème\n• Mises à jour auto via GitHub Releases\n• Système de journalisation\n• Raccourci bureau\n\nExplorez l'app pour commencer !",
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
    /// Sets the UI language for the app. Applies instantly: <see cref="LanguageChanged"/>
    /// notifies open pages, the nav pane, and dialogs to re-apply strings.
    /// </summary>
    public void SetLanguage(string languageTag)
    {
        if (_resources.ContainsKey(languageTag))
        {
            _currentLanguage = languageTag;
            try { ApplicationLanguages.PrimaryLanguageOverride = languageTag; }
            catch { }
            Log.Information("Language changed to: {Language}", languageTag);
            LanguageChanged?.Invoke(this, EventArgs.Empty);
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
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Represents a supported language.
/// </summary>
public record LanguageInfo(string Tag, string NativeName, string EnglishName)
{
    public override string ToString() => NativeName;
}
