using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// A TimePicker-style segmented field (hour : minute · AM/PM) that opens a <see cref="RadialClock"/>
/// dial in a flyout. The flyout seeds the dial from <see cref="Time"/> on open; OK commits the dial's
/// value back to <see cref="Time"/>, Cancel discards. <see cref="Is24Hour"/> switches both the dial and
/// the field to the 24-hour layout (no AM/PM).
/// </summary>
public partial class RadialTimePicker : UserControl
{
    public static readonly StyledProperty<TimeSpan> TimeProperty =
        AvaloniaProperty.Register<RadialTimePicker, TimeSpan>(
            nameof(Time), defaultValue: new TimeSpan(9, 0, 0), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The committed time of day.</summary>
    public TimeSpan Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<RadialTimePicker, bool>(nameof(Is24Hour));

    /// <summary>When true the field + dial use the 24-hour clock (0–23, no AM/PM).</summary>
    public bool Is24Hour { get => GetValue(Is24HourProperty); set => SetValue(Is24HourProperty, value); }

    public RadialTimePicker()
    {
        InitializeComponent();

        OkBtn.Click += (_, _) => { Time = Clock.Time; Field.Flyout?.Hide(); };
        CancelBtn.Click += (_, _) => Field.Flyout?.Hide();   // discard: dial re-seeds on next open

        if (Field.Flyout is FlyoutBase fb)
            fb.Opened += (_, _) => Clock.Time = Time;   // seed the dial from the committed value each open

        ApplyMode();
        UpdateField();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs c)
    {
        base.OnPropertyChanged(c);
        if (c.Property == TimeProperty)
            UpdateField();
        else if (c.Property == Is24HourProperty)
        {
            ApplyMode();
            UpdateField();
        }
    }

    // Paint the segmented field from the committed value.
    private void UpdateField()
    {
        var t = Time;
        if (Is24Hour)
            HourSeg.Text = t.Hours.ToString("00");
        else
        {
            var h12 = t.Hours % 12 == 0 ? 12 : t.Hours % 12;
            HourSeg.Text = h12.ToString();
            AmPmSeg.Text = t.Hours >= 12 ? "PM" : "AM";
        }
        MinuteSeg.Text = t.Minutes.ToString("00");
    }

    // 24-hour hides the field's AM/PM segment (collapsing its divider + column so hour | minute split
    // the field evenly) and switches the dial; 12-hour restores them.
    private void ApplyMode()
    {
        var twelve = !Is24Hour;
        AmPmDiv.IsVisible = twelve;
        AmPmSeg.IsVisible = twelve;
        FieldGrid.ColumnDefinitions[3].Width = twelve ? GridLength.Auto : new GridLength(0);
        FieldGrid.ColumnDefinitions[4].Width = twelve ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Clock.Is24Hour = Is24Hour;
    }
}
