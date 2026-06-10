using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// Shared geometry + rendering for the radial controls (<see cref="RadialClock"/> /
/// <see cref="RadialTimePicker"/>, <see cref="RadialSlider"/>, <see cref="ProgressCircle"/>). Each ring
/// is "a Fluent slider bent into a circle": an accent fill arc from 12 o'clock plus a complementary
/// track arc, both sampled as polylines on the exact radius (so a ring never appears to jump radius
/// the way an SVG arc can), with the thumb / fill sharing one brush. Pulled out of the controls so the
/// look and the maths are defined in exactly one place.
/// </summary>
internal static class RadialDial
{
    /// <summary>Default stroke width of a ring (the rail / fill), matching the original dial.</summary>
    public const double TrackThickness = 4;

    /// <summary>Diameter of a draggable thumb.</summary>
    public const double ThumbDiameter = 20;

    /// <summary>A point on a circle of radius <paramref name="r"/> about (center, center), measured
    /// clockwise from 12 o'clock.</summary>
    public static Point OnCircle(double center, double r, double angleDeg)
    {
        var a = angleDeg * Math.PI / 180.0;
        return new Point(center + r * Math.Sin(a), center - r * Math.Cos(a));
    }

    /// <summary>Distance of <paramref name="p"/> from the centre (used for ring hit-testing / hover).</summary>
    public static double DistanceFromCenter(double center, Point p) =>
        Math.Sqrt((p.X - center) * (p.X - center) + (p.Y - center) * (p.Y - center));

    /// <summary>How far <paramref name="value"/> sits in [<paramref name="min"/>, <paramref name="max"/>],
    /// clamped to [0, 1] (0 when the range is empty) — the fill fraction of a ring.</summary>
    public static double Fraction(double value, double min, double max)
    {
        var range = max - min;
        if (range <= 0)
            return 0;
        var f = (value - min) / range;
        return f < 0 ? 0 : f > 1 ? 1 : f;
    }

    /// <summary>Pointer position → angle in degrees [0, 360), clockwise from 12 o'clock.</summary>
    public static double AngleAt(double center, Point p)
    {
        var ang = Math.Atan2(p.X - center, -(p.Y - center)) * 180.0 / Math.PI;
        return ang < 0 ? ang + 360 : ang;
    }

    /// <summary>Append one arc (a polyline sampled every 2°) onto the canvas.</summary>
    public static void AddArc(Canvas canvas, double center, double r, double startDeg, double endDeg, IBrush stroke, double thickness)
    {
        if (endDeg - startDeg <= 0.01)
            return;

        var line = new Polyline
        {
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeLineCap = PenLineCap.Flat,
            StrokeJoin = PenLineJoin.Round,
        };
        for (var a = startDeg; a < endDeg; a += 2.0)
            line.Points.Add(OnCircle(center, r, a));
        line.Points.Add(OnCircle(center, r, endDeg));
        canvas.Children.Add(line);
    }

    /// <summary>Draw a full ring: accent fill from 0..<paramref name="angle"/>, track from
    /// <paramref name="angle"/>..360 (complementary, so the two never overlap).</summary>
    public static void DrawRing(Canvas canvas, double center, double r, double angle, IBrush fill, IBrush track, double thickness)
    {
        if (angle <= 0.01)
        {
            AddArc(canvas, center, r, 0, 360, track, thickness);
            return;
        }
        AddArc(canvas, center, r, 0, angle, fill, thickness);
        AddArc(canvas, center, r, angle, 360, track, thickness);
    }

    /// <summary>A solid copy of a brush — drops any 0.8/0.9 opacity the stock Slider state brushes carry
    /// (which would otherwise let the backing disc bleed through a thumb / fill).</summary>
    public static IBrush Opaque(IBrush b) =>
        b is ISolidColorBrush s ? new SolidColorBrush(s.Color) : b;
}

/// <summary>Resource helpers for the radial controls, resolved against the control's live theme variant.</summary>
internal static class RadialControlExtensions
{
    /// <summary>Resolve a brush resource for the control's current theme variant, or a fallback.</summary>
    public static IBrush Resource(this Control c, string key, IBrush fallback) =>
        c.TryFindResource(key, c.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    /// <summary>The stock Slider thumb/fill brush for the given interaction state, made opaque — so a
    /// radial ring's fill and thumb move through default → hover → pressed exactly like a real Slider.</summary>
    public static IBrush RingBrush(this Control c, bool pressed, bool hover)
    {
        var fallback = c.Resource("AccentFillColorDefaultBrush", Brushes.DodgerBlue);
        var key = pressed ? "SliderThumbBackgroundPressed"
            : hover ? "SliderThumbBackgroundPointerOver"
            : "SliderThumbBackground";
        return RadialDial.Opaque(c.Resource(key, fallback));
    }
}
