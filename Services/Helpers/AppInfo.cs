using System.Reflection;
using System.Runtime.InteropServices;

namespace DevTemWinUi3.Services;

public sealed class AppInfo
{
    public static AppInfo Current { get; } = new();

    public string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string VersionDisplay => $"Version {Version}";

    /// <summary>
    /// Full informational version (may carry a "-beta" suffix for beta builds).
    /// </summary>
    public string InformationalVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Version;

    /// <summary>Whether this build is a beta release.</summary>
    public bool IsBetaBuild => ChannelResolver.IsBetaVersion(InformationalVersion);

    /// <summary>Update channel fresh installs of this build should track.</summary>
    public string DefaultChannel => IsBetaBuild ? ChannelResolver.Beta : ChannelResolver.Stable;

    /// <summary>
    /// Source commit the binary was built from (pain-log #18: stale desktop
    /// shortcuts can launch an old binary where new routes do not exist —
    /// the click "does nothing"). Baked in via the
    /// <c>DEVTEM_BUILD_COMMIT</c> environment value at build time
    /// (CI sets it to the tag/commit); "dev" for local builds.
    /// About/Diagnostics surfaces include it so a bug report names the
    /// exact binary. Never throws.
    /// </summary>
    public static string BuildCommit
    {
        get
        {
            try
            {
                string? env = System.Environment.GetEnvironmentVariable("DEVTEM_BUILD_COMMIT");
                if (!string.IsNullOrWhiteSpace(env))
                    return env.Trim();
                var asm = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyMetadataAttribute>();
                if (asm is not null && asm.Key == "BuildCommit" && !string.IsNullOrWhiteSpace(asm.Value))
                    return asm.Value;
            }
            catch { }
            return "dev";
        }
    }

    /// <summary>Version plus build commit when known (About/Diagnostics).</summary>
    public string VersionWithCommit
    {
        get
        {
            string commit = BuildCommit;
            return commit == "dev" ? Version : Version + " (" + commit + ")";
        }
    }

    private const int AppmodelErrorNoPackage = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);

    /// <summary>
    /// Whether the app runs inside an MSIX package. Packaged runs get
    /// registry/file virtualization: the HKCU Run autostart toggle does not
    /// apply (startup goes through the manifest StartupTask instead).
    /// </summary>
    public static bool IsPackaged
    {
        get
        {
            try
            {
                uint length = 0;
                return GetCurrentPackageFullName(ref length, IntPtr.Zero) != AppmodelErrorNoPackage;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Package family name for Store deep links
    /// (<c>ms-windows-store://pdp/?PFN=…</c>). Null when unpackaged.
    /// Never throws.
    /// </summary>
    public static string? PackageFamilyName
    {
        get
        {
            try
            {
                if (!IsPackaged)
                    return null;
                var name = Windows.ApplicationModel.Package.Current?.Id?.FamilyName;
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch
            {
                return null;
            }
        }
    }
}
