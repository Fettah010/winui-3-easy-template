using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevTemWinUi3.Services;

/// <summary>
/// Implemented by pages that want navigation callbacks without overriding
/// <c>Frame.OnNavigatedTo</c>. The <see cref="NavigationService"/> invokes
/// these for every service-initiated navigation (tray, buttons, deep links),
/// which is the single pattern new pages should follow.
/// </summary>
public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom();
}

public sealed class NavigationService
{
    private readonly Dictionary<string, Type> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();

    // Instance cache honoring each page's NavigationCacheMode (the Frame
    // journal is retired, so the service owns reuse): cached pages resolve
    // to the same instance, transient pages construct fresh every time.
    private readonly Dictionary<string, Page> _pageCache = new(StringComparer.OrdinalIgnoreCase);

    private Frame? _frame;
    private string _currentTag = string.Empty;
    private bool _isNavigating;

    public static NavigationService Current { get; } = new();

    private NavigationService() { }

    public event EventHandler<string>? Navigated;
    public event EventHandler<string>? NavigationFailed;

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
    public string CurrentTag => _currentTag;

    public void RegisterRoute(string tag, Type pageType)
    {
        _routes[tag] = pageType;
    }

    public void SetFrame(Frame frame)
    {
        _frame = frame;
        // The service owns history (the stacks below) and content hosting
        // (ResolvePage): the Frame journal is retired so factory-injected
        // and legacy Activator pages share one path with no divergence.
        try { _frame.IsNavigationStackEnabled = false; } catch { }
        _frame.Navigated += OnFrameNavigated;
    }

    /// <summary>
    /// Resolves the page for a tag: cached instance, factory construction
    /// (constructor-injected pages), or legacy <c>Activator</c> fallback.
    /// UI thread only (page construction is thread-affine). Never throws.
    /// </summary>
    private Page? ResolvePage(string tag)
    {
        try
        {
            if (_pageCache.TryGetValue(tag, out var cached) && cached is not null)
                return cached;
            Page? page = null;
            if (!PageFactory.TryCreate(tag, out var created) || created is null)
            {
                if (_routes.TryGetValue(tag, out var pageType))
                {
                    try { page = Activator.CreateInstance(pageType) as Page; } catch { page = null; }
                }
            }
            else
            {
                page = created;
            }
            if (page is null)
                return null;
            try
            {
                if (page.NavigationCacheMode != NavigationCacheMode.Disabled)
                    _pageCache[tag] = page;
            }
            catch { }
            return page;
        }
        catch
        {
            return null;
        }
    }

    public bool NavigateTo(string tag, object? parameter = null, bool addToHistory = true)
    {
        if (_frame is null || _isNavigating)
            return false;

        if (!_routes.TryGetValue(tag, out _))
        {
            NavigationFailed?.Invoke(this, tag);
            return false;
        }

        // Store parameter for the target page to retrieve
        if (parameter is not null)
            _parameters[tag] = parameter;

        try
        {
            _isNavigating = true;

            if (addToHistory && !string.IsNullOrEmpty(_currentTag))
            {
                _backStack.Push(new NavigationEntry(_currentTag, null));
                _forwardStack.Clear();
            }

            (_frame.Content as INavigationAware)?.OnNavigatedFrom();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var page = ResolvePage(tag);
            if (page is null)
            {
                NavigationFailed?.Invoke(this, tag);
                return false;
            }
            _frame.Content = page;
            watch.Stop();
            _currentTag = tag;
            Navigated?.Invoke(this, tag);
            (page as INavigationAware)?.OnNavigatedTo(parameter);
            try
            {
                // P2-3: count + time every navigation (last-value per
                // page feeds the diagnostics nav-timing display).
                Diagnostics.AppMetrics.RecordNavigation(tag, watch.Elapsed.TotalMilliseconds);
                CrashReportingService.AddBreadcrumb("Navigate: " + tag, "navigation");
            }
            catch { }
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Navigation to {Tag} failed", tag);
            NavigationFailed?.Invoke(this, tag);
            return false;
        }
        finally
        {
            _isNavigating = false;
        }
    }

    public bool GoBack()
    {
        if (!CanGoBack || _frame is null || _isNavigating)
            return false;

        var entry = _backStack.Pop();
        _forwardStack.Push(new NavigationEntry(_currentTag, null));

        _isNavigating = true;
        try
        {
            var page = ResolvePage(entry.Tag);
            if (page is null)
                return false;
            (_frame.Content as INavigationAware)?.OnNavigatedFrom();
            _frame.Content = page;
            _currentTag = entry.Tag;
            Navigated?.Invoke(this, entry.Tag);
            (page as INavigationAware)?.OnNavigatedTo(null);
            return true;
        }
        finally
        {
            _isNavigating = false;
        }
    }

    public bool GoForward()
    {
        if (!CanGoForward || _frame is null || _isNavigating)
            return false;

        var entry = _forwardStack.Pop();
        _backStack.Push(new NavigationEntry(_currentTag, null));

        _isNavigating = true;
        try
        {
            var page = ResolvePage(entry.Tag);
            if (page is null)
                return false;
            (_frame.Content as INavigationAware)?.OnNavigatedFrom();
            _frame.Content = page;
            _currentTag = entry.Tag;
            Navigated?.Invoke(this, entry.Tag);
            (page as INavigationAware)?.OnNavigatedTo(null);
            return true;
        }
        finally
        {
            _isNavigating = false;
        }
    }

    public T? GetParameter<T>(string tag)
    {
        if (_parameters.TryGetValue(tag, out var value) && value is T typed)
            return typed;
        return default;
    }

    public bool TryNavigateByUri(string uri, out string tag)
    {
        tag = string.Empty;

        if (string.IsNullOrWhiteSpace(uri))
            return false;

        // Deep links (devtem://settings, devtem:///home?theme=dark) parse to
        // a tag; anything else falls back to a plain tag (legacy callers).
        if (ProtocolService.TryParseRoute(uri, out tag))
            return NavigateTo(tag);

        var fallback = uri.Trim();
        var queryIndex = fallback.IndexOf('?');
        if (queryIndex >= 0)
            fallback = fallback[..queryIndex];
        fallback = fallback.Trim('/');
        tag = fallback;
        return NavigateTo(tag);
    }

    private void OnFrameNavigated(object sender, object e)
    {
        // Sync handled externally by MainWindow
    }

    private record NavigationEntry(string Tag, object? Parameter);
}
