using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// A text input with a leading label "chip", modelled on the ColorView's component inputs:
/// a recessed bordered box split into a small label segment and a borderless editable field.
/// </summary>
public class LabeledTextInput : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<LabeledTextInput, string?>(nameof(Label));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<LabeledTextInput, string?>(
            nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<LabeledTextInput, string?>(nameof(Watermark));

    /// <summary>The short label shown in the leading chip (e.g. "R", "Name", "@").</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>The editable text value.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Placeholder text shown when the field is empty.</summary>
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
}
