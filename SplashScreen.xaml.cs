using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public sealed partial class SplashScreen : Window
{
    private readonly CompositionCapabilities _capabilities = new();

    public SplashScreen()
    {
        this.InitializeComponent();
        this.Title = string.Empty;

        // Borderless, fixed size, no chrome
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
        }

        // Set version
        VersionText.Text = AppInfo.Current.VersionDisplay;

        // Center on screen
        CenterToScreen();

        // Start entrance animation once loaded
        this.Activated += OnActivated;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            this.Activated -= OnActivated;
            _ = PlayEntranceAnimation();
        }
    }

    private void CenterToScreen()
    {
        try
        {
            var displayArea = DisplayArea.GetFromPoint(this.AppWindow.Position, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            int width = 480;
            int height = 320;

            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            this.AppWindow.Move(new Windows.Graphics.PointInt32(
                workArea.X + (workArea.Width - width) / 2,
                workArea.Y + (workArea.Height - height) / 2));
        }
        catch { }
    }

    private async Task PlayEntranceAnimation()
    {
        // Fade in + scale up
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

        // Start pulsing dots after entrance
        await Task.Delay(400);
        StartPulsingDots();
    }

    private void StartPulsingDots()
    {
        _ = AnimateDot(Dot1, 0);
        _ = AnimateDot(Dot2, 200);
        _ = AnimateDot(Dot3, 400);
    }

    private async Task AnimateDot(Ellipse dot, int delay)
    {
        while (true)
        {
            try
            {
                await Task.Delay(delay);

                // Pulse up
                var up = new DoubleAnimation
                {
                    From = 0.3,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(up, dot);
                Storyboard.SetTargetProperty(up, "Opacity");
                var s1 = new Storyboard();
                s1.Children.Add(up);
                s1.Begin();

                await Task.Delay(300);

                // Pulse down
                var down = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.3,
                    Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(down, dot);
                Storyboard.SetTargetProperty(down, "Opacity");
                var s2 = new Storyboard();
                s2.Children.Add(down);
                s2.Begin();

                await Task.Delay(600);
            }
            catch { break; }
        }
    }

    /// <summary>
    /// Plays the exit animation then closes the window.
    /// </summary>
    public async Task CloseWithAnimation()
    {
        try
        {
            // Fade out + slight scale down
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
