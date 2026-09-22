using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.Services;

/// <summary>
/// Route-to-page factories (Advancement C1). WinUI instantiates navigated
/// pages via <c>Activator</c>, which forces parameterless constructors and
/// pushes view-model resolution into page code-behind
/// (<c>ServiceLocator.GetRequiredService</c> at click time). Registered
/// routes construct through their factory instead, so migrated pages take
/// their view model as a constructor parameter and
/// <see cref="NavigationService"/> never news a page. Migration is
/// per-route and additive: unregistered routes keep the legacy
/// <c>Activator</c> path, so pages migrate one by one with no flag day.
/// Registrations live in <c>ServiceLocator.AddCore</c> next to the view
/// models they inject. Factories run on the UI thread (page construction
/// is thread-affine); the registry itself never throws.
/// </summary>
public static class PageFactory
{
    private static readonly Dictionary<string, Func<Page>> _factories =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _gate = new();

    /// <summary>
    /// Registers the factory for a route. Re-registering replaces the
    /// previous factory (last write wins; tests rely on this).
    /// </summary>
    public static void Register(string route, Func<Page> factory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(route) || factory is null)
                return;
            lock (_gate)
            {
                _factories[route] = factory;
            }
        }
        catch { }
    }

    /// <summary>Whether a factory is registered for the route.</summary>
    public static bool IsRegistered(string route)
    {
        try
        {
            lock (_gate)
            {
                return !string.IsNullOrWhiteSpace(route) && _factories.ContainsKey(route);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the factory WITHOUT invoking it (headless-safe: constructing
    /// a <c>Page</c> needs the UI thread). The navigation service invokes
    /// the factory on the UI thread at navigation time.
    /// </summary>
    public static bool TryGetFactory(string route, out Func<Page>? factory)
    {
        factory = null;
        try
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;
            lock (_gate)
            {
                return _factories.TryGetValue(route, out factory);
            }
        }
        catch
        {
            factory = null;
            return false;
        }
    }

    /// <summary>
    /// Constructs the page for a registered route. UI thread only (page
    /// construction is thread-affine). Returns false when unregistered or
    /// when the factory throws (the caller falls back to the legacy path).
    /// Never throws.
    /// </summary>
    public static bool TryCreate(string route, out Page? page)
    {
        page = null;
        try
        {
            if (!TryGetFactory(route, out var factory) || factory is null)
                return false;
            page = factory();
            return page is not null;
        }
        catch
        {
            page = null;
            return false;
        }
    }

    internal static void ResetForTests()
    {
        try
        {
            lock (_gate)
            {
                _factories.Clear();
            }
        }
        catch { }
    }
}
