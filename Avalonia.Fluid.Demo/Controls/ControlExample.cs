using Avalonia;
using Avalonia.Controls;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// The WinUI 3 Gallery "ControlExample" card: an optional header, a live example region,
/// an optional options pane on the right, and a collapsible "Source code" (XAML) footer.
/// Visibility of the header / options pane / footer is driven in the template via converters.
/// </summary>
public class ControlExample : ContentControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<ControlExample, string?>(nameof(Header));

    public static readonly StyledProperty<object?> OptionsProperty =
        AvaloniaProperty.Register<ControlExample, object?>(nameof(Options));

    public static readonly StyledProperty<string?> XamlSourceProperty =
        AvaloniaProperty.Register<ControlExample, string?>(nameof(XamlSource));

    /// <summary>Short description shown above the card.</summary>
    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Content of the options pane on the right. When null the pane is hidden.</summary>
    public object? Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    /// <summary>XAML shown in the collapsible source-code footer. When empty the footer is hidden.</summary>
    public string? XamlSource
    {
        get => GetValue(XamlSourceProperty);
        set => SetValue(XamlSourceProperty, value);
    }
}
