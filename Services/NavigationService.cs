using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.Services;

public sealed class NavigationService
{
    private readonly Dictionary<string, Type> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();

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
        _frame.Navigated += OnFrameNavigated;
    }

    public bool NavigateTo(string tag, object? parameter = null, bool addToHistory = true)
    {
        if (_frame is null || _isNavigating)
            return false;

        if (!_routes.TryGetValue(tag, out var pageType))
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

            var success = _frame.Navigate(pageType, parameter);
            if (success)
            {
                _currentTag = tag;
                Navigated?.Invoke(this, tag);
            }
            return success;
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Navigation to {Tag} failed", tag);
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
            _frame.GoBack();
            _currentTag = entry.Tag;
            Navigated?.Invoke(this, entry.Tag);
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
            _frame.GoForward();
            _currentTag = entry.Tag;
            Navigated?.Invoke(this, entry.Tag);
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

        // Support formats:
        //   devtem://settings
        //   devtem:///settings
        //   devtem://home?theme=dark
        var cleanUri = uri.Replace("devtem://", "").Replace("devtem:///", "").Trim('/');
        var queryIndex = cleanUri.IndexOf('?');
        if (queryIndex >= 0)
            cleanUri = cleanUri[..queryIndex];

        tag = cleanUri;
        return NavigateTo(tag);
    }

    private void OnFrameNavigated(object sender, object e)
    {
        // Sync handled externally by MainWindow
    }

    private record NavigationEntry(string Tag, object? Parameter);
}
