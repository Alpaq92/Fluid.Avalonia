using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Fluid.Locale;

namespace Avalonia.Fluid.Demo.Controls;

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

    // 24-hour hides the field's AM/PM segment (collapsing its divider + column so the remaining five
    // segments split the field evenly) and switches the spinners; 12-hour restores them.
    private void ApplyClockMode()
    {
        AmPmText.IsVisible = !Is24;
        AmPmDiv.IsVisible = !Is24;
        FieldGrid.ColumnDefinitions[9].Width = Is24 ? new GridLength(0) : GridLength.Auto;
        // AM/PM only ever holds a 2-letter designator, so size that column to its content (Auto) rather
        // than giving it a full equal star share — otherwise it steals width and squishes the date segments.
        FieldGrid.ColumnDefinitions[10].Width = Is24 ? new GridLength(0) : GridLength.Auto;
        // 12-hour packs six segments, so widen the field enough that its five date/time segments get the
        // same breathing room as the five-segment 24-hour layout (the AM/PM column is extra on top).
        Trigger.MinWidth = Is24 ? 320 : 360;
        Spinners.ClockIdentifier = ClockIdentifier;
    }

    // Paint the segmented field from the committed value (placeholders when nothing is set yet).
    private void UpdateLabel()
    {
        if (SelectedDateTime is { } dt)
        {
            SetSeg(MonthText, dt.ToString("MMM"), false);
            SetSeg(DayText, dt.ToString("dd"), false);
            SetSeg(YearText, dt.ToString("yyyy"), false);

            var hour = Is24 ? dt.Hour : (dt.Hour % 12 == 0 ? 12 : dt.Hour % 12);
            SetSeg(HourText, hour.ToString(Is24 ? "00" : "0"), false);
            SetSeg(MinuteText, dt.ToString("mm"), false);
            if (!Is24)
                SetSeg(AmPmText, dt.ToString("tt"), false);
        }
        else
        {
            SetSeg(MonthText, L("STRING_MONTH"), true);
            SetSeg(DayText, L("STRING_DAY"), true);
            SetSeg(YearText, L("STRING_YEAR"), true);
            SetSeg(HourText, L("STRING_HOUR"), true);
            SetSeg(MinuteText, L("STRING_MINUTE"), true);
            if (!Is24)
                SetSeg(AmPmText, "AM", true);
        }
    }

    // Resolve a localized string from the merged Locale dictionary (falls back to the key).
    private string L(string key) => this.TryFindResource(key, out var v) && v is string s ? s : key;

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

    private static void SetSeg(TextBlock tb, string text, bool placeholder)
    {
        tb.Text = text;
        tb.Classes.Set("placeholder", placeholder);
    }
}
