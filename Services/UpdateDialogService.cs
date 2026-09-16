using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Controls;
using DevTemWinUi3.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.Services;

/// <summary>
/// The single update surface (replaces the Update Center page): an animated
/// popup driven by <see cref="UpdateCenterViewModel"/>. Entry points are the
/// Settings check button, the tray menu, the Home quick action (all via the
/// Settings "check" navigation parameter), and the background service
/// (post-download restart prompt, post-detection ask-mode prompt).
/// UI-bound by house rule: every path is never-throw guarded, all public
/// methods marshal to the UI thread and are safe to call headless (no-op
/// before <see cref="Initialize"/>).
/// </summary>
public static class UpdateDialogService
{
    private static Window? _window;
    private static UpdatePopup? _popup;
    private static CancellationTokenSource? _flowCts;
    private static readonly object _gate = new();

    /// <summary>
    /// Wires the service to the main window and its popup overlay. Called
    /// once from <c>MainWindow</c> deferred init. Never throws.
    /// </summary>
    public static void Initialize(Window window, UpdatePopup popup)
    {
        try
        {
            lock (_gate)
            {
                _window = window;
                _popup = popup;
            }
        }
        catch { }
    }

    public static bool IsReady
    {
        get
        {
            try
            {
                lock (_gate)
                {
                    return _window is not null && _popup is not null;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Full user-initiated check flow: checking (animated) → up-to-date |
    /// available → downloading (progress) → ready | error. No-op when the
    /// window is not ready. Never throws.
    /// </summary>
    public static Task ShowCheckAsync() => RunOnUiAsync(ShowCheckCoreAsync);

    /// <summary>
    /// Post-download restart prompt (background auto-install path and the
    /// previous-session prepared update): notes + Restart now / Later.
    /// "Later" keeps the download staged and re-prompts after the next
    /// launch — it never applies silently at startup (Velopack auto-apply
    /// stays off; see Program). Never throws.
    /// </summary>
    public static Task ShowReadyAsync(string? version, string? notes) =>
        RunOnUiAsync(() => ShowReadyCoreAsync(version, notes));

    /// <summary>
    /// Ask-mode prompt from an already-computed check result (background
    /// detection with auto-install off): Install downloads with progress
    /// in-popup, Later dismisses. Never throws.
    /// </summary>
    public static Task ShowAvailableAsync(UpdateCheckResult result) =>
        RunOnUiAsync(() => ShowAvailableCoreAsync(result));

    /// <summary>Closes the popup and cancels any running flow. Never throws.</summary>
    public static Task DismissAsync() => RunOnUiAsync(HideCoreAsync);

    private static Task RunOnUiAsync(Func<Task> core)
    {
        Window? window;
        lock (_gate)
        {
            window = _window;
        }
        if (window is null)
            return Task.CompletedTask;
        try
        {
            var queue = window.DispatcherQueue;
            if (queue is null)
                return Task.CompletedTask;
            if (queue.HasThreadAccess)
                return RunGuardedAsync(core);
            var done = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            if (!queue.TryEnqueue(async () =>
            {
                try
                {
                    await RunGuardedAsync(core).ConfigureAwait(false);
                    done.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    done.TrySetException(ex);
                }
            }))
                return Task.CompletedTask;
            return done.Task;
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    private static async Task RunGuardedAsync(Func<Task> core)
    {
        try
        {
            await core().ConfigureAwait(false);
        }
        catch { }
    }

    private static void CancelFlow()
    {
        try
        {
            CancellationTokenSource? flow;
            lock (_gate)
            {
                flow = _flowCts;
                _flowCts = null;
            }
            if (flow is not null)
            {
                try { flow.Cancel(); } catch { }
                try { flow.Dispose(); } catch { }
            }
        }
        catch { }
    }

    private static CancellationToken NewFlowToken()
    {
        CancelFlow();
        var cts = new CancellationTokenSource();
        lock (_gate)
        {
            _flowCts = cts;
        }
        return cts.Token;
    }

    private static (Window Window, UpdatePopup Popup, LocalizationService Loc)? Snapshot()
    {
        try
        {
            lock (_gate)
            {
                if (_window is null || _popup is null)
                    return null;
                return (_window, _popup, LocalizationService.Current);
            }
        }
        catch
        {
            return null;
        }
    }

    private static IUpdateService? ResolveUpdates()
    {
        try
        {
            return ServiceLocator.GetService<IUpdateService>();
        }
        catch
        {
            return null;
        }
    }

    private static async Task HideCoreAsync()
    {
        CancelFlow();
        UpdatePopup? popup;
        lock (_gate)
        {
            popup = _popup;
        }
        if (popup is not null)
        {
            try { await popup.HideAsync().ConfigureAwait(false); } catch { }
        }
    }

    private static async Task ShowCheckCoreAsync()
    {
        var snap = Snapshot();
        if (snap is null)
            return;
        var (window, popup, loc) = snap.Value;
        _ = window;

        var updates = ResolveUpdates();
        popup.SetTitle(loc.GetString("UpdatePopupTitle"));
        popup.SetNotes(null);

        if (AppFeatures.IsExternallyManaged)
        {
            popup.SetStatus(UpdateCenterViewModel.ExternalHandlerText(loc));
            popup.HideProgress();
            popup.SetPrimary(loc.GetString("UpdateClose"));
            popup.SetSecondary(null);
            EventHandler? close = null;
            close = (_, _) =>
            {
                try { popup.PrimaryPressed -= close; } catch { }
                _ = HideCoreAsync();
            };
            popup.PrimaryPressed += close;
            await popup.ShowAsync().ConfigureAwait(false);
            return;
        }

        if (updates is null)
        {
            popup.SetStatus(loc.GetString("UpdateCenterNoEngine"));
            popup.HideProgress();
            popup.SetPrimary(loc.GetString("UpdateClose"));
            popup.SetSecondary(null);
            EventHandler? close = null;
            close = (_, _) =>
            {
                try { popup.PrimaryPressed -= close; } catch { }
                _ = HideCoreAsync();
            };
            popup.PrimaryPressed += close;
            await popup.ShowAsync().ConfigureAwait(false);
            return;
        }

        var token = NewFlowToken();
        var vm = new UpdateCenterViewModel(updates);
        popup.SetCheckingStatus(TrimEllipsis(loc.GetString("SettingsChecking")));
        popup.SetProgressIndeterminate(true);
        popup.SetPrimary(null);
        popup.SetSecondary(loc.GetString("UpdateCancel"));
        EventHandler? cancel = null;
        cancel = (_, _) =>
        {
            try { popup.SecondaryPressed -= cancel; } catch { }
            CancelFlow();
            _ = HideCoreAsync();
        };
        popup.SecondaryPressed += cancel;
        await popup.ShowAsync().ConfigureAwait(false);

        try
        {
            await vm.CheckAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // The VM never throws; belt and suspenders for the await above.
        }
        try { popup.SecondaryPressed -= cancel; } catch { }
        if (token.IsCancellationRequested)
            return;

        popup.SetProgressIndeterminate(false);
        popup.HideProgress();
        if (vm.LastCheckFailed)
        {
            popup.SetStatus(vm.StatusMessage);
            popup.SetPrimary(loc.GetString("UpdateRetry"));
            popup.SetSecondary(loc.GetString("UpdateClose"));
            EventHandler? retry = null;
            EventHandler? close = null;
            retry = (_, _) =>
            {
                try { popup.PrimaryPressed -= retry; } catch { }
                try { popup.SecondaryPressed -= close; } catch { }
                _ = ShowCheckCoreAsync();
            };
            close = (_, _) =>
            {
                try { popup.PrimaryPressed -= retry; } catch { }
                try { popup.SecondaryPressed -= close; } catch { }
                _ = HideCoreAsync();
            };
            popup.PrimaryPressed += retry;
            popup.SecondaryPressed += close;
            return;
        }
        if (!vm.HasUpdate)
        {
            popup.SetStatus(loc.GetString("SettingsNoUpdate"));
            popup.SetPrimary(loc.GetString("UpdateClose"));
            popup.SetSecondary(null);
            EventHandler? close = null;
            close = (_, _) =>
            {
                try { popup.PrimaryPressed -= close; } catch { }
                _ = HideCoreAsync();
            };
            popup.PrimaryPressed += close;
            return;
        }
        await ShowAvailableStateAsync(popup, vm, loc, token).ConfigureAwait(false);
    }

    private static async Task ShowAvailableCoreAsync(UpdateCheckResult result)
    {
        var snap = Snapshot();
        if (snap is null)
            return;
        var (_, popup, loc) = snap.Value;
        var updates = ResolveUpdates();
        if (updates is null || !result.HasUpdate)
            return;
        var token = NewFlowToken();
        var vm = new UpdateCenterViewModel(updates);
        vm.PublishResult(result);
        popup.SetTitle(loc.GetString("UpdatePopupTitle"));
        await popup.ShowAsync().ConfigureAwait(false);
        await ShowAvailableStateAsync(popup, vm, loc, token).ConfigureAwait(false);
    }

    private static Task ShowAvailableStateAsync(
        UpdatePopup popup, UpdateCenterViewModel vm, LocalizationService loc, CancellationToken token)
    {
        popup.SetStatus(loc.GetString("UpdateAvailableVersion", vm.PendingVersion));
        popup.SetNotes(string.IsNullOrWhiteSpace(vm.ReleaseNotes) ? null : vm.ReleaseNotes);
        popup.HideProgress();
        popup.SetPrimary(loc.GetString("UpdateInstallNow"));
        popup.SetSecondary(loc.GetString("UpdateLater"));
        EventHandler? install = null;
        EventHandler? later = null;
        install = (_, _) =>
        {
            try { popup.PrimaryPressed -= install; } catch { }
            try { popup.SecondaryPressed -= later; } catch { }
            _ = DownloadStateAsync(popup, vm, loc, NewFlowToken());
        };
        later = (_, _) =>
        {
            try { popup.PrimaryPressed -= install; } catch { }
            try { popup.SecondaryPressed -= later; } catch { }
            _ = HideCoreAsync();
        };
        popup.PrimaryPressed += install;
        popup.SecondaryPressed += later;
        _ = token;
        return Task.CompletedTask;
    }

    private static async Task DownloadStateAsync(
        UpdatePopup popup, UpdateCenterViewModel vm, LocalizationService loc, CancellationToken token)
    {
        popup.SetStatus(loc.GetString("SettingsDownloadingProgress", 0));
        popup.SetProgress(0);
        popup.SetPrimary(null);
        popup.SetSecondary(null);
        void OnProgress(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(UpdateCenterViewModel.DownloadProgress))
                {
                    int percent = vm.DownloadProgress;
                    popup.SetProgress(percent);
                    popup.SetStatus(loc.GetString("SettingsDownloadingProgress", percent));
                }
            }
            catch { }
        }
        vm.PropertyChanged += OnProgress;
        try
        {
            await vm.DownloadAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch { }
        finally
        {
            try { vm.PropertyChanged -= OnProgress; } catch { }
        }
        if (token.IsCancellationRequested)
            return;
        popup.HideProgress();
        if (!vm.CanInstall)
        {
            popup.SetStatus(vm.StatusMessage);
            popup.SetPrimary(loc.GetString("UpdateClose"));
            popup.SetSecondary(null);
            EventHandler? close = null;
            close = (_, _) =>
            {
                try { popup.PrimaryPressed -= close; } catch { }
                _ = HideCoreAsync();
            };
            popup.PrimaryPressed += close;
            return;
        }
        await ShowReadyStateAsync(popup, vm, loc).ConfigureAwait(false);
    }

    private static Task ShowReadyCoreAsync(string? version, string? notes)
    {
        var snap = Snapshot();
        if (snap is null)
            return Task.CompletedTask;
        var (_, popup, loc) = snap.Value;
        var updates = ResolveUpdates();
        popup.SetTitle(loc.GetString("UpdatePopupTitle"));
        return ShowReadyCoreInnerAsync(popup, updates, loc, version, notes);
    }

    private static async Task ShowReadyCoreInnerAsync(
        UpdatePopup popup, IUpdateService? updates, LocalizationService loc,
        string? version, string? notes)
    {
        CancelFlow();
        string display = string.IsNullOrWhiteSpace(version)
            ? AppInfo.Current.VersionDisplay
            : version;
        popup.SetStatus(loc.GetString("UpdateRestartTitle", display));
        popup.SetNotes(string.IsNullOrWhiteSpace(notes)
            ? loc.GetString("UpdateRestartBody")
            : notes);
        popup.HideProgress();
        popup.SetPrimary(loc.GetString("UpdateRestartNow"));
        popup.SetSecondary(loc.GetString("UpdateRestartLater"));
        EventHandler? restart = null;
        EventHandler? later = null;
        restart = (_, _) =>
        {
            try { popup.PrimaryPressed -= restart; } catch { }
            try { popup.SecondaryPressed -= later; } catch { }
            _ = HideCoreAsync();
            try { updates?.ApplyPendingUpdateAndRestart(); } catch { }
        };
        later = (_, _) =>
        {
            try { popup.PrimaryPressed -= restart; } catch { }
            try { popup.SecondaryPressed -= later; } catch { }
            _ = HideCoreAsync();
        };
        popup.PrimaryPressed += restart;
        popup.SecondaryPressed += later;
        await popup.ShowAsync().ConfigureAwait(false);
    }

    private static Task ShowReadyStateAsync(
        UpdatePopup popup, UpdateCenterViewModel vm, LocalizationService loc) =>
        ShowReadyCoreInnerAsync(
            popup, ResolveUpdates(), loc,
            string.IsNullOrWhiteSpace(vm.PendingVersion) ? null : vm.PendingVersion,
            string.IsNullOrWhiteSpace(vm.ReleaseNotes) ? null : vm.ReleaseNotes);

    private static string TrimEllipsis(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return text.TrimEnd('…', '.', ' ', '\t');
        }
        catch
        {
            return text ?? string.Empty;
        }
    }
}
