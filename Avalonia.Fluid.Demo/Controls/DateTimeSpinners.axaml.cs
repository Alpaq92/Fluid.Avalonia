using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// The looping spinner-column date + time picker, factored out of <see cref="DateTimePicker"/> so it
/// can be shown inline as well as hosted in the picker's flyout. Built on
/// <see cref="DateTimePickerPanel"/> (the primitive the native DatePicker / TimePicker use). It edits
/// <see cref="SelectedDateTime"/> live as you scroll; a host seeds it on open and reads it back on
/// commit. <see cref="ClockIdentifier"/> chooses 12-hour (with an AM/PM column) or 24-hour.
/// </summary>
public partial class DateTimeSpinners : UserControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateTimeProperty =
        AvaloniaProperty.Register<DateTimeSpinners, DateTime?>(
            nameof(SelectedDateTime), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> ClockIdentifierProperty =
        AvaloniaProperty.Register<DateTimeSpinners, string>(nameof(ClockIdentifier), "12HourClock");

    /// <summary>The live date + time (updated continuously as the columns scroll).</summary>
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
    private bool _updating;   // guards the SelectedDateTime <-> columns sync from re-entering

    public DateTimeSpinners()
    {
        InitializeComponent();

        // Avalonia's DateTimePickerPanel formats Day items as new DateTime(FormatDate.Year,
        // FormatDate.Month, value); FormatDate defaults to DateTime.Now, so when the day Maximum
        // reaches 31 in a < 31-day current month it throws "un-representable DateTime". Pin the date
        // columns' (internal) FormatDate to a 31-day month so every day 1..31 is always representable.
        var pin = new DateTime(2000, 1, 1);
        PinFormatDate(MonthPanel, pin);
        PinFormatDate(DayPanel, pin);
        PinFormatDate(YearPanel, pin);

        // Fixed column ranges (PanelType alone does not set these). Always set Max before Min so we
        // never transiently have Minimum > Maximum.
        MonthPanel.MaximumValue = 12;
        MonthPanel.MinimumValue = 1;
        DayPanel.MaximumValue = 31;
        DayPanel.MinimumValue = 1;
        YearPanel.MaximumValue = 2100;
        YearPanel.MinimumValue = 1900;
        MinutePanel.MaximumValue = 59;
        MinutePanel.MinimumValue = 0;

        MonthPanel.SelectionChanged += (_, _) => { ClampDay(); OnScroll(); };
        YearPanel.SelectionChanged += (_, _) => { ClampDay(); OnScroll(); };
        DayPanel.SelectionChanged += (_, _) => OnScroll();
        HourPanel.SelectionChanged += (_, _) => OnScroll();
        MinutePanel.SelectionChanged += (_, _) => OnScroll();
        PeriodPanel.SelectionChanged += (_, _) => OnScroll();

        ConfigureHourColumn();

        // Seed once the columns are laid out (so SelectedValue can scroll to the right item).
        Loaded += (_, _) => SeedScrollers();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs c)
    {
        base.OnPropertyChanged(c);
        if (MonthPanel is null)
            return;

        if (c.Property == SelectedDateTimeProperty)
        {
            if (!_updating)
                SeedScrollers();   // external seed (host opened its flyout, or a binding pushed a value)
        }
        else if (c.Property == ClockIdentifierProperty)
        {
            ConfigureHourColumn();
            SeedScrollers();
        }
    }

    /// <summary>Reset the time-of-day columns to the default (leaves the chosen date alone).</summary>
    public void ResetTime()
    {
        MinutePanel.SelectedValue = DefaultTime.Minutes;
        if (Is24)
        {
            HourPanel.SelectedValue = DefaultTime.Hours;
        }
        else
        {
            var h12 = DefaultTime.Hours % 12;
            HourPanel.SelectedValue = h12 == 0 ? 12 : h12;
            PeriodPanel.SelectedValue = DefaultTime.Hours >= 12 ? 1 : 0;
        }
    }

    // A column scrolled: publish the new value (guarded so it doesn't re-seed the columns).
    private void OnScroll()
    {
        if (_updating)
            return;
        _updating = true;
        SetCurrentValue(SelectedDateTimeProperty, ReadScrollers());
        _updating = false;
    }

    // 12-hour → 1..12 hour column + AM/PM column; 24-hour → 0..23 hour column, no period column.
    private void ConfigureHourColumn()
    {
        if (HourPanel is null)
            return;

        // Hour/Minute columns are formatted as a TimeSpan (not a DateTime), so the format string must
        // use TimeSpan specifiers ("hh"/"%h"/"mm") — "%H" is invalid for TimeSpan and throws. Set Max
        // before Min so we never transiently have Minimum > Maximum.
        if (Is24)
        {
            HourPanel.MaximumValue = 23;
            HourPanel.MinimumValue = 0;
            HourPanel.ItemFormat = "hh";   // 00..23
            PeriodHost.IsVisible = false;
        }
        else
        {
            HourPanel.MaximumValue = 12;
            HourPanel.MinimumValue = 1;
            HourPanel.ItemFormat = "%h";   // 1..12
            PeriodHost.IsVisible = true;
        }

        PeriodPanel.MaximumValue = 1;     // 0 = AM, 1 = PM
        PeriodPanel.MinimumValue = 0;
    }

    // Pin a DateTimePickerPanel's internal FormatDate so item formatting uses a fixed, always-valid
    // reference month (no public API exists for this). Best-effort: a no-op if the field shape changes.
    private static void PinFormatDate(DateTimePickerPanel panel, DateTime reference)
    {
        var member = typeof(DateTimePickerPanel).GetProperty(
            "FormatDate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (member is { CanWrite: true })
            member.SetValue(panel, reference);
    }

    private void ClampDay()
    {
        if (DayPanel is null)
            return;

        var days = DateTime.DaysInMonth(YearPanel.SelectedValue, MonthPanel.SelectedValue);
        DayPanel.MaximumValue = days;
        if (DayPanel.SelectedValue > days)
            DayPanel.SelectedValue = days;
    }

    // Scroll the columns to the current value (or a sensible default), then publish that value so
    // SelectedDateTime is never left stale/null after seeding.
    private void SeedScrollers()
    {
        _updating = true;

        var d = SelectedDateTime ?? DateTime.Today + DefaultTime;
        YearPanel.SelectedValue = d.Year;
        MonthPanel.SelectedValue = d.Month;
        ClampDay();
        DayPanel.SelectedValue = d.Day;
        MinutePanel.SelectedValue = d.Minute;

        if (Is24)
        {
            HourPanel.SelectedValue = d.Hour;
        }
        else
        {
            var h12 = d.Hour % 12;
            HourPanel.SelectedValue = h12 == 0 ? 12 : h12;
            PeriodPanel.SelectedValue = d.Hour >= 12 ? 1 : 0;
        }

        SetCurrentValue(SelectedDateTimeProperty, ReadScrollers());
        _updating = false;
    }

    // Read a DateTime back out of the spinner columns.
    private DateTime ReadScrollers()
    {
        var year = YearPanel.SelectedValue;
        var month = MonthPanel.SelectedValue;
        var day = Math.Min(DayPanel.SelectedValue, DateTime.DaysInMonth(year, month));
        var minute = MinutePanel.SelectedValue;

        int hour;
        if (Is24)
        {
            hour = HourPanel.SelectedValue;
        }
        else
        {
            hour = HourPanel.SelectedValue % 12;       // 12 → 0
            if (PeriodPanel.SelectedValue == 1)        // PM
                hour += 12;
        }

        return new DateTime(year, month, day, hour, minute, 0);
    }
}
