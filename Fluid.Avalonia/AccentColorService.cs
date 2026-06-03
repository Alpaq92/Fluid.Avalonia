using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Win32;

namespace Fluid.Avalonia;

/// <summary>
/// Sources the accent color ramp and publishes it as the <c>SystemAccentColor</c> /
/// <c>SystemAccentColor{Light,Dark}{1,2,3}</c> resources that FluentTheme's accent brushes
/// resolve through <c>DynamicResource</c>.
///
/// Accent resolution order:
/// <list type="number">
///   <item>An explicit override set via <see cref="SetAccent"/> (e.g. a preset or a color the
///   user picked) — lets apps offer manual accent selection on any OS.</item>
///   <item>The live OS accent, read natively per-platform: Windows (<c>AccentPalette</c>,
///   full seven-shade ramp), macOS (<c>AppleAccentColor</c>), Linux (GNOME
///   <c>accent-color</c> or KDE <c>kdeglobals</c>).</item>
///   <item>Avalonia's own <see cref="IPlatformSettings"/> accent (covers platforms/portals
///   the native readers miss).</item>
///   <item>A neutral Fluent blue, so the app always has a sensible accent.</item>
/// </list>
/// For single-color sources the six shades are derived with the same HSL lightness steps
/// FluentTheme uses, so the ramp stays consistent with the rest of the theme.
/// </summary>
public static class AccentColorService
{
    private const string AccentKey = "SystemAccentColor";
    private const string Light1Key = "SystemAccentColorLight1";
    private const string Light2Key = "SystemAccentColorLight2";
    private const string Light3Key = "SystemAccentColorLight3";
    private const string Dark1Key = "SystemAccentColorDark1";
    private const string Dark2Key = "SystemAccentColorDark2";
    private const string Dark3Key = "SystemAccentColorDark3";

    private static readonly Color FallbackAccent = Color.FromRgb(0, 120, 215);

    private static bool _subscribed;
    private static Color? _override;
    private static Application? _app;

    /// <summary>A named accent color users can pick from when the OS accent isn't available
    /// (or they just want a different one).</summary>
    public readonly record struct AccentPreset(string Name, Color Color);

    /// <summary>
    /// A Metro-inspired set of 20 accent presets (the Windows 8 "Metro" UI palette,
    /// https://www.w3schools.com/colors/colors_metro.asp), offered as a manual alternative to the
    /// system accent. Ordered around the wheel through the earth tones, ending with a light and a
    /// dark neutral so there's always a bright and a muted option.
    /// </summary>
    public static IReadOnlyList<AccentPreset> Presets { get; } = new[]
    {
        new AccentPreset("Red", Color.FromRgb(0xE5, 0x14, 0x00)),
        new AccentPreset("Orange", Color.FromRgb(0xFA, 0x68, 0x00)),
        new AccentPreset("Amber", Color.FromRgb(0xF0, 0xA3, 0x0A)),
        new AccentPreset("Yellow", Color.FromRgb(0xE3, 0xC8, 0x00)),
        new AccentPreset("Lime", Color.FromRgb(0xA4, 0xC4, 0x00)),
        new AccentPreset("Green", Color.FromRgb(0x60, 0xA9, 0x17)),
        new AccentPreset("Emerald", Color.FromRgb(0x00, 0x8A, 0x00)),
        new AccentPreset("Teal", Color.FromRgb(0x00, 0xAB, 0xA9)),
        new AccentPreset("Cyan", Color.FromRgb(0x1B, 0xA1, 0xE2)),
        new AccentPreset("Cobalt", Color.FromRgb(0x00, 0x50, 0xEF)),
        new AccentPreset("Indigo", Color.FromRgb(0x6A, 0x00, 0xFF)),
        new AccentPreset("Violet", Color.FromRgb(0xAA, 0x00, 0xFF)),
        new AccentPreset("Pink", Color.FromRgb(0xF4, 0x72, 0xD0)),
        new AccentPreset("Magenta", Color.FromRgb(0xD8, 0x00, 0x73)),
        new AccentPreset("Brown", Color.FromRgb(0x82, 0x5A, 0x2C)),
        new AccentPreset("Olive", Color.FromRgb(0x6D, 0x87, 0x64)),
        new AccentPreset("Steel", Color.FromRgb(0x64, 0x76, 0x87)),
        new AccentPreset("Mauve", Color.FromRgb(0x76, 0x60, 0x8A)),
        new AccentPreset("Silver", Color.FromRgb(0xC8, 0xC8, 0xC8)),
        new AccentPreset("Charcoal", Color.FromRgb(0x1D, 0x1D, 0x1D)),
    };

    /// <summary>
    /// Applies the current accent ramp to the application's resources and keeps it in sync
    /// with later OS accent / theme changes. Call once after the FluentTheme has been added to
    /// <see cref="Application.Styles"/> (e.g. from <c>FluidTheme</c>).
    /// </summary>
    public static void Apply(Application app)
    {
        _app = app;
        Refresh(app);

        if (_subscribed)
            return;

        var settings = app.PlatformSettings;
        if (settings is not null)
        {
            settings.ColorValuesChanged += (_, _) => Dispatcher.UIThread.Post(() => Refresh(app));
            _subscribed = true;
        }
    }

