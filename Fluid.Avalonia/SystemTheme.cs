using System.Diagnostics;
using Avalonia.Styling;

namespace Fluid.Avalonia;

/// <summary>
/// Synchronous OS light/dark detection for app startup, to prevent the Linux "flash of light theme".
/// On Windows and macOS the platform resolves the OS variant synchronously before the first frame, so a
/// follow-OS app (<c>RequestedThemeVariant="Default"</c>) already paints correctly. On Linux/X11, Avalonia
/// reads the freedesktop <c>org.freedesktop.appearance</c> <c>color-scheme</c> portal <b>asynchronously</b>
/// (a fire-and-forget D-Bus call), so the first frame is painted in the Light default and then flips to
/// Dark once the reply lands — the visible flash. This helper probes the OS itself, synchronously, and
/// seeds the variant before the first window paints. Call <see cref="SeedStartupVariant"/> from the first
/// window's constructor (before it is shown).
/// </summary>
/// <remarks>
/// Because Avalonia 12 exposes no public way to observe when its own asynchronous detection resolves
/// (<c>IPlatformSettings</c> is private on <c>TopLevel</c> and the service locator was removed), the seed
/// is left in place: on Linux the app starts in the detected variant rather than the "follow-OS"
/// (<see cref="ThemeVariant.Default"/>) state. A "System" / follow-OS option (e.g. a settings toggle that
/// sets <see cref="ThemeVariant.Default"/>) still works when the user picks it.
/// </remarks>
public static class SystemTheme
{
    /// <summary>Synchronously detects the OS light/dark preference. Returns <see cref="ThemeVariant.Default"/>
    /// on Windows/macOS (their backends already resolve it synchronously, so there's no flash to fix) and
    /// whenever nothing conclusive is found on Linux. On Linux it tries, in order: the freedesktop
    /// <c>color-scheme</c> portal (the same source Avalonia uses, read synchronously via <c>gdbus</c>),
    /// GNOME's <c>color-scheme</c> gsetting, then the Cinnamon/Mint GTK-theme name (dark is selected there
    /// by a dark GTK theme like <c>Mint-Y-Dark-Aqua</c>, with no <c>color-scheme</c> preference — and the
    /// portal is unreliable on Cinnamon, so the theme name is the dependable signal).</summary>
    public static ThemeVariant DetectStartupVariant()
    {
        if (!OperatingSystem.IsLinux())
            return ThemeVariant.Default;

        // 1) The freedesktop portal, read synchronously. color-scheme: 1 = prefer dark, 2 = prefer light,
        //    0 = no preference (only 1 means dark).
        var portal = Run("gdbus",
            "call --session --dest org.freedesktop.portal.Desktop " +
            "--object-path /org/freedesktop/portal/desktop " +
            "--method org.freedesktop.portal.Settings.ReadOne " +
            "org.freedesktop.appearance color-scheme");
        if (portal.Contains("uint32 1")) return ThemeVariant.Dark;
        if (portal.Contains("uint32 2")) return ThemeVariant.Light;

        // 2) GNOME's color-scheme key (no portal dependency).
        if (Run("gsettings", "get org.gnome.desktop.interface color-scheme").Contains("prefer-dark"))
            return ThemeVariant.Dark;

        // 3) Cinnamon / Mint: dark is chosen via a dark GTK *theme*, so read its name and look for "dark".
        if (Run("gsettings", "get org.cinnamon.desktop.interface gtk-theme")
                .Contains("dark", StringComparison.OrdinalIgnoreCase))
            return ThemeVariant.Dark;

        return ThemeVariant.Default;
    }

    /// <summary>When the app is following the OS (<see cref="Application.RequestedThemeVariant"/> is
    /// <see cref="ThemeVariant.Default"/> or unset), detects the OS variant with
    /// <see cref="DetectStartupVariant"/> and applies it as the explicit variant so the first window frame
    /// is painted correctly — the fix for the Linux flash. No-op on Windows/macOS, when the app already has
    /// an explicit theme, or when detection is inconclusive. Returns the seeded variant (or
    /// <see cref="ThemeVariant.Default"/> when nothing was seeded). Call from the first window's
    /// constructor, before it is shown.</summary>
    public static ThemeVariant SeedStartupVariant()
    {
        if (Application.Current is not { } app)
            return ThemeVariant.Default;

        var current = app.RequestedThemeVariant;
        if (current is not null && current != ThemeVariant.Default)
            return ThemeVariant.Default;   // the app has chosen an explicit theme; don't override it

        var detected = DetectStartupVariant();
        if (detected == ThemeVariant.Default)
            return ThemeVariant.Default;

        app.RequestedThemeVariant = detected;
        return detected;
    }

    // Runs a short-lived CLI tool and returns its stdout, bounded so a stuck portal/daemon can never hang
    // startup. Any failure (tool missing, sandboxed, timeout) yields an empty string, so the caller falls
    // back to the next probe — and ultimately to Avalonia's own (async) detection.
    private static string Run(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null)
                return string.Empty;

            var stdout = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(600))
            {
                try { process.Kill(); } catch { /* best effort */ }
                return string.Empty;
            }
            return stdout.GetAwaiter().GetResult();
        }
        catch
        {
            return string.Empty;
        }
    }
}
