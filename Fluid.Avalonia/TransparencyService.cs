using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Win32;

namespace Fluid.Avalonia;

/// <summary>
/// Drives a window's translucent backdrop and its solid fallback, cross-platform: a Mica-style blur on
/// Windows, vibrancy (<c>AcrylicBlur</c>) on macOS, a KWin blur on KDE&#160;Plasma (Linux) and the API-31+
/// window blur on Android — degrading to an opaque base surface everywhere a real backdrop can't be
/// rendered (a bare blur-less transparent level is painted solid, so non-KWin Linux stays opaque). The
/// Windows "Transparency effects" user setting is exposed via
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
    /// (so the backdrop shows through) ONLY when a real blur backdrop is active — Mica, macOS vibrancy
    /// (<c>AcrylicBlur</c>) or KWin <c>Blur</c> — and the solid base surface otherwise. Crucially, a bare
    /// blur-less <see cref="WindowTransparencyLevel.Transparent"/> is painted solid too: Avalonia's X11
    /// backend falls back to it on any compositing desktop that can't do the real effect (e.g. GNOME) —
    /// and a see-through window without blur isn't wanted — so those stay opaque. Call this from the
    /// window's <c>OnOpened</c> and whenever its <see cref="TopLevel.ActualTransparencyLevel"/> changes.</summary>
    public static void ReconcileBackground(Window window)
    {
        var level = window.ActualTransparencyLevel;
        var hasBackdrop = level == WindowTransparencyLevel.Mica
            || level == WindowTransparencyLevel.AcrylicBlur
            || level == WindowTransparencyLevel.Blur;
        window.Background = hasBackdrop ? Brushes.Transparent : SolidBase(window);
    }

    // Best-first translucency request per OS. macOS maps AcrylicBlur → NSVisualEffectView vibrancy
    // (Mica/Blur fall through to opaque there). Linux and Android both use Blur → on Linux that's the
    // KWin (KDE) blur, on Android the API-31+ window blur (BlurBehindRadius); each falls to opaque None
    // otherwise. On non-KWin Linux Avalonia's X11 backend resolves the hint to a bare Transparent level
    // regardless, but ReconcileBackground paints that solid (no blur-less see-through), so it stays
    // opaque; on Android < 31 the None entry is granted directly (a solid window). The trailing None is
    // the explicit opaque fallback.
    private static WindowTransparencyLevel[] PreferredLevels() =>
        OperatingSystem.IsWindows()
            ? new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.None }
            : OperatingSystem.IsMacOS()
                ? new[] { WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.None }
                : OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()
                    ? new[] { WindowTransparencyLevel.Blur, WindowTransparencyLevel.None }
                    : SolidOnly;

    private static IBrush SolidBase(Window window) =>
        window.TryFindResource("SolidBackgroundFillColorBaseBrush", window.ActualThemeVariant, out var b) && b is IBrush brush
            ? brush
            : Brushes.Transparent;
}
