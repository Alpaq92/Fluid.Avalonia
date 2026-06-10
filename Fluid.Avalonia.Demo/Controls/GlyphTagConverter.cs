using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>Returns a <see cref="ComboBoxItem"/>'s <c>Tag</c> (a Codicon character string) — used by the
/// demo's VisualRate glyph picker to feed the selected item's glyph to <c>VisualRate.Glyph</c>.</summary>
public sealed class GlyphTagConverter : IValueConverter
{
    public static readonly GlyphTagConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value as ComboBoxItem)?.Tag as string ?? "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
