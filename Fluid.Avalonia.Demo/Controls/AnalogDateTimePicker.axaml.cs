using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Fluid.Avalonia.Locale;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// A merged date + time picker whose dropdown pairs a <see cref="Calendar"/> (date) with a
/// <see cref="RadialClock"/> dial (time), side by side — an analog clock face rather than digital
/// spinner wheels. The flyout seeds both from <see cref="SelectedDateTime"/> on open; OK commits the
/// picked day + dial time back into it, Cancel discards, Reset returns the time to its default.
/// <see cref="ClockIdentifier"/> chooses 12-hour (with an AM/PM column + dial period) or 24-hour.
/// </summary>
public partial class AnalogDateTimePicker : UserControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<AnalogDateTimePicker, DateTime?>(
            nameof(SelectedDateTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> ClockIdentifierProperty =
        AvaloniaProperty.Register<AnalogDateTimePicker, string>(nameof(ClockIdentifier), "12HourClock");

    /// <summary>The combined, committed date + time (null until OK is pressed).</summary>
    public DateTime? SelectedDateTime
    {
        get => GetValue(SelectedDateTimeProperty);
        set => SetValue(SelectedDateTimeProperty, value);
    }

    /// <summary>"12HourClock" or "24HourClock".</summary>
    public string ClockIdentifier
    {
        get => GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    private static readonly TimeSpan DefaultTime = new(9, 0, 0);
    private bool Is24 => ClockIdentifier == "24HourClock";

    public AnalogDateTimePicker()
    {
        InitializeComponent();

        OkBtn.Click += (_, _) => { SelectedDateTime = Combine(); Trigger.Flyout?.Hide(); };
        CancelBtn.Click += (_, _) => Trigger.Flyout?.Hide();   // discard: re-seeds on next open
        ResetBtn.Click += (_, _) => Clock.Time = DefaultTime;  // reset time only, keep the chosen date

        // Re-seed the calendar + dial from the committed value each time the flyout opens.
        if (Trigger.Flyout is FlyoutBase fb)
            fb.Opened += (_, _) => SeedFlyout();

        ApplyClockMode();
        UpdateLabel();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs c)
    {
        base.OnPropertyChanged(c);
        if (FieldGrid is null)
            return;

        if (c.Property == SelectedDateTimeProperty)
        {
            UpdateLabel();
        }
        else if (c.Property == ClockIdentifierProperty)
        {
            ApplyClockMode();
            UpdateLabel();
        }
    }

    // Seed the calendar + dial from the committed value (or a sensible default).
    private void SeedFlyout()
    {
        var d = SelectedDateTime ?? DateTime.Today + DefaultTime;
        Cal.SelectedDate = d.Date;
        Cal.DisplayDate = d.Date;
        Clock.Time = d.TimeOfDay;
    }

    // Combine the calendar's day with the dial's time of day.
    private DateTime Combine()
    {
        var date = (Cal.SelectedDate ?? DateTime.Today).Date;
        return date + Clock.Time;
    }

    // Collapse/restore the field's AM/PM segment per clock mode (shared logic) and switch the dial.
    private void ApplyClockMode()
    {
        SegmentedDateTimeField.ApplyClockMode(Is24, FieldGrid, AmPmText, AmPmDiv, Trigger);
        Clock.Is24Hour = Is24;
    }

    // Paint the segmented field from the committed value (placeholders when nothing is set yet).
    private void UpdateLabel() =>
        SegmentedDateTimeField.UpdateLabel(this, SelectedDateTime, Is24,
            MonthText, DayText, YearText, HourText, MinuteText, AmPmText);

    // Re-localize the placeholders when the language changes (button captions use {DynamicResource}).
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LocaleManager.LanguageChanged += OnLanguageChanged;
        UpdateLabel();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LocaleManager.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged() => UpdateLabel();
}
