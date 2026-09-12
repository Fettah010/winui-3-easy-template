using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public sealed partial class SplashScreen : Window
{
    public SplashScreen()
    {
        this.InitializeComponent();
        this.Title = string.Empty;

        // Borderless, centered, fixed size
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // Set version
        VersionText.Text = AppInfo.Current.VersionDisplay;

        // Center on screen
        this.CenterToScreen();
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
}
