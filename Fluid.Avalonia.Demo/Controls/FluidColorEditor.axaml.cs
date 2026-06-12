using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// The Fluid colour-picking surface hosted by <see cref="FluidColorPicker"/>'s flyout (and any
/// other flyout that needs it): a spectrum + hue slider, the Open Color preset palette, and a
/// components slider view, mirrored onto the two-way <see cref="Color"/> property in code,
/// the same way <c>AccentsPage</c> keeps its pickers in sync.
/// </summary>
public partial class FluidColorEditor : UserControl
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<FluidColorEditor, Color>(
            nameof(Color),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The colour shown by every editor. Live — changes on every edit.</summary>
    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    private bool _syncing;       // guards programmatic editor writes from looping back as user edits
    private object? _editSource; // the editor that produced the current change (skipped when mirroring)

    public FluidColorEditor()
    {
        InitializeComponent();
        BuildPresets();

        // Start from the live accent (OnPropertyChanged mirrors it into every editor), then listen.
        Color = AccentService.CurrentAccent;

        Spectrum.ColorChanged += OnEditorColorChanged;
        HueSlider.ColorChanged += OnEditorColorChanged;
        Components.ColorChanged += OnEditorColorChanged;
        // Clicking an accent shade in the bottom previewer picks that shade.
        Preview.ColorChanged += OnEditorColorChanged;
    }

    // Every Color change — a user edit or a programmatic write (a host seeding the editor) — is
    // mirrored onto all editors except the one that produced it.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ColorProperty || Spectrum is null)
            return;

        var color = change.GetNewValue<Color>();
        _syncing = true;
        if (!ReferenceEquals(_editSource, Spectrum)) Spectrum.Color = color;
        if (!ReferenceEquals(_editSource, HueSlider)) HueSlider.Color = color;
        if (!ReferenceEquals(_editSource, Components)) Components.Color = color;
        if (!ReferenceEquals(_editSource, Preview)) Preview.HsvColor = color.ToHsv();
        _syncing = false;
    }

    private void OnEditorColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_syncing || e.NewColor == Color)
            return;

        _editSource = sender;
        Color = e.NewColor;
        _editSource = null;
    }

    // The same Open Color preset swatches the Accents page offers.
    private void BuildPresets()
    {
        foreach (var preset in AccentService.Preset)
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
            button.Click += (_, _) => Color = color;

            PresetPanel.Children.Add(button);
        }
    }
}
