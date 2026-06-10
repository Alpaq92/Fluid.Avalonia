using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Fluid.Avalonia.Locale;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// A merged date + time picker: one segmented field (month / day / year · hour : minute · AM-PM) that
/// opens a flyout of looping spinner columns (the reusable <see cref="DateTimeSpinners"/>). The flyout
/// seeds the spinners from <see cref="SelectedDateTime"/> on open; OK commits the spinners' value back
/// into it, Cancel discards, Reset returns the time to a default. <see cref="ClockIdentifier"/> chooses
/// 12-hour (with an AM/PM column) or 24-hour.
/// </summary>
public partial class DateTimePicker : UserControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<DateTimePicker, DateTime?>(
            nameof(SelectedDateTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> ClockIdentifierProperty =
        AvaloniaProperty.Register<DateTimePicker, string>(nameof(ClockIdentifier), "12HourClock");

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

    private bool Is24 => ClockIdentifier == "24HourClock";

    public DateTimePicker()
    {
        InitializeComponent();

        OkBtn.Click += (_, _) => { SelectedDateTime = Spinners.SelectedDateTime; Trigger.Flyout?.Hide(); };
        CancelBtn.Click += (_, _) => Trigger.Flyout?.Hide();   // discard: spinners re-seed on next open
        ResetBtn.Click += (_, _) => Spinners.ResetTime();

        // Seed the spinner draft from the committed value each time the flyout opens.
        if (Trigger.Flyout is FlyoutBase fb)
            fb.Opened += (_, _) => Spinners.SelectedDateTime = SelectedDateTime;

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

    // Collapse/restore the field's AM/PM segment per clock mode (shared logic) and switch the spinners.
    private void ApplyClockMode()
    {
        SegmentedDateTimeField.ApplyClockMode(Is24, FieldGrid, AmPmText, AmPmDiv, Trigger);
        Spinners.ClockIdentifier = ClockIdentifier;
    }

    // Paint the segmented field from the committed value (placeholders when nothing is set yet).
    private void UpdateLabel() =>
        SegmentedDateTimeField.UpdateLabel(this, SelectedDateTime, Is24,
            MonthText, DayText, YearText, HourText, MinuteText, AmPmText);

    // Re-localize the placeholders when the language changes (the button captions use {DynamicResource}
    // and update themselves; the segmented field is painted in code, so it subscribes here).
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
