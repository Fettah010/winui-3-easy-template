using System;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Controls;
using DevTemWinUi3.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.Services;

/// <summary>
/// The single update surface: lightweight <see cref="NotificationService"/>
/// toasts for transient states (checking, up-to-date, errors) plus native
/// WinUI 3 <see cref="ContentDialog"/>s for states that need a decision
/// (available → install, downloaded → restart). Entry points are the
/// Settings check button, the tray menu, the Home quick action (all via the
/// Settings "check" navigation parameter), and the background service
/// (post-download restart prompt, post-detection ask-mode prompt).
/// UI-bound by house rule: every path is never-throw guarded, all public
/// methods marshal to the UI thread and are safe to call headless (no-op
/// before <see cref="Initialize"/>). Checks always terminate: an
/// unpackaged run short-circuits (nothing to update) and network checks
/// race a timeout, so the "checking forever" state cannot happen.
/// </summary>
public static class UpdateDialogService
{
    private static Window? _window;

    /// <summary>
    /// Legacy overlay, kept wired so existing XAML still initializes. The
    /// new flow never shows it: toasts + <see cref="ContentDialog"/> carry
    /// every state. Retained (hidden) for template parity and smoke-test
    /// stability.
    /// </summary>
    private static UpdatePopup? _popup;
    private static CancellationTokenSource? _flowCts;
    private static ContentDialog? _activeDialog;
    private static readonly object _gate = new();

    /// <summary>
    /// How long a manual check waits for the feed before reporting failure.
    /// Never infinite: the timeout guarantees the checking state always
    /// resolves to a toast or a dialog.
    /// </summary>
    internal static TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Wires the service to the main window (and its legacy popup overlay).
    /// Called once from <c>MainWindow</c> deferred init. Never throws.
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
                    return _window is not null;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Full user-initiated check flow: toast (checking) → toast
    /// (up-to-date) | dialog (available → downloading → ready) | toast or
    /// dialog (error). No-op when the window is not ready. Never throws.
    /// </summary>
    public static Task ShowCheckAsync() => RunOnUiAsync(ShowCheckCoreAsync);

    /// <summary>
    /// Post-download restart prompt (background auto-install path and the
    /// previous-session prepared update): notes + Restart now / Later.
    /// "Later" keeps the download staged and re-prompts after the next
    /// launch — it never applies silently at startup. Never throws.
    /// </summary>
    public static Task ShowReadyAsync(string? version, string? notes) =>
        RunOnUiAsync(() => ShowReadyCoreAsync(version, notes));

    /// <summary>
    /// Ask-mode prompt from an already-computed check result (background
    /// detection with auto-install off): Install downloads with progress
    /// in-dialog, Later dismisses. Never throws.
    /// </summary>
    public static Task ShowAvailableAsync(UpdateCheckResult result) =>
        RunOnUiAsync(() => ShowAvailableCoreAsync(result));

