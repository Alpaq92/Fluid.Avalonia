using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// A freehand signature pad with a natural, velocity-driven variable-width pen.
/// Ported from <see href="https://github.com/warting/android-signaturepad">warting/android-signaturepad</see>
/// (MIT, © 2021 Stefan Wärting), itself based on gcacace/android-signaturepad (Apache-2.0,
/// © Gianluca Cacace): as the user draws, each stroke is smoothed into cubic Béziers and the pen
/// width follows pointer velocity (faster → thinner, slower → thicker), stamping round dabs along
/// the curve so the ink reads as calligraphic rather than a constant-width line.
/// </summary>
/// <remarks>
/// Derives from <see cref="Control"/> and paints the card surface, hairline, rounded corners and the
/// ink itself straight onto the <see cref="DrawingContext"/> in <see cref="Render"/> (rather than the
/// demo's usual Canvas-of-Shapes), so a long signature — thousands of dabs — stays cheap. The drawn
/// background fill is also what makes the whole surface hit-testable. (<see cref="Border"/> seals
/// <c>Render</c>, so it can't host custom drawing.)
/// </remarks>
public class SignaturePad : Control
{
    /// <summary>Card surface fill (also what makes the pad hit-testable). Themed via the Style.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<SignaturePad, IBrush?>(nameof(Background));

    /// <summary>Hairline border brush. Themed via the Style.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<SignaturePad, IBrush?>(nameof(BorderBrush));

    /// <summary>Uniform border thickness.</summary>
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<SignaturePad, double>(nameof(BorderThickness), 1);

    /// <summary>Corner radius (uniform — the top-left value is applied to all corners). Typed as
    /// <see cref="CornerRadius"/> so the theme's radius tokens (e.g. OverlayCornerRadius) assign.</summary>
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<SignaturePad, CornerRadius>(nameof(CornerRadius), new CornerRadius(8));

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public double BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The pen colour for new strokes. Existing strokes keep the colour they were drawn with.</summary>
    public static readonly StyledProperty<Color> StrokeColorProperty =
        AvaloniaProperty.Register<SignaturePad, Color>(nameof(StrokeColor), Colors.Black);

    /// <summary>The thinnest the pen gets (fast strokes), in device-independent pixels.</summary>
    public static readonly StyledProperty<double> MinStrokeWidthProperty =
        AvaloniaProperty.Register<SignaturePad, double>(nameof(MinStrokeWidth), 2.5);

    /// <summary>The thickest the pen gets (slow strokes), in device-independent pixels.</summary>
    public static readonly StyledProperty<double> MaxStrokeWidthProperty =
        AvaloniaProperty.Register<SignaturePad, double>(nameof(MaxStrokeWidth), 7.0);

    /// <summary>How heavily velocity is smoothed between points (0..1; the upstream default is 0.9).</summary>
    public static readonly StyledProperty<double> VelocityFilterWeightProperty =
        AvaloniaProperty.Register<SignaturePad, double>(nameof(VelocityFilterWeight), 0.9);

    /// <summary>True until something is drawn; reset by <see cref="Clear"/>.</summary>
    public static readonly DirectProperty<SignaturePad, bool> IsEmptyProperty =
        AvaloniaProperty.RegisterDirect<SignaturePad, bool>(nameof(IsEmpty), o => o.IsEmpty);

    public Color StrokeColor { get => GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }
    public double MinStrokeWidth { get => GetValue(MinStrokeWidthProperty); set => SetValue(MinStrokeWidthProperty, value); }
    public double MaxStrokeWidth { get => GetValue(MaxStrokeWidthProperty); set => SetValue(MaxStrokeWidthProperty, value); }
    public double VelocityFilterWeight { get => GetValue(VelocityFilterWeightProperty); set => SetValue(VelocityFilterWeightProperty, value); }