    /// <summary>Overrides the accent with a specific color (e.g. a preset or a picked color)
    /// and republishes the ramp. Pass through <see cref="UseSystemAccent"/> to clear it.</summary>
    public static void SetAccent(Color accent)
    {
        _override = accent;
        if (_app is { } app)
            Refresh(app);
    }

    /// <summary>Clears any manual override and reverts to the live OS accent.</summary>
    public static void UseSystemAccent()
    {
        _override = null;
        if (_app is { } app)
            Refresh(app);
    }

    /// <summary>The accent color currently published to the application resources.</summary>
    public static Color CurrentAccent =>
        _app?.Resources.TryGetResource(AccentKey, null, out var v) == true && v is Color c
            ? c
            : FallbackAccent;

    private static void Refresh(Application app)
    {
        var ramp =
            (_override is { } o ? RampFromSeed(o) : (Ramp?)null) ??
            ReadWindowsPalette() ??
            ReadMacOsAccent() ??
            ReadLinuxAccent() ??
            DeriveFromPlatform(app.PlatformSettings) ??
            RampFromSeed(FallbackAccent);

        var r = ramp;
        Set(app, AccentKey, r.Accent);
        Set(app, Light1Key, r.Light1);
        Set(app, Light2Key, r.Light2);
        Set(app, Light3Key, r.Light3);
        Set(app, Dark1Key, r.Dark1);
        Set(app, Dark2Key, r.Dark2);
        Set(app, Dark3Key, r.Dark3);
    }

    private static void Set(Application app, string key, Color value)
        => app.Resources[key] = value;

    private readonly record struct Ramp(
        Color Light3, Color Light2, Color Light1,
        Color Accent,
        Color Dark1, Color Dark2, Color Dark3);

    /// <summary>Generates the six shades around a single seed color using the same HSL
    /// lightness steps FluentTheme applies, so derived ramps match the built-in look.</summary>
    private static Ramp RampFromSeed(Color accent)
    {
        var hsl = accent.ToHsl();

        Color Shade(double deltaL) =>
            new HslColor(1, hsl.H, hsl.S, System.Math.Clamp(hsl.L + deltaL, 0, 1)).ToRgb();

        return new Ramp(
            Light3: Shade(103 / 255d),
            Light2: Shade(70 / 255d),
            Light1: Shade(39 / 255d),
            Accent: accent,
            Dark1: Shade(-28.5 / 255d),
            Dark2: Shade(-49 / 255d),
            Dark3: Shade(-74.5 / 255d));
    }

    // ===== Windows =====

    /// <summary>
    /// Reads the eight-color <c>AccentPalette</c> blob written by Windows. Layout is eight
    /// RGBA quads: Light3, Light2, Light1, Accent, Dark1, Dark2, Dark3, and a trailing
    /// high-contrast color we ignore.
    /// </summary>
    private static Ramp? ReadWindowsPalette()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (key?.GetValue("AccentPalette") is not byte[] blob || blob.Length < 28)
                return null;

            // Windows stores each entry as RGBA but leaves the alpha byte 0, so force opaque.
            Color At(int index)
            {
                var o = index * 4;
                return Color.FromArgb(0xFF, blob[o], blob[o + 1], blob[o + 2]);
            }

