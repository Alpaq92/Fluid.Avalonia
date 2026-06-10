using Avalonia.Controls;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// Shared logic for the segmented date + time field that <see cref="DateTimePicker"/> and
/// <see cref="AnalogDateTimePicker"/> both render: painting the six segments from the committed
/// value (localized placeholders when unset) and collapsing the AM/PM segment in 24-hour mode.
/// </summary>
internal static class SegmentedDateTimeField
{
    /// <summary>Paint the segmented field from the committed value (placeholders when nothing is
    /// set yet). <paramref name="host"/> resolves the localized placeholder strings from the merged
    /// Locale dictionary (falling back to the key).</summary>
    public static void UpdateLabel(
        Control host, DateTime? value, bool is24,
        TextBlock month, TextBlock day, TextBlock year,
        TextBlock hour, TextBlock minute, TextBlock amPm)
    {
        if (value is { } dt)
        {
            SetSeg(month, dt.ToString("MMM"), false);
            SetSeg(day, dt.ToString("dd"), false);
            SetSeg(year, dt.ToString("yyyy"), false);

            var h = is24 ? dt.Hour : (dt.Hour % 12 == 0 ? 12 : dt.Hour % 12);
            SetSeg(hour, h.ToString(is24 ? "00" : "0"), false);
            SetSeg(minute, dt.ToString("mm"), false);
            if (!is24)
                SetSeg(amPm, dt.ToString("tt"), false);
        }
        else
        {
            SetSeg(month, L(host, "STRING_MONTH"), true);
            SetSeg(day, L(host, "STRING_DAY"), true);
            SetSeg(year, L(host, "STRING_YEAR"), true);
            SetSeg(hour, L(host, "STRING_HOUR"), true);
            SetSeg(minute, L(host, "STRING_MINUTE"), true);
            if (!is24)
                SetSeg(amPm, "AM", true);
        }
    }

    /// <summary>24-hour hides the field's AM/PM segment — collapsing its divider + columns so the
    /// remaining five segments split the field evenly; 12-hour restores them. The AM/PM column only
    /// ever holds a 2-letter designator, so it sizes to content (Auto) rather than a full star share,
    /// which would steal width and squish the date segments. 12-hour packs six segments, so the
    /// trigger widens enough that its five date/time segments get the same breathing room as the
    /// five-segment 24-hour layout (the AM/PM column is extra on top).</summary>
    public static void ApplyClockMode(
        bool is24, Grid fieldGrid, TextBlock amPm, Control amPmDivider, Control trigger)
    {
        amPm.IsVisible = !is24;
        amPmDivider.IsVisible = !is24;
        fieldGrid.ColumnDefinitions[9].Width = is24 ? new GridLength(0) : GridLength.Auto;
        fieldGrid.ColumnDefinitions[10].Width = is24 ? new GridLength(0) : GridLength.Auto;
        trigger.MinWidth = is24 ? 320 : 360;
    }

    private static string L(Control host, string key)
        => host.TryFindResource(key, out var v) && v is string s ? s : key;

    private static void SetSeg(TextBlock tb, string text, bool placeholder)
    {
        tb.Text = text;
        tb.Classes.Set("placeholder", placeholder);
    }
}
