using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DevTemWinUi3.Services;

/// <summary>
/// Provides dependency injection for the application.
/// Register services here and resolve them via GetService or GetRequiredService.
/// Uses a bare <see cref="ServiceCollection"/> on purpose: the generic host
/// (CreateDefaultBuilder) costs seconds at startup for configuration/logging
/// plumbing this app never uses.
/// </summary>
public static class ServiceLocator
{
    private static ServiceProvider? _provider;
    private static readonly object _lock = new();

    public static IServiceProvider Services
    {
        get
        {
            if (_provider is null)
                throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
            return _provider;
        }
    }

    /// <summary>
    /// Initializes the DI container. Call once at startup (in App.xaml.cs or Program.cs).
    /// Composition root with feature modules (performance plan P2-1): each
    /// <c>Add*</c> method owns one area, so a new service touches exactly
    /// one module. Ownership rule (see <c>docs/DECISIONS.md</c>):
    /// process-lifetime instances are owned by their <c>Current</c>
    /// accessor (the container only references them); type-registered
    /// services are container-owned and disposed with <see cref="Shutdown"/>.
    /// No generic host by design (see the class doc).
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_provider is not null) return;

            var services = new ServiceCollection();

            // <devtem:services>
            // Register application-owned services here. Keep template
            // infrastructure registrations below this marker unchanged.
            // </devtem:services>

            AddData(services);
            AddUpdates(services);
            AddPresence(services);
            AddCore(services);
            AddHttp(services);

            _provider = services.BuildServiceProvider();
        }
    }

    /// <summary>Persistence module: the local database.</summary>
    private static void AddData(ServiceCollection services)
    {
        // Singleton services (process-lifetime objects expose Current;
        // the container owns the registration so there is one composition
        // root instead of scattered news).
#if (database)
        services.AddSingleton<DatabaseService>(DatabaseService.Current);
#endif
    }

    /// <summary>Updates module: whichever engine the app was built with.</summary>
    private static void AddUpdates(ServiceCollection services)
    {
#if (updates == 'velopack')
        services.AddSingleton<UpdateService>(UpdateService.Current);
        services.AddSingleton<IUpdateService>(UpdateService.Current);
#endif
#if (updates == 'basic')
        services.AddSingleton<BasicGithubUpdateService>(BasicGithubUpdateService.Current);
        services.AddSingleton<IUpdateService>(BasicGithubUpdateService.Current);
#endif
    }

    /// <summary>Presence module: tray icon and OS toasts.</summary>
    private static void AddPresence(ServiceCollection services)
    {
#if (tray)
        services.AddSingleton<SystemTrayService>(SystemTrayService.Current);
#endif
#if (tray)
        services.AddSingleton<DesktopToastService>(DesktopToastService.Current);
#endif
    }

    /// <summary>Core module: state, navigation, and page view models.</summary>
    private static void AddCore(ServiceCollection services)
    {
        services.AddSingleton<WindowStateService>(WindowStateService.Current);
        services.AddSingleton<FirstRunService>(FirstRunService.Current);
        services.AddSingleton<LocalizationService>(LocalizationService.Current);
        services.AddSingleton<AppInfo>(AppInfo.Current);
        services.AddSingleton<ThemeService>(ThemeService.Current);
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<NavigationService>(NavigationService.Current);

        // ViewModels are transient: each page gets a fresh instance.
        // UpdateCenterViewModel is unconditional: the page ships in
        // every combo and the VM is null-tolerant without an engine.
        services.AddTransient<ViewModels.SettingsPageViewModel>();
        services.AddTransient<ViewModels.UpdateCenterViewModel>();
#if (setup)
        services.AddTransient<ViewModels.SetupWizardViewModel>();
#endif
#if (health)
        services.AddTransient<ViewModels.DiagnosticsPageViewModel>();
#endif
    }

    /// <summary>HTTP module: typed clients with the resilience kit (P2-2).</summary>
    private static void AddHttp(ServiceCollection services)
    {
#if (http)
        services.AddTransient<ExponentialRetryHandler>();
        // HTTP client (transient by default)
        services.AddHttpClient<ApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", AppMetadata.UserAgent);
        }).AddHttpMessageHandler<ExponentialRetryHandler>();
#endif
    }

    /// <summary>
    /// Gets a service from the container.
    /// </summary>
    public static T? GetService<T>() where T : class
    {
        return Services.GetService<T>();
    }

    /// <summary>
    /// Gets a required service from the container.
    /// </summary>
    public static T GetRequiredService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Shuts down the container and disposes all services.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            _provider?.Dispose();
            _provider = null;
        }
    }
}
