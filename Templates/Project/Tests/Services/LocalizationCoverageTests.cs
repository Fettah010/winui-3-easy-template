using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LocalizationCoverageTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Never leak a test language into other tests.
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void NewHomeKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Buscar actualizaciones", loc.GetString("HomeCheckUpdates"));
        Assert.AreEqual("ACCIONES RÁPIDAS", loc.GetString("HomeQuickActions"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Rechercher les mises à jour", loc.GetString("HomeCheckUpdates"));
        Assert.AreEqual("ACTIONS RAPIDES", loc.GetString("HomeQuickActions"));
    }

    [TestMethod]
    public void NewAboutKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("DE UN VISTAZO", loc.GetString("AboutGlance"));
        Assert.AreEqual("Ver licencia", loc.GetString("AboutViewLicense"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("EN UN COUP D'ŒIL", loc.GetString("AboutGlance"));
        Assert.AreEqual("Voir la licence", loc.GetString("AboutViewLicense"));
    }

    [TestMethod]
    public void NewSettingsAndTrayKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Canal de actualización", loc.GetString("SettingsChannelHeader"));
        Assert.AreEqual("Buscar actualizaciones", loc.GetString("TrayCheckUpdates"));
        Assert.AreEqual("Salir", loc.GetString("TrayExit"));
        Assert.AreEqual("Las instalaciones empaquetadas (MSIX) gestionan el inicio en Configuración de Windows.", loc.GetString("SettingsTrayPackagedNote"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Canal de mise à jour", loc.GetString("SettingsChannelHeader"));
        Assert.AreEqual("Rechercher les mises à jour", loc.GetString("TrayCheckUpdates"));
        Assert.AreEqual("Quitter", loc.GetString("TrayExit"));
        Assert.AreEqual("Les installations packagées (MSIX) gèrent le démarrage dans les paramètres Windows.", loc.GetString("SettingsTrayPackagedNote"));
    }

    [TestMethod]
    public void NewBackupKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("RESPALDO", loc.GetString("SettingsBackup"));
        Assert.AreEqual("Restablecer, exportar, importar", loc.GetString("SettingsBackupHeader"));
        Assert.AreEqual("Restablecer", loc.GetString("SettingsReset"));
        Assert.AreEqual("Configuración importada.", loc.GetString("SettingsBackupImportDone"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("SAUVEGARDE", loc.GetString("SettingsBackup"));
        Assert.AreEqual("Réinitialiser, exporter, importer", loc.GetString("SettingsBackupHeader"));
        Assert.AreEqual("Réinitialiser", loc.GetString("SettingsReset"));
        Assert.AreEqual("Paramètres importés.", loc.GetString("SettingsBackupImportDone"));
    }

    [TestMethod]
    public void NewDiagnosticsKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Diagnóstico", loc.GetString("NavDiagnostics"));
        Assert.AreEqual("Actualizar", loc.GetString("DiagnosticsRefresh"));
        Assert.AreEqual("Aún no hay archivos de registro.", loc.GetString("DiagnosticsNoLogs"));
        Assert.AreEqual("Buscar en los registros…", loc.GetString("DiagnosticsSearchPlaceholder"));
        Assert.AreEqual("Todos los niveles", loc.GetString("DiagnosticsLevelAll"));
        Assert.AreEqual("Copiar", loc.GetString("DiagnosticsCopy"));
        Assert.AreEqual("Borrar", loc.GetString("DiagnosticsClear"));
        Assert.AreEqual("Ninguna línea coincide con el filtro.", loc.GetString("DiagnosticsNoMatch"));
        Assert.AreEqual("1 de 4 líneas", loc.GetString("DiagnosticsLines", 1, 4));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Diagnostic", loc.GetString("NavDiagnostics"));
        Assert.AreEqual("Actualiser", loc.GetString("DiagnosticsRefresh"));
        Assert.AreEqual("Aucun journal pour l'instant.", loc.GetString("DiagnosticsNoLogs"));
        Assert.AreEqual("Rechercher dans les journaux…", loc.GetString("DiagnosticsSearchPlaceholder"));
        Assert.AreEqual("Tous les niveaux", loc.GetString("DiagnosticsLevelAll"));
        Assert.AreEqual("Copier", loc.GetString("DiagnosticsCopy"));
        Assert.AreEqual("Effacer", loc.GetString("DiagnosticsClear"));
        Assert.AreEqual("Aucune ligne ne correspond au filtre.", loc.GetString("DiagnosticsNoMatch"));
        Assert.AreEqual("1 sur 4 lignes", loc.GetString("DiagnosticsLines", 1, 4));

        loc.SetLanguage("en-US");
        Assert.AreEqual("Search logs…", loc.GetString("DiagnosticsSearchPlaceholder"));
        Assert.AreEqual("1 of 4 lines", loc.GetString("DiagnosticsLines", 1, 4));
        Assert.AreEqual("Startup time", loc.GetString("DiagnosticsStartup"));
        Assert.AreEqual("Log level", loc.GetString("DiagnosticsLogLevel"));
        Assert.AreEqual("Unexpected error", loc.GetString("StartupCrashTitle"));
    }

    [TestMethod]
    public void NewStartupCrashKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Error inesperado", loc.GetString("StartupCrashTitle"));
        Assert.AreEqual(
            "La aplicación se cerró inesperadamente.\n\nRegistros: C:\\logs",
            loc.GetString("StartupCrashBody", "C:\\logs"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Erreur inattendue", loc.GetString("StartupCrashTitle"));
        Assert.AreEqual(
            "L'application s'est fermée de manière inattendue.\n\nJournaux : C:\\logs",
            loc.GetString("StartupCrashBody", "C:\\logs"));
    }

    [TestMethod]
    public void NewPhase3Keys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("Crash reports", loc.GetString("DiagnosticsCrashReports"));
        Assert.AreEqual("METRICS", loc.GetString("DiagnosticsMetrics"));
        Assert.AreEqual("Navigations", loc.GetString("DiagnosticsNavigations"));
        Assert.AreEqual("Update checks", loc.GetString("DiagnosticsUpdateChecks"));
        Assert.AreEqual("Exceptions", loc.GetString("DiagnosticsExceptions"));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Informes de errores", loc.GetString("DiagnosticsCrashReports"));
        Assert.AreEqual("MÉTRICAS", loc.GetString("DiagnosticsMetrics"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Rapports d'erreurs", loc.GetString("DiagnosticsCrashReports"));
        Assert.AreEqual("MÉTRIQUES", loc.GetString("DiagnosticsMetrics"));
    }

    [TestMethod]
    public void NewPhase2Keys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("File", loc.GetString("DiagnosticsViewFile"));
        Assert.AreEqual("Live", loc.GetString("DiagnosticsViewLive"));
        Assert.AreEqual("Export bundle", loc.GetString("DiagnosticsExportBundle"));
        Assert.AreEqual("Close", loc.GetString("DiagnosticsClose"));
        Assert.AreEqual("None", loc.GetString("DiagnosticsNone"));
        Assert.AreEqual("Yes", loc.GetString("DiagnosticsYes"));
        Assert.AreEqual("2 of 9 events", loc.GetString("DiagnosticsEventsCount", 2, 9));
        Assert.AreEqual("3 new", loc.GetString("DiagnosticsNewEvents", 3));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Archivo", loc.GetString("DiagnosticsViewFile"));
        Assert.AreEqual("En vivo", loc.GetString("DiagnosticsViewLive"));
        Assert.AreEqual("Cerrar", loc.GetString("DiagnosticsClose"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Fichier", loc.GetString("DiagnosticsViewFile"));
        Assert.AreEqual("En direct", loc.GetString("DiagnosticsViewLive"));
        Assert.AreEqual("Fermer", loc.GetString("DiagnosticsClose"));
    }

    [TestMethod]
    public void NewVerboseKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("Verbose logging", loc.GetString("DiagnosticsVerbose"));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Registro detallado", loc.GetString("DiagnosticsVerbose"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Journalisation détaillée", loc.GetString("DiagnosticsVerbose"));
    }

    [TestMethod]
    public void SetLanguage_RaisesLanguageChanged()
    {
        var loc = LocalizationService.Current;
        int raised = 0;
        void Handler(object? s, System.EventArgs e) => raised++;

        loc.LanguageChanged += Handler;
        try
        {
            loc.SetLanguage("fr-FR");
            Assert.AreEqual(1, raised);
            Assert.AreEqual("fr-FR", loc.CurrentLanguage);
        }
        finally
        {
            loc.LanguageChanged -= Handler;
        }
    }

    [TestMethod]
    public void DownloadProgress_FormatWorksInAllLanguages()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("Downloading… 42%", loc.GetString("SettingsDownloadingProgress", 42));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Descargando… 42%", loc.GetString("SettingsDownloadingProgress", 42));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Téléchargement… 42%", loc.GetString("SettingsDownloadingProgress", 42));
    }

    [TestMethod]
    public void SetLanguage_UnknownTag_KeepsCurrentLanguage()
    {
        var loc = LocalizationService.Current;
        loc.SetLanguage("es-ES");
        loc.SetLanguage("xx-XX");
        Assert.AreEqual("es-ES", loc.CurrentLanguage);
    }

    [TestMethod]
    public void SetLanguage_PersistsChoice_ForNextLaunch()
    {
        var loc = LocalizationService.Current;
        loc.SetLanguage("fr-FR");

        // A fresh read (as Initialize does on next launch) sees the choice.
        string? persisted = LocalSettingsStore.Shared.Get<string?>("AppLanguage", null);
        Assert.AreEqual("fr-FR", persisted);
    }

    [TestMethod]
    public void ShipBody_FormatsCurrentVersion_ThroughSlot()
    {
        var loc = LocalizationService.Current;
        loc.SetLanguage("en-US");
        // ShipBody must flow through the {0} slot — never a hardcoded string.
        Assert.AreEqual(
            loc.GetString("HomeShipBody", AppInfo.Current.Version),
            loc.ShipBody);
        Assert.Contains(AppInfo.Current.Version, loc.ShipBody);
    }

    [TestMethod]
    public void NewUpdateFlowKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("v9.9 available", loc.GetString("UpdateAvailableVersion", "9.9"));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("v9.9 disponible", loc.GetString("UpdateAvailableVersion", "9.9"));
        Assert.AreEqual("Actualización descargada. Reiniciando…", loc.GetString("UpdateDownloadedRestart"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("v9.9 disponible", loc.GetString("UpdateAvailableVersion", "9.9"));
        Assert.AreEqual("Mise à jour téléchargée. Redémarrage…", loc.GetString("UpdateDownloadedRestart"));
    }

    [TestMethod]
    public void NewRestartPromptKeys_AreTranslated()
    {        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("Update v9.9 ready", loc.GetString("UpdateRestartTitle", "9.9"));
        Assert.AreEqual("Later", loc.GetString("UpdateRestartLater"));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Actualización v9.9 lista", loc.GetString("UpdateRestartTitle", "9.9"));
        Assert.AreEqual("Más tarde", loc.GetString("UpdateRestartLater"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Mise à jour v9.9 prête", loc.GetString("UpdateRestartTitle", "9.9"));
        Assert.AreEqual("Plus tard", loc.GetString("UpdateRestartLater"));
    }

    [TestMethod]
    public void NewP3Keys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("Updates", loc.GetString("NavUpdates"));
        Assert.AreEqual("Update Center", loc.GetString("UpdateCenterTitle"));
        Assert.AreEqual("Download", loc.GetString("UpdateCenterDownload"));
        Assert.AreEqual("Release notes", loc.GetString("UpdateCenterNotesHeader"));
        Assert.AreEqual("Open Update Center", loc.GetString("SettingsOpenUpdateCenter"));
        Assert.AreEqual("View details", loc.GetString("UpdateDetails"));
        Assert.AreEqual("Setup", loc.GetString("SetupWizardTitle"));
        Assert.AreEqual("Finish setup", loc.GetString("SetupWizardComplete"));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Actualizaciones", loc.GetString("NavUpdates"));
        Assert.AreEqual("Centro de actualizaciones", loc.GetString("UpdateCenterTitle"));
        Assert.AreEqual("Descargar", loc.GetString("UpdateCenterDownload"));
        Assert.AreEqual("Ver detalles", loc.GetString("UpdateDetails"));
        Assert.AreEqual("Terminar", loc.GetString("SetupWizardComplete"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Mises à jour", loc.GetString("NavUpdates"));
        Assert.AreEqual("Centre de mise à jour", loc.GetString("UpdateCenterTitle"));
        Assert.AreEqual("Télécharger", loc.GetString("UpdateCenterDownload"));
        Assert.AreEqual("Voir les détails", loc.GetString("UpdateDetails"));
        Assert.AreEqual("Terminer", loc.GetString("SetupWizardComplete"));
    }

    [TestMethod]
    public void Dictionaries_ContainNoHardcodedVersions()
    {        var versionLike = new System.Text.RegularExpressions.Regex(@"\bv\d+\.\d+");
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var value in LocalizationService.GetAllValues())
        {
            if (versionLike.IsMatch(value))
                offenders.Add(value);
        }
        Assert.IsEmpty(offenders,
            "Hardcoded versions in loc files: " + string.Join(" | ", offenders));
    }
}
