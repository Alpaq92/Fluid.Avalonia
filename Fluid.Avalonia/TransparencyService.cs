using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;
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

    // The per-window DynamicResource subscription on Background for the solid case. Tracked so it can
    // be torn down when a real backdrop takes over (then the window must be Transparent, not the base).
    private static readonly ConditionalWeakTable<Window, IDisposable> SolidBackgroundBindings = new();

    /// <summary>Reconciles the window background to the granted transparency level: transparent (so a
    /// REAL backdrop shows through) ONLY when one is active — Mica, macOS vibrancy (<c>AcrylicBlur</c>)
    /// or KWin <c>Blur</c> — otherwise the solid base surface. The solid case is BOUND to the theme
    /// resource (not assigned a one-shot brush) so it tracks the active theme variant exactly like the
    /// content/text do; a concrete brush snapshots a single variant and goes stale, which on Linux —
    /// where Avalonia may settle the OS dark scheme only after the window is up, with no later event —
    /// left a light base under dark-variant (light) text: unreadable. A bare blur-less
    /// <see cref="WindowTransparencyLevel.Transparent"/> (Avalonia's X11 fallback on non-KWin compositing
    /// desktops) is treated as no-backdrop, so it's painted solid too. Call from the window's
    /// <c>OnOpened</c> and whenever its <see cref="TopLevel.ActualTransparencyLevel"/> changes.</summary>
    public static void ReconcileBackground(Window window)
    {
        // Drop any prior solid-background binding before re-deciding.
        if (SolidBackgroundBindings.TryGetValue(window, out var existing))
        {
            existing.Dispose();
            SolidBackgroundBindings.Remove(window);
        }

        if (IsRealBackdrop(window.ActualTransparencyLevel))
        {
            window.Background = Brushes.Transparent;
            return;
        }

        // Bind, don't assign: DynamicResource re-resolves on every theme-variant change, so the
        // surface always matches the live variant (no stale snapshot, no dependence on a flip event).
        SolidBackgroundBindings.Add(window, window.Bind(
            TemplatedControl.BackgroundProperty,
            new DynamicResourceExtension("SolidBackgroundFillColorBaseBrush")));
    }

    /// <summary>Whether a real translucent backdrop is achievable for <paramref name="window"/> in this
    /// environment — so a "use the backdrop" toggle is meaningful. Returns <see langword="false"/> when a
    /// backdrop was requested but the platform could only grant a bare blur-less
    /// <see cref="WindowTransparencyLevel.Transparent"/> (e.g. a non-KWin Linux desktop, where the window
    /// just stays solid) — letting callers disable that toggle. <see cref="WindowTransparencyLevel.None"/>
    /// is left as "available" (it's the state when the backdrop is simply switched off, or a Windows 11
    /// window with the OS "Transparency effects" setting off but Mica still capable). The window must be
    /// open so its granted <see cref="TopLevel.ActualTransparencyLevel"/> has resolved.</summary>
    public static bool IsBackdropSupported(Window window) =>
        window.ActualTransparencyLevel != WindowTransparencyLevel.Transparent;

    private static bool IsRealBackdrop(WindowTransparencyLevel level) =>
        level == WindowTransparencyLevel.Mica
        || level == WindowTransparencyLevel.AcrylicBlur
        || level == WindowTransparencyLevel.Blur;

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
}
