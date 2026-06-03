using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// The concentric ring-slider clock dial, factored out of <see cref="RadialTimePicker"/> so it can be
/// reused (e.g. by <see cref="AnalogDateTimePicker"/>) and shown inline. The INNER ring is the hour,
/// the OUTER ring the minute; each is a Fluent slider bent into a circle (rail + accent fill + thumb
/// that takes the stock Slider brushes through default / hover / pressed — see <see cref="RadialDial"/>).
/// It edits <see cref="Time"/> live as you drag. <see cref="Is24Hour"/> switches to the Material
/// 24-hour clock (0–23, even hours labelled, 0 at top, no AM/PM).
/// </summary>
public partial class RadialClock : UserControl
{
    public static readonly StyledProperty<TimeSpan> TimeProperty =
        AvaloniaProperty.Register<RadialClock, TimeSpan>(
            nameof(Time), defaultValue: new TimeSpan(9, 0, 0), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The live time of day (updated continuously while dragging).</summary>
    public TimeSpan Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<RadialClock, bool>(nameof(Is24Hour));

    /// <summary>When true the hour ring runs 0–23 (15°/hour, even hours labelled, 0 at top) with no AM/PM.</summary>
    public bool Is24Hour { get => GetValue(Is24HourProperty); set => SetValue(Is24HourProperty, value); }

    private const double C = 128, RMin = 110, RNum = 82, RHour = 56, HoverBand = 18;

    private readonly Canvas _parts;
    private readonly Ellipse _hourThumb;
    private readonly Ellipse _minuteThumb;

    private bool _pressed, _draggingMinute, _hourHover, _minuteHover, _syncing, _updating;

    public RadialClock()
    {
        InitializeComponent();

        _parts = new Canvas { Width = 256, Height = 256 };
        Face.Children.Add(_parts);
        _hourThumb = new Ellipse { Width = RadialDial.ThumbDiameter, Height = RadialDial.ThumbDiameter };
        _minuteThumb = new Ellipse { Width = RadialDial.ThumbDiameter, Height = RadialDial.ThumbDiameter };
        Face.Children.Add(_hourThumb);
        Face.Children.Add(_minuteThumb);

        Face.PointerPressed += OnDown;
        Face.PointerMoved += OnMove;
        Face.PointerReleased += OnUp;
        Face.PointerExited += (_, _) => SetHover(false, false);

        AmPmSelector.IsCheckedChanged += (_, _) => OnAmPm();

        ActualThemeVariantChanged += (_, _) => RenderDial();

        ApplyMode();
        Refresh();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs c)
    {
        base.OnPropertyChanged(c);
        if (c.Property == TimeProperty)
        {
            if (_updating)
                return;     // internal edit already refreshed the visuals
            Refresh();      // external seed (host opened its flyout, or a two-way binding pushed a value)
        }
        else if (c.Property == Is24HourProperty)
        {
            ApplyMode();
            Refresh();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RenderDial();   // resolve theme-variant brushes now that we're in the tree
    }

    private void Refresh()
    {
        UpdateHeader();
        SyncAmPm();
        RenderDial();
    }

    private void ApplyMode() => AmPmSelector.IsVisible = !Is24Hour;

    // Set Time from inside the dial without re-entering the external-seed path in OnPropertyChanged.
    private void SetTimeInternal(TimeSpan t)
    {
        _updating = true;
        Time = t;
        _updating = false;
    }

    private void UpdateHeader() =>
        Header.Text = (DateTime.Today + Time).ToString(Is24Hour ? "HH:mm" : "h:mm");

    private void OnAmPm()
    {
        if (_syncing || Is24Hour)
            return;

        var t = Time;
        var pm = AmPmSelector.IsChecked == true;   // right side = PM
        if (!pm && t.Hours >= 12)
            SetTimeInternal(t - TimeSpan.FromHours(12));
        else if (pm && t.Hours < 12)
            SetTimeInternal(t + TimeSpan.FromHours(12));
        UpdateHeader();
        RenderDial();
    }

    private void SyncAmPm()
    {
        if (Is24Hour)
            return;
        _syncing = true;
        AmPmSelector.IsChecked = Time.Hours >= 12;   // true = PM (right)
        _syncing = false;
    }

    // ===== Dial input =====
    private void OnDown(object? s, PointerPressedEventArgs e)
    {
        _pressed = true;
        e.Pointer.Capture(Face);
        var p = e.GetPosition(Face);
        _draggingMinute = RadialDial.DistanceFromCenter(C, p) >= (RMin + RHour) / 2;
        _hourHover = _minuteHover = false;
        HandleDial(p);
    }

    private void OnMove(object? s, PointerEventArgs e)
    {
        if (_pressed) HandleDial(e.GetPosition(Face));
        else UpdateHover(e.GetPosition(Face));
    }

    private void OnUp(object? s, PointerReleasedEventArgs e)
    {
        _pressed = false;
        e.Pointer.Capture(null);
        UpdateHover(e.GetPosition(Face));
        RenderDial();
    }

    private void UpdateHover(Point p)
    {
        var r = RadialDial.DistanceFromCenter(C, p);
        SetHover(Math.Abs(r - RHour) <= HoverBand, Math.Abs(r - RMin) <= HoverBand);
    }

    private void SetHover(bool hour, bool minute)
    {
        if (hour == _hourHover && minute == _minuteHover)
            return;
        _hourHover = hour;
        _minuteHover = minute;
        RenderDial();
    }

    private void HandleDial(Point p)
    {
        var ang = RadialDial.AngleAt(C, p);

        if (_draggingMinute)
        {
            var m = ((int)Math.Round(ang / 6.0)) % 60;
            SetTimeInternal(new TimeSpan(Time.Hours, m, 0));
        }
        else if (Is24Hour)
        {
            var hh = ((int)Math.Round(ang / 15.0)) % 24;
            SetTimeInternal(new TimeSpan(hh, Time.Minutes, 0));
        }
        else
        {
            var idx = ((int)Math.Round(ang / 30.0)) % 12;
            var h12 = idx == 0 ? 12 : idx;
            var pm = Time.Hours >= 12;
            SetTimeInternal(new TimeSpan(h12 % 12 + (pm ? 12 : 0), Time.Minutes, 0));
        }
        UpdateHeader();
        SyncAmPm();
        RenderDial();
    }

    // ===== Dial rendering =====
    private void RenderDial()
    {
        if (_parts is null)
            return;

        _parts.Children.Clear();

        var track = this.Resource("ControlStrongFillColorDefaultBrush", Brushes.Gray);
        var primary = this.Resource("TextFillColorPrimaryBrush", Brushes.White);
        var accentText = this.Resource("AccentTextFillColorPrimaryBrush", Brushes.DodgerBlue);

        var minuteBrush = this.RingBrush(_pressed && _draggingMinute, _minuteHover);
        var hourBrush = this.RingBrush(_pressed && !_draggingMinute, _hourHover);

        var minAng = Time.Minutes * 6.0;
        var hourAng = Is24Hour ? Time.Hours % 24 * 15.0 : (Time.Hours % 12 == 0 ? 12 : Time.Hours % 12) % 12 * 30.0;

        RadialDial.DrawRing(_parts, C, RMin, minAng, minuteBrush, track, RadialDial.TrackThickness);
        RadialDial.DrawRing(_parts, C, RHour, hourAng, hourBrush, track, RadialDial.TrackThickness);

        if (Is24Hour)
        {
            for (var hh = 0; hh < 24; hh += 2)
                AddNumber(hh.ToString(), RadialDial.OnCircle(C, RNum, hh * 15.0), hh == Time.Hours, accentText, primary);
        }
        else
        {
            var h12 = Time.Hours % 12 == 0 ? 12 : Time.Hours % 12;
            for (var i = 1; i <= 12; i++)
                AddNumber(i.ToString(), RadialDial.OnCircle(C, RNum, i % 12 * 30.0), i == h12, accentText, primary);
        }

        _hourThumb.Fill = hourBrush;
        _minuteThumb.Fill = minuteBrush;
        Position(_hourThumb, RadialDial.OnCircle(C, RHour, hourAng));
        Position(_minuteThumb, RadialDial.OnCircle(C, RMin, minAng));
    }

    private static void Position(Control thumb, Point p)
    {
        Canvas.SetLeft(thumb, p.X - RadialDial.ThumbDiameter / 2);
        Canvas.SetTop(thumb, p.Y - RadialDial.ThumbDiameter / 2);
    }

    private void AddNumber(string text, Point p, bool selected, IBrush accentText, IBrush primary)
    {
        const double box = 26;
        var b = new Border
        {
            Width = box,
            Height = box,
            Background = Brushes.Transparent,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 14,
                FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                Foreground = selected ? accentText : primary,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        Canvas.SetLeft(b, p.X - box / 2);
        Canvas.SetTop(b, p.Y - box / 2);
        _parts.Children.Add(b);
    }
}
