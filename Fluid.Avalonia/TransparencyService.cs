using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Win32;

namespace Fluid.Avalonia;

/// <summary>
/// Drives a window's translucent backdrop and its solid fallback, cross-platform: a Mica-style blur on
/// Windows, vibrancy (<c>AcrylicBlur</c>) on macOS, and blur / transparency on Linux where the compositor
/// supports it (real blur only on KDE&#160;Plasma / KWin) — degrading to an opaque base surface everywhere
/// it can't be rendered. The Windows "Transparency effects" user setting is exposed via
/// <see cref="IsOsTransparencyEnabled"/> so callers can seed their default from it (there is no equivalent
/// global switch on macOS / Linux — the compositor decides per window). <see cref="FluidWindow"/> uses this
/// for its <see cref="FluidWindow.TransparencyEnabled"/> property; it works on any <see cref="Window"/>.
/// </summary>
public static class TransparencyService
{
    private static readonly WindowTransparencyLevel[] SolidOnly = { WindowTransparencyLevel.None };

    /// <summary>Whether the OS asks apps to use window transparency. On Windows this reads the
    /// "Transparency effects" setting (HKCU\…\Themes\Personalize\EnableTransparency). Returns
    /// <see langword="true"/> on macOS / Linux and on any read error — those platforms have no such
    /// global switch, so the per-window request (and the compositor) decides.</summary>
    public static bool IsOsTransparencyEnabled()
    {
        if (!OperatingSystem.IsWindows())
            return true;

        try
        {
            return Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency", 1) is not 0;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Requests the backdrop on <paramref name="window"/>: the best translucent level the
    /// current OS offers when <paramref name="enabled"/>, otherwise opaque. The level actually granted
    /// resolves asynchronously, so pair this with <see cref="ReconcileBackground"/> (call it from the
    /// window's <c>OnOpened</c> and whenever its <see cref="TopLevel.ActualTransparencyLevel"/> changes).</summary>
    public static void Apply(Window window, bool enabled) =>
        window.TransparencyLevelHint = enabled ? PreferredLevels() : SolidOnly;

    /// <summary>Sets the window background to match the level the platform actually granted: transparent
    /// (so the backdrop shows through) when a translucent level is active, else the solid base surface.
    /// This is the graceful fallback for compositors that couldn't honor the request (e.g. GNOME, an
    /// un-composited X11 session, or down-level Windows).</summary>
    public static void ReconcileBackground(Window window) =>
        window.Background = window.ActualTransparencyLevel == WindowTransparencyLevel.None
            ? SolidBase(window)
            : Brushes.Transparent;

    // Best-first translucency request per OS. macOS maps AcrylicBlur → NSVisualEffectView vibrancy
    // (Mica/Blur fall through to opaque there); Linux honors Blur only on KWin and otherwise degrades
    // to plain Transparent, then None. The trailing None is the explicit opaque fallback.
    private static WindowTransparencyLevel[] PreferredLevels() =>
        OperatingSystem.IsWindows()
            ? new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.None }
            : OperatingSystem.IsMacOS()
                ? new[] { WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None }
                : OperatingSystem.IsLinux()
                    ? new[] { WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None }
                    : SolidOnly;

    private static IBrush SolidBase(Window window) =>
        window.TryFindResource("SolidBackgroundFillColorBaseBrush", window.ActualThemeVariant, out var b) && b is IBrush brush
            ? brush
            : Brushes.Transparent;
}
