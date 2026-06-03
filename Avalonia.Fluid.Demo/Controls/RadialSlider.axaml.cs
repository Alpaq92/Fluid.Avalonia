using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// A circular slider: a single Fluent "slider bent into a circle" ring whose accent fill + draggable
/// thumb sweep clockwise from 12 o'clock to represent <see cref="Value"/> in [<see cref="Minimum"/>,
/// <see cref="Maximum"/>], with the value in the centre. Built on <see cref="RadialDial"/>, so the
/// rail / fill / thumb and the default / hover / pressed states match the stock Slider and the
/// RadialTimePicker dial. <see cref="Value"/> snaps to whole units as you drag.
/// </summary>
public partial class RadialSlider : UserControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RadialSlider, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RadialSlider, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RadialSlider, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>The selected value (two-way; snaps to whole units while dragging).</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private const double C = 70, R = 56, HoverBand = 18;
    private bool _pressed, _hover;

    public RadialSlider()
    {
        InitializeComponent();

        Face.PointerPressed += OnDown;
        Face.PointerMoved += OnMove;
        Face.PointerReleased += OnUp;
        Face.PointerExited += (_, _) => SetHover(false);

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

    private double Fraction()
    {
        var range = Maximum - Minimum;
        if (range <= 0)
            return 0;
        var f = (Value - Minimum) / range;
        return f < 0 ? 0 : f > 1 ? 1 : f;
    }

    private void OnDown(object? s, PointerPressedEventArgs e)
    {
        _pressed = true;
        e.Pointer.Capture(Face);
        SetValueFrom(e.GetPosition(Face));
    }

    private void OnMove(object? s, PointerEventArgs e)
    {
        if (_pressed)
            SetValueFrom(e.GetPosition(Face));
        else
            SetHover(Math.Abs(RadialDial.DistanceFromCenter(C, e.GetPosition(Face)) - R) <= HoverBand);
    }

    private void OnUp(object? s, PointerReleasedEventArgs e)
    {
        _pressed = false;
        e.Pointer.Capture(null);
        _hover = Math.Abs(RadialDial.DistanceFromCenter(C, e.GetPosition(Face)) - R) <= HoverBand;
        Render();   // drop the pressed brush
    }

    private void SetHover(bool hover)
    {
        if (hover == _hover)
            return;
        _hover = hover;
        Render();
    }

    private void SetValueFrom(Point p)
    {
        var f = RadialDial.AngleAt(C, p) / 360.0;
        Value = Math.Round(Minimum + f * (Maximum - Minimum));   // OnPropertyChanged → Render
    }

    private void Render()
    {
        if (Face is null)
            return;

        Face.Children.Clear();

        var track = this.Resource("ControlStrongFillColorDefaultBrush", Brushes.Gray);
        var brush = this.RingBrush(_pressed, _hover);
        var angle = Fraction() * 360.0;

        RadialDial.DrawRing(Face, C, R, angle, brush, track, RadialDial.TrackThickness);

        var thumb = new Ellipse
        {
            Width = RadialDial.ThumbDiameter,
            Height = RadialDial.ThumbDiameter,
            Fill = brush,
        };
        var tp = RadialDial.OnCircle(C, R, angle);
        Canvas.SetLeft(thumb, tp.X - RadialDial.ThumbDiameter / 2);
        Canvas.SetTop(thumb, tp.Y - RadialDial.ThumbDiameter / 2);
        Face.Children.Add(thumb);

        ValueText.Text = ((int)Math.Round(Value)).ToString();
    }
}
