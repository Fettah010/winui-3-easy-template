using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevTemWinUi3.Services;

/// <summary>
/// Provides dependency injection for the application.
/// Register services here and resolve them via GetService or GetRequiredService.
/// </summary>
public static class ServiceLocator
{
    private static IHost? _host;
    private static readonly object _lock = new();

    public static IServiceProvider Services
    {
        get
        {
            if (_host is null)
                throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
            return _host.Services;
        }
    }

    /// <summary>
    /// Initializes the DI container. Call once at startup (in App.xaml.cs or Program.cs).
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_host is not null) return;

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    // Singleton services
                    services.AddSingleton<DatabaseService>(DatabaseService.Current);
                    services.AddSingleton<WindowStateService>(WindowStateService.Current);
                    services.AddSingleton<FirstRunService>(FirstRunService.Current);

                    // HTTP client (transient by default)
                    services.AddHttpClient<ApiService>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add("User-Agent", "DevTem-WinUI3/1.0");
                    });
                })
                .Build();
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
    /// Shuts down the host and disposes all services.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            _host?.Dispose();
            _host = null;
        }
    }
}
