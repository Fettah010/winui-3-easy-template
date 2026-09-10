using System.Reflection;

namespace MyWinUIApp.Services;

public sealed class AppInfo
{
    public static AppInfo Current { get; } = new();

    public string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string VersionDisplay => $"Version {Version}";
}