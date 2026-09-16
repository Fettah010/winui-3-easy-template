using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Controls;

/// <summary>
/// Animated update popup (replaces the Update Center page): a full-window
/// overlay with a centered card, native-feel fade + scale entrance, linear
/// progress (indeterminate while checking, determinate while downloading),
/// and an animated ellipsis while checking — no spinner ring anywhere.
/// The <see cref="Services.UpdateDialogService"/> drives the states; this
/// class owns only visuals, motion, and button routing. UI-bound by house
/// rule (never-throw, untested headless).
/// </summary>
public sealed partial class UpdatePopup : UserControl
{
    private readonly DispatcherTimer _dotsTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private string _dotsBase = string.Empty;
    private int _dotsCount;
    private bool _showing;

    /// <summary>Invoked on the UI thread when the primary button is pressed.</summary>
    public event EventHandler? PrimaryPressed;

    /// <summary>Invoked on the UI thread when the secondary button is pressed.</summary>
    public event EventHandler? SecondaryPressed;

    public UpdatePopup()
    {
        this.InitializeComponent();
        _dotsTimer.Tick += (_, _) => StepDots();
    }

    /// <summary>Whether the overlay is currently visible.</summary>
    public bool IsShowing => _showing && OverlayGrid.Visibility == Visibility.Visible;

    /// <summary>Shows the overlay with the entrance animation. UI thread only.</summary>
    public async Task ShowAsync()
    {
        try
        {
            if (_showing)
                return;
            _showing = true;
            OverlayGrid.Visibility = Visibility.Visible;

            var fade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = new Duration(LaunchAnimations.Scale(TimeSpan.FromMilliseconds(200))),
            };
            var cardFade = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = new Duration(LaunchAnimations.Scale(TimeSpan.FromMilliseconds(250))),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var scaleX = new DoubleAnimation
            {
                From = 0.96, To = 1.0,
                Duration = new Duration(LaunchAnimations.Scale(TimeSpan.FromMilliseconds(250))),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var scaleY = new DoubleAnimation
            {
                From = 0.96, To = 1.0,
                Duration = new Duration(LaunchAnimations.Scale(TimeSpan.FromMilliseconds(250))),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fade, OverlayGrid);
            Storyboard.SetTargetProperty(fade, "Opacity");
            Storyboard.SetTarget(cardFade, Card);
            Storyboard.SetTargetProperty(cardFade, "Opacity");
            Storyboard.SetTarget(scaleX, CardScale);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");
            Storyboard.SetTarget(scaleY, CardScale);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");
            var story = new Storyboard();
            story.Children.Add(fade);
            story.Children.Add(cardFade);
            story.Children.Add(scaleX);
            story.Children.Add(scaleY);
            await LaunchAnimations.AwaitStoryboardAsync(story, TimeSpan.FromMilliseconds(400));
        }
        catch { }
    }

    /// <summary>Hides the overlay with a quick fade. UI thread only.</summary>
    public async Task HideAsync()
    {
        try
        {
            StopDots();
            if (!_showing)
                return;
            var fade = new DoubleAnimation
            {
                From = 1, To = 0,
                Duration = new Duration(LaunchAnimations.Scale(TimeSpan.FromMilliseconds(150))),
            };
            Storyboard.SetTarget(fade, OverlayGrid);
            Storyboard.SetTargetProperty(fade, "Opacity");
            var story = new Storyboard();
            story.Children.Add(fade);
            await LaunchAnimations.AwaitStoryboardAsync(story, TimeSpan.FromMilliseconds(250));
        }
        catch { }
        finally
        {
            try
            {
                _showing = false;
                OverlayGrid.Visibility = Visibility.Collapsed;
            }
            catch { }
        }
    }

    public void SetTitle(string title)
    {
        try { TitleText.Text = title ?? string.Empty; } catch { }
    }

    public void SetStatus(string status)
    {
        try
        {
            StopDots();
            StatusText.Text = status ?? string.Empty;
        }
        catch { }
    }

    /// <summary>Status line with an animated ellipsis (checking state).</summary>
    public void SetCheckingStatus(string baseText)
    {
        try
        {
            _dotsBase = baseText ?? string.Empty;
            _dotsCount = 0;
            StatusText.Text = _dotsBase;
            _dotsTimer.Stop();
            _dotsTimer.Start();
        }
        catch { }
    }

    public void SetProgressIndeterminate(bool visible)
    {
        try
        {
            if (visible)
            {
                Bar.IsIndeterminate = true;
                Bar.Visibility = Visibility.Visible;
            }
            else
            {
                Bar.IsIndeterminate = false;
                Bar.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }

    public void SetProgress(int percent)
    {
        try
        {
            Bar.IsIndeterminate = false;
            Bar.Value = Math.Clamp(percent, 0, 100);
            Bar.Visibility = Visibility.Visible;
        }
        catch { }
    }

    public void HideProgress()
    {
        try
        {
            Bar.IsIndeterminate = false;
            Bar.Visibility = Visibility.Collapsed;
        }
        catch { }
    }

    public void SetNotes(string? notes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                NotesText.Text = string.Empty;
                NotesText.Visibility = Visibility.Collapsed;
            }
            else
            {
                NotesText.Text = notes;
                NotesText.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    public void SetPrimary(string? label)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(label))
                PrimaryButton.Visibility = Visibility.Collapsed;
            else
            {
                PrimaryButton.Content = label;
                PrimaryButton.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    public void SetSecondary(string? label)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(label))
                SecondaryButton.Visibility = Visibility.Collapsed;
            else
            {
                SecondaryButton.Content = label;
                SecondaryButton.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    private void StepDots()
    {
        try
        {
            if (!_showing)
            {
                StopDots();
                return;
            }
            _dotsCount = (_dotsCount + 1) % 4;
            StatusText.Text = _dotsBase + new string('.', _dotsCount);
        }
        catch { }
    }

    private void StopDots()
    {
        try { _dotsTimer.Stop(); } catch { }
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        try { PrimaryPressed?.Invoke(this, EventArgs.Empty); } catch { }
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        try { SecondaryPressed?.Invoke(this, EventArgs.Empty); } catch { }
    }
}
