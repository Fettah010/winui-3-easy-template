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
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_provider is not null) return;

            var services = new ServiceCollection();

            // Singleton services (process-lifetime objects expose Current;
            // the container owns the registration so there is one composition
            // root instead of scattered news).
            services.AddSingleton<DatabaseService>(DatabaseService.Current);
            services.AddSingleton<WindowStateService>(WindowStateService.Current);
            services.AddSingleton<FirstRunService>(FirstRunService.Current);
            services.AddSingleton<LocalizationService>(LocalizationService.Current);
            services.AddSingleton<AppInfo>(AppInfo.Current);
            services.AddSingleton<ThemeService>(ThemeService.Current);
            services.AddSingleton<UpdateService>(UpdateService.Current);
            services.AddSingleton<IUpdateService>(UpdateService.Current);
            services.AddSingleton<IFilePickerService, FilePickerService>();
            services.AddSingleton<SystemTrayService>(SystemTrayService.Current);
            services.AddSingleton<NavigationService>(NavigationService.Current);
            services.AddSingleton<DesktopToastService>(DesktopToastService.Current);

            // ViewModels are transient: each page gets a fresh instance.
            services.AddTransient<ViewModels.SettingsPageViewModel>();
            services.AddTransient<ViewModels.DiagnosticsPageViewModel>();

            // HTTP client (transient by default)
                    services.AddHttpClient<ApiService>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add("User-Agent", AppMetadata.UserAgent);
                    });

            _provider = services.BuildServiceProvider();
        }
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
