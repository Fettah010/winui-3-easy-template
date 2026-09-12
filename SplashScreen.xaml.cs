using System;
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
    public SplashScreen()
    {
        this.InitializeComponent();
        this.Title = string.Empty;
        this.SystemBackdrop = new MicaBackdrop();

        // Borderless, fixed size
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
        }

        VersionText.Text = AppInfo.Current.VersionDisplay;
        CenterToScreen();

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
            int height = 380;

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
