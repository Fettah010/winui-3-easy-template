using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public sealed partial class SplashScreen : Window
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWCP_ROUND = 2;
    private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

    public SplashScreen()
    {
        this.InitializeComponent();
        this.Title = string.Empty;
        this.SystemBackdrop = new MicaBackdrop();

        // Honor the persisted app theme (System/Light/Dark) instead of always
        // following Windows (single source: ThemeService, mirrors MainWindow).
        if (Content is FrameworkElement themedRoot)
            ThemeService.Current.ApplyTo(themedRoot);

        // Taskbar icon matches the app/installer icon (guarded: a missing file
        // can blank the icon instead of throwing on installed builds).
        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (System.IO.File.Exists(iconPath))
                this.AppWindow.SetIcon(iconPath);
        }
        catch { }

        ApplyWindowChrome();

        // Frameless is NOT an option here, by platform design:
        // - A window with no border/caption can never get DWM rounded corners,
        //   even via DWMWA_WINDOW_CORNER_PREFERENCE (MS Learn "Apply rounded
        //   corners in desktop apps", category 3).
        // - On WindowsAppSDK 1.6+, SetBorderAndTitleBar(false, false) leaves a
        //   phantom white frame (microsoft-ui-xaml#9978).
        // So: keep the sizing border (DWM rounds it, draws shadow, and honors
        // DWMWA_BORDER_COLOR), drop only the title bar.
        // NOTE: IsResizable must stay true — a non-resizable custom frame loses
        // DWM rounding on unpackaged apps (microsoft-ui-xaml#8374). The splash
        // only lives ~1s, so the resize cursor is a non-issue in practice.
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        VersionText.Text = AppInfo.Current.VersionDisplay;
        CenterToScreen();

        this.Activated += OnActivated;
    }

    /// <summary>
    /// Reports real startup progress on the splash (fraction 0..1 + status
    /// text). Thread-safe: marshals to the UI thread when needed.
    /// </summary>
    public void ReportProgress(double fraction, string status)
    {
        try
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() => ReportProgress(fraction, status));
                return;
            }

            if (double.IsNaN(fraction) || double.IsInfinity(fraction))
                return;
            LoadProgress.Value = Math.Clamp(fraction * 100.0, 0, 100);
            StatusText.Text = status;
        }
        catch { }
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            this.Activated -= OnActivated;
            _ = PlayEntranceAnimation();
        }
    }

    /// <summary>
    /// Forces Windows 11 rounded corners and removes the 1px DWM border that
    /// Windows draws around borderless windows. Without this the splash gets
    /// square "harsh" corners with a light edge.
    /// </summary>
    private void ApplyWindowChrome()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hwnd == IntPtr.Zero)
                return;

            int round = DWMWCP_ROUND;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));

            int noBorder = DWMWA_COLOR_NONE;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref noBorder, sizeof(int));
        }
        catch { }
    }

    private void CenterToScreen()
    {
        try
        {
            var displayArea = DisplayArea.GetFromPoint(this.AppWindow.Position, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            int width = 560;
            int height = 360;

            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            this.AppWindow.Move(new Windows.Graphics.PointInt32(
                workArea.X + (workArea.Width - width) / 2,
                workArea.Y + (workArea.Height - height) / 2));
        }
        catch { }
    }

    private async Task PlayEntranceAnimation()
    {
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var scaleX = new DoubleAnimation
        {
            From = 0.96,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var scaleY = new DoubleAnimation
        {
            From = 0.96,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(fadeIn, RootGrid);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        Storyboard.SetTarget(scaleX, RootTransform);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        Storyboard.SetTarget(scaleY, RootTransform);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var story = new Storyboard();
        story.Children.Add(fadeIn);
        story.Children.Add(scaleX);
        story.Children.Add(scaleY);
        story.Begin();

        await Task.Delay(600);
    }

    public async Task CloseWithAnimation()
    {
        try
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var scaleXOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.98,
                Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var scaleYOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.98,
                Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(fadeOut, RootGrid);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            Storyboard.SetTarget(scaleXOut, RootTransform);
            Storyboard.SetTargetProperty(scaleXOut, "ScaleX");
            Storyboard.SetTarget(scaleYOut, RootTransform);
            Storyboard.SetTargetProperty(scaleYOut, "ScaleY");

            var story = new Storyboard();
            story.Children.Add(fadeOut);
            story.Children.Add(scaleXOut);
            story.Children.Add(scaleYOut);
            story.Begin();

            await Task.Delay(300);
        }
        catch { }

        this.Close();
    }
}