    static SignaturePad() =>
        AffectsRender<SignaturePad>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty);

    private bool _isEmpty = true;
    public bool IsEmpty { get => _isEmpty; private set => SetAndRaise(IsEmptyProperty, ref _isEmpty, value); }

    private readonly List<Stroke> _strokes = new();
    private readonly List<TimedPoint> _points = new();   // sliding window for the Bézier smoothing
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private Stroke? _current;
    private IPointer? _activePointer;   // the pointer that owns the in-progress stroke (multi-touch guard)
    private double _lastWidth;
    private double _lastVelocity;

    /// <summary>Wipe the pad.</summary>
    public void Clear()
    {
        _strokes.Clear();
        _current = null;
        _activePointer = null;
        _points.Clear();
        _lastVelocity = 0;
        _lastWidth = (MinStrokeWidth + MaxStrokeWidth) / 2;
        IsEmpty = true;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // One stroke at a time: ignore a second finger / pointer while one is already drawing
        // (otherwise it would overwrite the in-progress stroke and corrupt both).
        if (_current is not null)
            return;

        // Mouse: only the left button draws. Touch / pen always draw.
        if (e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(this);
        _activePointer = e.Pointer;
        _current = new Stroke(new ImmutableSolidColorBrush(StrokeColor));
        _points.Clear();
        _lastVelocity = 0;
        _lastWidth = (MinStrokeWidth + MaxStrokeWidth) / 2;
        AddPoint(NewPoint(e.GetPosition(this)));
        IsEmpty = false;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_current is null || e.Pointer != _activePointer)
            return;   // not drawing, or a different pointer (hover / second finger)
        AddPoint(NewPoint(e.GetPosition(this)));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_current is null || e.Pointer != _activePointer)
            return;

        AddPoint(NewPoint(e.GetPosition(this)));
        // Finalize before releasing capture (Capture(null) re-enters OnPointerCaptureLost, which the
        // null _activePointer then makes a no-op).
        FinalizeStroke(e.GetPosition(this));
        e.Pointer.Capture(null);
    }

    // Capture can be revoked mid-stroke without a release — a popup/menu opening, another control
    // grabbing the pointer, or a touch cancel. Finalize what's drawn so we don't leave a dangling
    // _current that would append phantom ink on the next hover move (and stick IsEmpty at false).
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_current is null || e.Pointer != _activePointer)
            return;
        FinalizeStroke(null);   // capture is already gone — don't release it again
    }

    // Commits the in-progress stroke. A tap (no curve produced) leaves a single round dot, but only
    // on a real release (tap != null) — an interrupted empty stroke is dropped. IsEmpty is recomputed
    // so an interrupted blank stroke doesn't leave the pad stuck "not empty".
    private void FinalizeStroke(Point? tap)
    {
        if (_current is null)
            return;

        if (_current.Dabs.Count == 0 && tap is { } p)
            _current.Dabs.Add(new Dab(p.X, p.Y, (MinStrokeWidth + MaxStrokeWidth) / 4));

        if (_current.Dabs.Count > 0)
            _strokes.Add(_current);

        _current = null;
        _activePointer = null;
        IsEmpty = _strokes.Count == 0;
        InvalidateVisual();
    }

    private TimedPoint NewPoint(Point p) => new(p.X, p.Y, _clock.Elapsed.TotalMilliseconds);

    // Faithful port of SignaturePad.addPoint: keep a sliding window of points and, once we have
    // four, draw the Bézier curve between the middle two with a velocity-derived width.
    private void AddPoint(TimedPoint newPoint)
    {
        _points.Add(newPoint);
        var count = _points.Count;
        if (count > 3)
        {
            var (_, c2) = CalculateCurveControlPoints(_points[0], _points[1], _points[2]);
            var (c3, _) = CalculateCurveControlPoints(_points[1], _points[2], _points[3]);
            var curve = new Bezier(_points[1], c2, c3, _points[2]);

            var velocity = curve.End.VelocityFrom(curve.Start);
            velocity = double.IsNaN(velocity) ? 0 : velocity;
            velocity = VelocityFilterWeight * velocity + (1 - VelocityFilterWeight) * _lastVelocity;

            var newWidth = StrokeWidthFor(velocity);
            AddBezier(curve, _lastWidth, newWidth);
            _lastVelocity = velocity;
            _lastWidth = newWidth;

            _points.RemoveAt(0);
        }
        else if (count == 1)
        {
            // Duplicate the first point so the four-point window can fill from a single press.
            var f = _points[0];
            _points.Add(new TimedPoint(f.X, f.Y, f.T));
        }
    }

    private double StrokeWidthFor(double velocity) => Math.Max(MaxStrokeWidth / (velocity + 1), MinStrokeWidth);

    // Stamp round dabs along the curve; the width interpolates start→end weighted by t³ and the step
    // count is ceil(curve length), matching warting's fork (the original gcacace used floor).
    private void AddBezier(Bezier curve, double startWidth, double endWidth)
    {
        if (_current is null)
            return;

        var widthDelta = endWidth - startWidth;
        var drawSteps = (int)Math.Ceiling(curve.Length());
        for (var i = 0; i < drawSteps; i++)
        {
            var t = (double)i / drawSteps;
            var tt = t * t;
            var ttt = tt * t;
            var u = 1 - t;
            var uu = u * u;
            var uuu = uu * u;

            var x = (uuu * curve.Start.X) + (3 * uu * t * curve.C1.X) + (3 * u * tt * curve.C2.X) + (ttt * curve.End.X);
            var y = (uuu * curve.Start.Y) + (3 * uu * t * curve.C1.Y) + (3 * u * tt * curve.C2.Y) + (ttt * curve.End.Y);

            var width = startWidth + (ttt * widthDelta);
            _current.Dabs.Add(new Dab(x, y, width / 2));   // round dab: diameter = pen width
        }
    }

    // The midpoint construction from the upstream library: control points that keep the curve smooth
    // across the three sample points, biased by the relative segment lengths.
    private static (TimedPoint C1, TimedPoint C2) CalculateCurveControlPoints(TimedPoint s1, TimedPoint s2, TimedPoint s3)
    {
        double dx1 = s1.X - s2.X, dy1 = s1.Y - s2.Y;
        double dx2 = s2.X - s3.X, dy2 = s2.Y - s3.Y;
        double m1X = (s1.X + s2.X) / 2, m1Y = (s1.Y + s2.Y) / 2;
        double m2X = (s2.X + s3.X) / 2, m2Y = (s2.Y + s3.Y) / 2;

        var l1 = Math.Sqrt((dx1 * dx1) + (dy1 * dy1));
        var l2 = Math.Sqrt((dx2 * dx2) + (dy2 * dy2));
        var k = l2 / (l1 + l2);
        if (double.IsNaN(k))
            k = 0;

        double cmX = m2X + ((m1X - m2X) * k), cmY = m2Y + ((m1Y - m2Y) * k);
        double tx = s2.X - cmX, ty = s2.Y - cmY;
        return (new TimedPoint(m1X + tx, m1Y + ty, s2.T), new TimedPoint(m2X + tx, m2Y + ty, s2.T));
    }

    public override void Render(DrawingContext context)
    {
        var thickness = BorderThickness;
        var radius = CornerRadius.TopLeft;
        var full = new Rect(Bounds.Size);

        // Card surface + hairline (the filled rect is also what makes the pad hit-testable). The
        // border stroke is centred on the edge, so inset the rect by half its thickness to keep it in.
        var pen = BorderBrush is { } bb && thickness > 0 ? new Pen(bb, thickness) : null;
        var half = thickness / 2;
        context.DrawRectangle(Background, pen, new RoundedRect(full.Deflate(half), Math.Max(0, radius - half)));

        if (_strokes.Count == 0 && _current is null)
            return;

        // Clip the ink to the rounded surface, inset by the full border so it never paints over the frame.
        using (context.PushClip(new RoundedRect(full.Deflate(thickness), Math.Max(0, radius - thickness))))
        {
            foreach (var stroke in _strokes)
                DrawStroke(context, stroke);
            if (_current is not null)
                DrawStroke(context, _current);
        }
    }

    private static void DrawStroke(DrawingContext context, Stroke stroke)
    {
        foreach (var d in stroke.Dabs)
            context.DrawEllipse(stroke.Brush, null, new Point(d.X, d.Y), d.R, d.R);
    }

    // One drawn stroke: the round dabs that make it up, plus the (snapshotted) ink colour.
    private sealed class Stroke(IBrush brush)
    {
        public IBrush Brush { get; } = brush;
        public List<Dab> Dabs { get; } = new();
    }

    private readonly record struct Dab(double X, double Y, double R);

    private readonly record struct TimedPoint(double X, double Y, double T)
    {
        public double VelocityFrom(TimedPoint start)
        {
            var diff = T - start.T;
            if (diff <= 0)
                diff = 1;   // guard a zero/negative time delta, like the upstream
            var v = Distance(start) / diff;
            return double.IsInfinity(v) || double.IsNaN(v) ? 0 : v;
        }

        private double Distance(TimedPoint o)
        {
            var dx = o.X - X;
            var dy = o.Y - Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }

    private readonly record struct Bezier(TimedPoint Start, TimedPoint C1, TimedPoint C2, TimedPoint End)
    {
        // Approximate arc length by sampling 10 segments (the upstream value).
        public double Length()
        {
            const int steps = 10;
            double length = 0, px = 0, py = 0;
            for (var i = 0; i <= steps; i++)
            {
                var t = (double)i / steps;
                var cx = Cubic(t, Start.X, C1.X, C2.X, End.X);
                var cy = Cubic(t, Start.Y, C1.Y, C2.Y, End.Y);
                if (i > 0)
                {
                    var dx = cx - px;
                    var dy = cy - py;
                    length += Math.Sqrt((dx * dx) + (dy * dy));
                }
                px = cx;
                py = cy;
            }
            return length;
        }

        private static double Cubic(double t, double start, double c1, double c2, double end)
        {
            var u = 1 - t;
            return (start * u * u * u) + (3 * c1 * u * u * t) + (3 * c2 * u * t * t) + (end * t * t * t);
        }
    }
}