            return new Ramp(At(0), At(1), At(2), At(3), At(4), At(5), At(6));
        }
        catch
        {
            return null;
        }
    }

    // ===== macOS =====

    /// <summary>
    /// macOS stores the accent as an integer enum under the global <c>AppleAccentColor</c>
    /// default. We map it to the system accent swatch and derive the ramp. No key (or the
    /// "multicolor" value) means the default blue.
    /// </summary>
    private static Ramp? ReadMacOsAccent()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        try
        {
            var raw = RunProcess("defaults", "read -g AppleAccentColor");
            // -1 graphite, 0 red, 1 orange, 2 yellow, 3 green, 4 blue, 5 purple, 6 pink.
            var accent = (raw?.Trim()) switch
            {
                "-1" => Color.FromRgb(0x98, 0x98, 0x9D), // graphite
                "0" => Color.FromRgb(0xFF, 0x5A, 0x52),  // red
                "1" => Color.FromRgb(0xF7, 0x82, 0x1B),  // orange
                "2" => Color.FromRgb(0xFF, 0xC4, 0x09),  // yellow
                "3" => Color.FromRgb(0x62, 0xBA, 0x46),  // green
                "5" => Color.FromRgb(0xA0, 0x50, 0xBE),  // purple
                "6" => Color.FromRgb(0xF7, 0x4F, 0x9E),  // pink
                _ => Color.FromRgb(0x00, 0x7A, 0xFF),    // blue / multicolor / unset
            };
            return RampFromSeed(accent);
        }
        catch
        {
            return null;
        }
    }

    // ===== Linux =====

    /// <summary>
    /// Linux accent is desktop-environment specific. We try GNOME's
    /// <c>org.gnome.desktop.interface accent-color</c> (named hues, GNOME 47+) first, then KDE's
    /// custom <c>AccentColor</c> in <c>kdeglobals</c>, then Cinnamon (Linux Mint), which has no
    /// dedicated accent setting but encodes it in the theme name. Returns null if none is set.
    /// </summary>
    private static Ramp? ReadLinuxAccent()
    {
        if (!OperatingSystem.IsLinux())
            return null;

        return ReadGnomeAccent() ?? ReadKdeAccent() ?? ReadCinnamonAccent();
    }

    /// <summary>
    /// Cinnamon / Linux Mint has no accent-color key; instead Mint-Y / Mint-L themes carry the
    /// accent in their name (e.g. <c>Mint-Y-Dark-Aqua</c>). We read the active GTK/Cinnamon theme
    /// and map the trailing color word to the corresponding Mint accent (defaulting to Mint green).
    /// </summary>
    private static Ramp? ReadCinnamonAccent()
    {
        try
        {
            var raw = RunProcess("gsettings", "get org.cinnamon.desktop.interface gtk-theme")
                      ?? RunProcess("gsettings", "get org.cinnamon.theme name");
            var theme = raw?.Trim().Trim('\'', '"');
            if (string.IsNullOrEmpty(theme))
                return null;

            // Only treat it as a Mint theme accent if it actually looks like one.
            if (theme.IndexOf("Mint", StringComparison.OrdinalIgnoreCase) < 0)
                return null;

            bool Has(string word) => theme.EndsWith(word, StringComparison.OrdinalIgnoreCase);

            var accent =
                Has("Aqua") ? Color.FromRgb(0x4D, 0xB6, 0xAC) :
                Has("Teal") ? Color.FromRgb(0x26, 0xA6, 0x9A) :
                Has("Blue") ? Color.FromRgb(0x52, 0x94, 0xE2) :
                Has("Purple") ? Color.FromRgb(0x9C, 0x7B, 0xB0) :
                Has("Pink") ? Color.FromRgb(0xE6, 0x8F, 0xAC) :
                Has("Red") ? Color.FromRgb(0xE0, 0x4F, 0x5F) :
                Has("Orange") ? Color.FromRgb(0xE5, 0x8E, 0x3A) :
                Has("Brown") ? Color.FromRgb(0xA1, 0x88, 0x7F) :
                Has("Sand") ? Color.FromRgb(0xC9, 0xA2, 0x27) :
                Has("Grey") || Has("Gray") ? Color.FromRgb(0x9E, 0x9E, 0x9E) :
                Color.FromRgb(0x8F, 0xA8, 0x76); // Mint-Y default green

            return RampFromSeed(accent);
        }
        catch
        {
            return null;
        }
    }

    private static Ramp? ReadGnomeAccent()
    {
        try
        {
            var raw = RunProcess("gsettings", "get org.gnome.desktop.interface accent-color");
            var name = raw?.Trim().Trim('\'', '"');
            // GNOME 47 accent palette.
            Color? accent = name switch
            {
                "blue" => Color.FromRgb(0x35, 0x84, 0xe4),
                "teal" => Color.FromRgb(0x21, 0x90, 0xa4),
                "green" => Color.FromRgb(0x3a, 0x94, 0x4a),
                "yellow" => Color.FromRgb(0xc8, 0x88, 0x00),
                "orange" => Color.FromRgb(0xed, 0x5b, 0x00),
                "red" => Color.FromRgb(0xe6, 0x2d, 0x42),
                "pink" => Color.FromRgb(0xd5, 0x61, 0x99),
                "purple" => Color.FromRgb(0x91, 0x41, 0xac),
                "slate" => Color.FromRgb(0x6f, 0x83, 0x96),
                _ => null,
            };
            return accent is { } c ? RampFromSeed(c) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Ramp? ReadKdeAccent()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var path = Path.Combine(home, ".config", "kdeglobals");
            if (!File.Exists(path))
                return null;

            // Look for "AccentColor=r,g,b" (set when the user picks a custom accent).
            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("AccentColor=", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = trimmed["AccentColor=".Length..].Split(',');
                if (parts.Length >= 3 &&
                    byte.TryParse(parts[0], out var r) &&
                    byte.TryParse(parts[1], out var g) &&
                    byte.TryParse(parts[2], out var b))
                {
                    return RampFromSeed(Color.FromRgb(r, g, b));
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ===== Avalonia platform fallback =====

    private static Ramp? DeriveFromPlatform(IPlatformSettings? settings)
    {
        var accent = settings?.GetColorValues().AccentColor1;
        return accent is { } c ? RampFromSeed(c) : null;
    }

    private static string? RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var p = Process.Start(psi);
        if (p is null)
            return null;

        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(2000);
        return p.ExitCode == 0 ? output : null;
    }
}
