using System;
using System.IO;
using Microsoft.Win32;

namespace DevTemWinUi3.Services;

/// <summary>
/// End-to-end deep-link (<c>devtem://…</c>) support for the unpackaged app.
/// Parsing is pure and unit-tested (<see cref="TryParseRoute"/>,
/// <see cref="ExtractProtocolUri"/>); registration writes the per-user
/// <c>HKCU\Software\Classes\&lt;scheme&gt;</c> keys (no admin needed) so a
/// browser or Run dialog can launch the app. The app self-registers on
/// startup (<see cref="EnsureRegistered"/>); MSIX installs register through
/// the manifest protocol extension instead.
/// Single-instance handoff: a second launch writes its URI to a pending file
/// and signals the first instance, which picks it up on activation.
/// </summary>
public static class ProtocolService
{
    /// <summary>Deep-link scheme without <c>://</c> (e.g. "devtem").</summary>
    public static string Scheme => AppMetadata.ProtocolScheme;

    private static string Prefix => Scheme + "://";

    /// <summary>
    /// Finds the first deep-link URI in a command line. Ignores installer
    /// flags (Velopack <c>--veloapp-*</c>) and everything else. Never throws.
    /// </summary>
    public static string? ExtractProtocolUri(string[]? args)
    {
        try
        {
            if (args is null)
                return null;
            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;
                var clean = arg.Trim().Trim('"').Trim();
                if (clean.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    return clean;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Strips scheme, slashes and query down to a navigation tag:
    /// <c>devtem://settings</c> → <c>settings</c>,
    /// <c>devtem:///home?theme=dark</c> → <c>home</c>.
    /// Returns false for empty input, foreign schemes, or bare scheme.
    /// </summary>
    public static bool TryParseRoute(string? uri, out string tag)
    {
        tag = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(uri))
                return false;
            var clean = uri.Trim().Trim('"').Trim();
            if (!clean.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            clean = clean[Prefix.Length..].Trim('/');
            var query = clean.IndexOf('?');
            if (query >= 0)
                clean = clean[..query];
            clean = clean.Trim('/');
            if (string.IsNullOrWhiteSpace(clean))
                return false;
            tag = clean;
            return true;
        }
        catch
        {
            tag = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Registers the scheme for the current exe when missing (path changes
    /// after updates/installs). Safe to call on every startup. Packaged runs
    /// skip it (the manifest protocol extension owns the scheme there; HKCU
    /// writes would land in the per-app virtualized store). Never throws.
    /// </summary>
    public static void EnsureRegistered()
    {
        try
        {
            if (AppInfo.IsPackaged)
                return;
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe))
                return;
            if (!IsProtocolRegistered(exe))
                RegisterProtocol(exe);
        }
        catch { }
    }

    /// <summary>Writes the scheme keys under HKCU (no admin). Never throws.</summary>
    public static bool RegisterProtocol(string? exePath = null)
    {
        try
        {
            exePath ??= Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
                return false;
            using (var scheme = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Scheme}"))
            {
                if (scheme is null)
                    return false;
                scheme.SetValue(null, $"URL:{AppMetadata.AppName} deep link");
                scheme.SetValue("URL Protocol", string.Empty);
                using (var icon = scheme.CreateSubKey("DefaultIcon"))
                    icon?.SetValue(null, $"\"{exePath}\",0");
                using (var command = scheme.CreateSubKey(@"shell\open\command"))
                    command?.SetValue(null, $"\"{exePath}\" \"%1\"");
            }
            AppLog.Information("Protocol {Scheme} registered to {Exe}", Scheme, exePath);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Protocol registration failed");
            return false;
        }
    }

    /// <summary>Removes the scheme keys. Never throws.</summary>
    public static bool UnregisterProtocol()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{Scheme}", throwOnMissingSubKey: false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Whether the scheme currently points at <paramref name="exePath"/>. Never throws.</summary>
    public static bool IsProtocolRegistered(string? exePath = null)
    {
        try
        {
            exePath ??= Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
                return false;
            using var command = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{Scheme}\shell\open\command", writable: false);
            var value = command?.GetValue(null) as string;
            return !string.IsNullOrWhiteSpace(value) &&
                value.Contains(exePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Packaged protocol-launch URI. Packaged runs never see the URI on the
    /// command line — it arrives as activation args, so both the first
    /// process (App launch) and redirected second processes (single-instance
    /// handoff in Program) resolve it here. Unpackaged runs return null
    /// (the command line carries it). Never throws.
    /// </summary>
    public static string? GetPackagedProtocolUri()
    {
        try
        {
            if (!AppInfo.IsPackaged)
                return null;
            var activated = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent()?.GetActivatedEventArgs();
            if (activated?.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol &&
                activated.Data is Windows.ApplicationModel.Activation.ProtocolActivatedEventArgs protocol)
                return protocol.Uri?.AbsoluteUri;
        }
        catch { }
        return null;
    }

    internal static string DefaultPendingPath => Path.Combine(
        AppPaths.DataFolder,
        "pending-protocol.txt");

    /// <summary>
    /// Stashes a URI for the running instance (second-launch handoff).
    /// <paramref name="path"/> overrides the file (tests). Never throws.
    /// </summary>
    public static void WritePendingUri(string uri, string? path = null)
    {
        try
        {
            var file = path ?? DefaultPendingPath;
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(file, uri);
        }
        catch { }
    }

    /// <summary>
    /// Reads and deletes the stashed URI (first instance, on activation).
    /// Returns false when nothing is pending. Never throws.
    /// </summary>
    public static bool TryReadAndClearPendingUri(out string? uri, string? path = null)
    {
        uri = null;
        try
        {
            var file = path ?? DefaultPendingPath;
            if (!File.Exists(file))
                return false;
            uri = File.ReadAllText(file).Trim();
            try { File.Delete(file); } catch { }
            return !string.IsNullOrWhiteSpace(uri);
        }
        catch
        {
            return false;
        }
    }
}
