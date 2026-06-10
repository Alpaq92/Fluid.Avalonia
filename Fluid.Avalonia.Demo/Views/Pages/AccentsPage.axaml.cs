using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class AccentsPage : UserControl
{
    private enum Mode { System, Preset, Custom }

    private bool _updating;   // guards the mutually-exclusive mode checkboxes
    private bool _syncing;    // guards programmatic Picker.Color writes
    private Mode _mode = Mode.System;
    private Color _selectedPreset;

    public AccentsPage()
    {
        InitializeComponent();

        _selectedPreset = AccentService.Preset[0].Color;
        BuildPresetSwatches();

        SysAccentBtn.Click += (_, _) => SetMode(Mode.System, apply: true);

        ModeSystem.IsCheckedChanged += (_, _) => OnModeBox(ModeSystem, Mode.System);
        ModePreset.IsCheckedChanged += (_, _) => OnModeBox(ModePreset, Mode.Preset);
        ModeCustom.IsCheckedChanged += (_, _) => OnModeBox(ModeCustom, Mode.Custom);

        // Reflect the current (system) accent without re-applying it. We only start listening to
        // the ColorViews *after* this initial sync, so their own initialization ColorChanged
        // events don't get mistaken for a user edit (which would hijack the accent).
        SetMode(Mode.System, apply: false);
        Dispatcher.UIThread.Post(() =>
        {
            PushColorToEditors(AccentService.CurrentAccent);
            UpdateReadout();
            Spectrum.ColorChanged += OnPickerColorChanged;
            HueSlider.ColorChanged += OnPickerColorChanged;
            ComponentsPicker.ColorChanged += OnPickerColorChanged;
        }, DispatcherPriority.Background);
    }

    // Subscribe while attached so the readout follows a LIVE OS accent change (the swatch + hex are
    // snapshots of CurrentAccent, not DynamicResource-bound, so they need re-running on republish).
    // Unsubscribe on detach — AccentChanged is a static event and would otherwise root this page.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AccentService.AccentChanged += OnAccentChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        AccentService.AccentChanged -= OnAccentChanged;
    }

    // The accent was (re)published — e.g. the OS accent changed while in System mode. Refresh the
    // readout so the preview tile + hex track the live accent (in System mode also re-sync the editors).
    private void OnAccentChanged(object? sender, EventArgs e)
    {
        if (_mode == Mode.System)
            SyncFromCurrent();
        else
            UpdateReadout();
    }

    private void OnPickerColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (_syncing || e.NewColor == AccentService.CurrentAccent)
            return;

        // A user edit on any editor switches to Custom and applies the color.
        _mode = Mode.Custom;
        _updating = true;
        ModeSystem.IsChecked = false;
        ModePreset.IsChecked = false;
        ModeCustom.IsChecked = true;
        _updating = false;

        AccentService.SetAccent(e.NewColor);

        // Mirror onto the other editors so the spectrum, hue slider and sliders tab stay in sync.
        _syncing = true;
        if (!ReferenceEquals(sender, Spectrum)) Spectrum.Color = e.NewColor;
        if (!ReferenceEquals(sender, HueSlider)) HueSlider.Color = e.NewColor;
        if (!ReferenceEquals(sender, ComponentsPicker)) ComponentsPicker.Color = e.NewColor;
        _syncing = false;

        UpdateReadout();
    }

    private void PushColorToEditors(Color c)
    {
        _syncing = true;
        Spectrum.Color = c;
        HueSlider.Color = c;
        ComponentsPicker.Color = c;
        _syncing = false;
    }

    private void BuildPresetSwatches()
    {
        foreach (var preset in AccentService.Preset)
        {
            var swatch = new Border
            {
                Background = new SolidColorBrush(preset.Color),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                // Subtle but visible stroke, matching the FluidColorPicker preset swatches.
                [!Border.BorderBrushProperty] = new DynamicResourceExtension("ControlStrongStrokeColorDefaultBrush"),
            };

            var button = new Button
            {
                Width = 40,
                Height = 40,
                Padding = new Thickness(3),
                Margin = new Thickness(0, 0, 8, 8),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                // Round the focus/selection ring to match the rounded swatch.
                CornerRadius = new CornerRadius(12),
                Content = swatch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            ToolTip.SetTip(button, preset.Name);

            var color = preset.Color;
            button.Click += (_, _) =>
            {
                _selectedPreset = color;
                SetMode(Mode.Preset, apply: true);
            };

            PresetPanel.Children.Add(button);
        }
    }

    // A user clicked a mode checkbox. Apply it; refuse to leave nothing selected.
    private void OnModeBox(CheckBox box, Mode mode)
    {
        if (_updating)
            return;

        if (box.IsChecked == true)
        {
            SetMode(mode, apply: true);
        }
        else
        {
            // Can't deselect the active source — re-check it.
            _updating = true;
            box.IsChecked = true;
            _updating = false;
        }
    }

    private void SetMode(Mode mode, bool apply)
    {
        _mode = mode;

        _updating = true;
        ModeSystem.IsChecked = mode == Mode.System;
        ModePreset.IsChecked = mode == Mode.Preset;
        ModeCustom.IsChecked = mode == Mode.Custom;
        _updating = false;

        if (!apply)
            return;

        switch (mode)
        {
            case Mode.System:
                AccentService.UseSystemAccent();
                SyncFromCurrent();
                break;
            case Mode.Preset:
                AccentService.SetAccent(_selectedPreset);
                SyncFromCurrent();
                break;
            case Mode.Custom:
                AccentService.SetAccent(Spectrum.Color);
                UpdateReadout();
                break;
        }
    }

    // Push the current accent into both pickers (without re-applying) and refresh the readout.
    private void SyncFromCurrent()
    {
        Dispatcher.UIThread.Post(() =>
        {
            PushColorToEditors(AccentService.CurrentAccent);
            UpdateReadout();
        }, DispatcherPriority.Background);
    }

    private void UpdateReadout()
    {
        var c = AccentService.CurrentAccent;
        CurrentSwatch.Background = new SolidColorBrush(c);
        CurrentHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
