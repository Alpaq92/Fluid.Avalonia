using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Fluid;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// A Fluent color dropdown: a labelled <see cref="DropDownButton"/> (swatch + hex) whose flyout
/// hosts the same three editors used on the Accents page — a spectrum + hue slider, a preset
/// palette, and a components slider view. Colour state is mirrored across all editors and the
/// button in code, the same way <c>AccentsPage</c> keeps its pickers in sync.
/// </summary>
public partial class FluentColorPicker : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<FluentColorPicker, Color>(
            nameof(SelectedColor),
            defaultValue: Color.FromRgb(0x21, 0x96, 0xF3),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The currently selected colour.</summary>
    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private bool _syncing;   // guards programmatic editor writes from looping back as user edits

    public FluentColorPicker()
    {
        InitializeComponent();
        BuildPresets();

        // Start from the live accent, push it into every editor + the button, then listen.
        SelectedColor = AccentColorService.CurrentAccent;
        PushToEditors(SelectedColor);
        UpdateButton(SelectedColor);

        Spectrum.ColorChanged += OnEditorColorChanged;
        HueSlider.ColorChanged += OnEditorColorChanged;
        Components.ColorChanged += OnEditorColorChanged;
        // Clicking an accent shade in the bottom previewer picks that shade.
        Preview.ColorChanged += OnEditorColorChanged;
    }

    // A user edit on any editor becomes the selected colour and is mirrored onto the others.
    private void OnEditorColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_syncing || e.NewColor == SelectedColor)
            return;

        SetColor(e.NewColor, sender);
    }

    private void SetColor(Color color, object? source)
    {
        SelectedColor = color;
        UpdateButton(color);

        _syncing = true;
        if (!ReferenceEquals(source, Spectrum)) Spectrum.Color = color;
        if (!ReferenceEquals(source, HueSlider)) HueSlider.Color = color;
        if (!ReferenceEquals(source, Components)) Components.Color = color;
        if (!ReferenceEquals(source, Preview)) Preview.HsvColor = color.ToHsv();
        _syncing = false;
    }

    private void PushToEditors(Color color)
    {
        _syncing = true;
        Spectrum.Color = color;
        HueSlider.Color = color;
        Components.Color = color;
        Preview.HsvColor = color.ToHsv();
        _syncing = false;
    }

    private void UpdateButton(Color c)
    {
        Swatch.Background = new SolidColorBrush(c);
        HexText.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    // The same Material-inspired preset swatches the Accents page offers.
    private void BuildPresets()
    {
        foreach (var preset in AccentColorService.Presets)
        {
            var color = preset.Color;
            var swatch = new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                // Subtle but visible stroke (theme-aware), not a hard white line.
                [!Border.BorderBrushProperty] = new DynamicResourceExtension("ControlStrongStrokeColorDefaultBrush"),
            };

            var button = new Button
            {
                Height = 40,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Content = swatch,
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            ToolTip.SetTip(button, preset.Name);
            button.Click += (_, _) => SetColor(color, null);

            PresetPanel.Children.Add(button);
        }
    }
}
