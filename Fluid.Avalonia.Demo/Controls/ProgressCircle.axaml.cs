using Avalonia.Controls;
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// A determinate radial progress indicator: a track ring with an accent fill arc that sweeps clockwise
/// from 12 o'clock to show how far <see cref="Value"/> has progressed between <see cref="Minimum"/> and
/// <see cref="Maximum"/>, with the percentage in the centre. Built on <see cref="RadialDial"/> (the same
/// ring rendering as the RadialTimePicker dial / <see cref="RadialSlider"/>), but non-interactive and
/// filled with the app accent.
/// </summary>
public partial class ProgressCircle : UserControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ProgressCircle, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ProgressCircle, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProgressCircle, double>(nameof(Value));

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>How far along progress is, between <see cref="Minimum"/> and <see cref="Maximum"/>.</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private const double C = 70, R = 60, Thickness = 6;

    public ProgressCircle()
    {
        InitializeComponent();
        ActualThemeVariantChanged += (_, _) => Render();
        Render();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs c)
    {
        base.OnPropertyChanged(c);
        if (c.Property == ValueProperty || c.Property == MinimumProperty || c.Property == MaximumProperty)
            Render();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Render();   // resolve theme-variant brushes now that we're in the tree
    }

    private void Render()
    {
        if (Face is null)
            return;

        Face.Children.Clear();

        var track = this.Resource("ControlStrongFillColorDefaultBrush", Brushes.Gray);
        var fill = this.Resource("AccentFillColorDefaultBrush", Brushes.DodgerBlue);

        var fraction = RadialDial.Fraction(Value, Minimum, Maximum);
        RadialDial.DrawRing(Face, C, R, fraction * 360.0, fill, track, Thickness);

        ValueText.Text = $"{(int)Math.Round(fraction * 100)}%";
    }
}
