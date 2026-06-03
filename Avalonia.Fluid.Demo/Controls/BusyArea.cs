using Avalonia;
using Avalonia.Controls;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>The progress indicator a <see cref="BusyArea"/> shows while busy.</summary>
public enum BusyIndicatorStyle
{
    /// <summary>A circular indeterminate ProgressBar (the theme's "ProgressRing").</summary>
    Ring,

    /// <summary>A horizontal indeterminate ProgressBar.</summary>
    Bar,

    /// <summary>A rotating accent ring drawn from an Arc (a clean circular spinner).</summary>
    Circular,
}

/// <summary>
/// Wraps content and, while <see cref="IsBusy"/> is true, dims it behind a translucent scrim with
/// a progress indicator (and optional <see cref="BusyText"/>). Inspired by SukiUI's BusyArea — a
/// simple way to block interaction and signal "working…" over any region.
/// </summary>
public class BusyArea : ContentControl
{
    public static readonly StyledProperty<bool> IsBusyProperty =
        AvaloniaProperty.Register<BusyArea, bool>(nameof(IsBusy));

    public static readonly StyledProperty<string?> BusyTextProperty =
        AvaloniaProperty.Register<BusyArea, string?>(nameof(BusyText));

    public static readonly StyledProperty<double> ScrimOpacityProperty =
        AvaloniaProperty.Register<BusyArea, double>(nameof(ScrimOpacity), defaultValue: 0.9);

    public static readonly StyledProperty<BusyIndicatorStyle> IndicatorStyleProperty =
        AvaloniaProperty.Register<BusyArea, BusyIndicatorStyle>(nameof(IndicatorStyle));

    /// <summary>When true, the busy overlay (scrim + indicator) covers the content.</summary>
    public bool IsBusy
    {
        get => GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    /// <summary>Optional caption shown under the indicator (e.g. "Loading…").</summary>
    public string? BusyText
    {
        get => GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    /// <summary>Opacity of the scrim that dims the content (0 = transparent, 1 = opaque).</summary>
    public double ScrimOpacity
    {
        get => GetValue(ScrimOpacityProperty);
        set => SetValue(ScrimOpacityProperty, value);
    }

    /// <summary>Which progress indicator to show (ring or bar).</summary>
    public BusyIndicatorStyle IndicatorStyle
    {
        get => GetValue(IndicatorStyleProperty);
        set => SetValue(IndicatorStyleProperty, value);
    }
}
