using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// A Fluid color dropdown: a labelled <see cref="DropDownButton"/> (swatch + hex) whose flyout
/// hosts the shared <see cref="FluidColorEditor"/> surface — the same editors the Accents page
/// uses. Selection is live through the two-way <see cref="SelectedColor"/>.
/// </summary>
public partial class FluidColorPicker : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<FluidColorPicker, Color>(
            nameof(SelectedColor),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The currently selected colour.</summary>
    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    private bool _syncing;   // guards the two-way SelectedColor <-> Editor.Color mirror

    public FluidColorPicker()
    {
        InitializeComponent();

        // The editor seeds itself from the live accent; adopt that as the initial selection.
        SelectedColor = Editor.Color;
        UpdateButton(Editor.Color);   // explicit: OnPropertyChanged won't fire if the value matched

        Editor.PropertyChanged += (_, e) =>
        {
            if (e.Property != FluidColorEditor.ColorProperty || _syncing)
                return;
            _syncing = true;
            SelectedColor = Editor.Color;
            _syncing = false;
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != SelectedColorProperty)
            return;

        var color = change.GetNewValue<Color>();
        UpdateButton(color);
        if (_syncing || Editor is null)
            return;
        _syncing = true;
        Editor.Color = color;
        _syncing = false;
    }

    private void UpdateButton(Color c)
    {
        Swatch.Background = new SolidColorBrush(c);
        HexText.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