    /// <summary>Closes any update UI and cancels any running flow. Never throws.</summary>
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
            ContentDialog? active;
            lock (_gate)
            {
                flow = _flowCts;
                _flowCts = null;
                active = _activeDialog;
                _activeDialog = null;
            }
            if (flow is not null)
            {
                try { flow.Cancel(); } catch { }
                try { flow.Dispose(); } catch { }
            }
            if (active is not null)
            {
                try { active.Hide(); } catch { }
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

    private static (Window Window, LocalizationService Loc)? Snapshot()
    {
        try
        {
            lock (_gate)
            {
                if (_window is null)
                    return null;
                return (_window, LocalizationService.Current);
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

    private static void ToastInfo(string title, string message)
    {
        try { NotificationService.Current.Info(title, message); } catch { }
    }

    private static void ToastSuccess(string title, string message)
    {
        try { NotificationService.Current.Success(title, message); } catch { }
    }

    private static void ToastError(string title, string message)
    {
        try { NotificationService.Current.Error(title, message); } catch { }
    }

    private static async Task ShowCheckCoreAsync()
    {
        var snap = Snapshot();
        if (snap is null)
            return;
        var (window, loc) = snap.Value;
        string title = loc.GetString("UpdatePopupTitle");

        var updates = ResolveUpdates();

        if (AppFeatures.IsExternallyManaged)
        {
            ToastInfo(title, UpdateCenterViewModel.ExternalHandlerText(loc));
            return;
        }

        if (updates is null)
        {
            ToastError(title, loc.GetString("UpdateCenterNoEngine"));
            return;
        }

        var token = NewFlowToken();
        TimeSpan timeout;
        lock (_gate)
        {
            timeout = CheckTimeout;
        }

        if (token.IsCancellationRequested)
            return;

        // Unpackaged runs (dotnet run / loose exe) have nothing to update:
        // say so immediately instead of polling the feed forever.
        try
        {
            if (!updates.IsInstalled)
            {
                ToastInfo(title, loc.GetString("SettingsNotInstalled"));
                return;
            }
        }
        catch { }

        ToastInfo(title, TrimEllipsis(loc.GetString("SettingsChecking")));

        var vm = new UpdateCenterViewModel(updates);
        try
        {
            var checkTask = vm.CheckAsync(token);
            var timeoutTask = Task.Delay(timeout, token);
            var finished = await Task.WhenAny(checkTask, timeoutTask).ConfigureAwait(false);
            if (finished != checkTask)
            {
                ToastError(title, loc.GetString("SettingsCheckFailed"));
                return;
            }
            try { await checkTask.ConfigureAwait(false); } catch { }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            // The VM never throws; belt and suspenders for the race above.
        }
        if (token.IsCancellationRequested)
            return;

        if (vm.LastCheckFailed)
        {
            await ShowErrorDialogAsync(window, loc, title, vm.StatusMessage).ConfigureAwait(false);
            return;
        }
        if (!vm.HasUpdate)
        {
            ToastSuccess(title, loc.GetString("SettingsNoUpdate"));
            return;
        }
        await ShowAvailableDialogAsync(window, loc, title, vm).ConfigureAwait(false);
    }

    private static async Task ShowAvailableCoreAsync(UpdateCheckResult result)
    {
        var snap = Snapshot();
        if (snap is null)
            return;
        var (window, loc) = snap.Value;
        var updates = ResolveUpdates();
        if (updates is null || !result.HasUpdate)
            return;
        string title = loc.GetString("UpdatePopupTitle");
        var vm = new UpdateCenterViewModel(updates);
        vm.PublishResult(result);
        await ShowAvailableDialogAsync(window, loc, title, vm).ConfigureAwait(false);
    }

    private static async Task ShowAvailableDialogAsync(
        Window window, LocalizationService loc, string title, UpdateCenterViewModel vm)
    {
        string version = string.IsNullOrWhiteSpace(vm.PendingVersion)
            ? AppInfo.Current.VersionDisplay
            : vm.PendingVersion;
        string status = loc.GetString("UpdateAvailableVersion", version);
        string? notes = string.IsNullOrWhiteSpace(vm.ReleaseNotes) ? null : vm.ReleaseNotes;
        ContentDialogResult choice = ContentDialogResult.None;
        ContentDialog? dialog = null;
        try
        {
            dialog = BuildDialog(window, title, status, notes, null,
                loc.GetString("UpdateInstallNow"), loc.GetString("UpdateLater"));
            TrackDialog(dialog);
            choice = await dialog.ShowAsync();
        }
        catch
        {
            choice = ContentDialogResult.None;
        }
        finally
        {
            UntrackDialog(dialog);
        }
        if (choice != ContentDialogResult.Primary)
            return;
        await DownloadAndPromptAsync(window, loc, title, vm).ConfigureAwait(false);
    }

    private static async Task DownloadAndPromptAsync(
        Window window, LocalizationService loc, string title, UpdateCenterViewModel vm)
    {
        var token = NewFlowToken();
        var statusBlock = new TextBlock
        {
            Text = loc.GetString("SettingsDownloadingProgress", 0),
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap
        };
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var panel = new StackPanel();
        try
        {
            panel.Children.Add(statusBlock);
            panel.Children.Add(bar);
        }
        catch { }

        ContentDialog? dialog = null;
        void OnCancel(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                CancelFlow();
            }
            catch { }
        }
        try
        {
            dialog = new ContentDialog
            {
                XamlRoot = window.Content.XamlRoot,
                Title = title,
                Content = panel,
                SecondaryButtonText = loc.GetString("UpdateCancel"),
                DefaultButton = ContentDialogButton.Secondary
            };
            dialog.SecondaryButtonClick += OnCancel;
            TrackDialog(dialog);
        }
        catch
        {
            return;
        }

        void OnProgress(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(UpdateCenterViewModel.DownloadProgress))
                {
                    int percent = vm.DownloadProgress;
                    try
                    {
                        bar.Value = Math.Clamp(percent, 0, 100);
                        statusBlock.Text = loc.GetString("SettingsDownloadingProgress", percent);
                    }
                    catch { }
                }
            }
            catch { }
        }
        vm.PropertyChanged += OnProgress;
        Task<ContentDialogResult>? showTask = null;
        Task? downloadTask = null;
        try
        {
            try { showTask = dialog.ShowAsync().AsTask(); } catch { showTask = null; }
            try
            {
                downloadTask = vm.DownloadAsync(token);
            }
            catch
            {
                downloadTask = Task.CompletedTask;
            }
            // No ConfigureAwait(false): the dialog below is thread-affine,
            // so the continuation must resume on the UI thread.
            try { await downloadTask; } catch { }
        }
        finally
        {
            try { vm.PropertyChanged -= OnProgress; } catch { }
        }
        if (token.IsCancellationRequested)
        {
            try { dialog.Hide(); } catch { }
            UntrackDialog(dialog);
            return;
        }
        try { dialog.Hide(); } catch { }
        UntrackDialog(dialog);
        if (showTask is not null)
        {
            try { await showTask; } catch { }
        }

        if (!vm.CanInstall)
        {
            ToastError(title, string.IsNullOrWhiteSpace(vm.StatusMessage)
                ? loc.GetString("SettingsCheckFailed")
                : vm.StatusMessage);
            return;
        }
        await ShowReadyCoreAsync(
            string.IsNullOrWhiteSpace(vm.PendingVersion) ? null : vm.PendingVersion,
            string.IsNullOrWhiteSpace(vm.ReleaseNotes) ? null : vm.ReleaseNotes).ConfigureAwait(false);
    }

    private static Task ShowReadyCoreAsync(string? version, string? notes)
    {
        var snap = Snapshot();
        if (snap is null)
            return Task.CompletedTask;
        var (window, loc) = snap.Value;
        return ShowReadyDialogAsync(window, loc, version, notes);
    }

    private static async Task ShowReadyDialogAsync(
        Window window, LocalizationService loc, string? version, string? notes)
    {
        CancelFlow();
        string display = string.IsNullOrWhiteSpace(version)
            ? AppInfo.Current.VersionDisplay
            : version;
        string title = loc.GetString("UpdatePopupTitle");
        string status = loc.GetString("UpdateRestartTitle", display);
        string? body = string.IsNullOrWhiteSpace(notes)
            ? loc.GetString("UpdateRestartBody")
            : notes;
        ContentDialogResult choice = ContentDialogResult.None;
        ContentDialog? dialog = null;
        try
        {
            dialog = BuildDialog(window, title, status, body, null,
                loc.GetString("UpdateRestartNow"), loc.GetString("UpdateRestartLater"));
            TrackDialog(dialog);
            choice = await dialog.ShowAsync();
        }
        catch
        {
            choice = ContentDialogResult.None;
        }
        finally
        {
            UntrackDialog(dialog);
        }
        if (choice == ContentDialogResult.Primary)
        {
            try
            {
                var updates = ResolveUpdates();
                updates?.ApplyPendingUpdateAndRestart();
            }
            catch { }
        }
    }

    private static async Task ShowErrorDialogAsync(
        Window window, LocalizationService loc, string title, string detail)
    {
        string status = string.IsNullOrWhiteSpace(detail)
            ? loc.GetString("SettingsCheckFailed")
            : detail;
        ContentDialogResult choice = ContentDialogResult.None;
        ContentDialog? dialog = null;
        try
        {
            dialog = BuildDialog(window, title, status, null, null,
                loc.GetString("UpdateRetry"), loc.GetString("UpdateClose"));
            TrackDialog(dialog);
            choice = await dialog.ShowAsync();
        }
        catch
        {
            choice = ContentDialogResult.None;
        }
        finally
        {
            UntrackDialog(dialog);
        }
        if (choice == ContentDialogResult.Primary)
            await ShowCheckCoreAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a native WinUI 3 dialog: system title + wrapping body text,
    /// optional release notes, optional progress slot. No custom chrome —
    /// the platform renders the card, corners, and buttons.
    /// </summary>
    private static ContentDialog BuildDialog(
        Window window, string title, string status, string? notes,
        FrameworkElement? extra, string? primary, string? secondary)
    {
        var panel = new StackPanel { Spacing = 8 };
        var statusBlock = new TextBlock
        {
            Text = status ?? string.Empty,
            Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap
        };
        try { panel.Children.Add(statusBlock); } catch { }
        if (!string.IsNullOrWhiteSpace(notes))
        {
            var notesBlock = new TextBlock
            {
                Text = notes,
                Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                Opacity = 0.8,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 200,
                IsTextSelectionEnabled = true
            };
            try { panel.Children.Add(notesBlock); } catch { }
        }
        if (extra is not null)
        {
            try { panel.Children.Add(extra); } catch { }
        }
        var dialog = new ContentDialog
        {
            XamlRoot = window.Content.XamlRoot,
            Title = title ?? string.Empty,
            Content = panel,
            DefaultButton = ContentDialogButton.Primary
        };
        if (!string.IsNullOrWhiteSpace(primary))
            dialog.PrimaryButtonText = primary;
        if (!string.IsNullOrWhiteSpace(secondary))
            dialog.SecondaryButtonText = secondary;
        return dialog;
    }

    private static void TrackDialog(ContentDialog? dialog)
    {
        try
        {
            if (dialog is null)
                return;
            lock (_gate)
            {
                _activeDialog = dialog;
            }
        }
        catch { }
    }

    private static void UntrackDialog(ContentDialog? dialog)
    {
        try
        {
            lock (_gate)
            {
                if (ReferenceEquals(_activeDialog, dialog))
                    _activeDialog = null;
            }
        }
        catch { }
    }

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
