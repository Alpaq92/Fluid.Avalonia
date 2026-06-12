using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Fluid.Avalonia.Demo.Controls;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class BasicInputPage : UserControl
{
    private static readonly string[] SpinnerLevels = { "Low", "Medium", "High" };

    // Ink presets for the SignaturePad demo — classic signature inks resolved from the shared
    // AccentService.Preset palette, so the same swatch name always means the same colour
    // app-wide (matching the Accents page and FluidColorEditor swatches).
    private static readonly string[] SignatureInkNames = { "Blue", "Violet", "Dark Gray", "Deep Red", "Deep Green" };

    private static readonly (string Name, Color Color)[] SignatureInks =
        SignatureInkNames
            .Select(n => AccentService.Preset.First(p => p.Name == n))
            .Select(p => (p.Name, p.Color))
            .ToArray();

    private readonly List<Border> _signatureRings = new();

    public BasicInputPage()
    {
        InitializeComponent();
        LevelSpinner.Spin += OnSpinnerSpin;

        BuildSignatureInkSwatches();
        BuildSignatureStrokeLabels();
        // The slider's 0..6 value is the max nib width; the min scales with it so the velocity
        // taper is preserved at every size. Apply it now and on every change.
        SignatureStrokeSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == RangeBase.ValueProperty)
                ApplySignatureStroke(SignatureStrokeSlider.Value);
        };
        ApplySignatureStroke(SignatureStrokeSlider.Value);
        // The clear (×) button only shows once something is drawn.
        SigClearButton.Click += (_, _) => SigPad.Clear();
        SigPad.PropertyChanged += (_, e) =>
        {
            if (e.Property == SignaturePad.IsEmptyProperty)
                SigClearButton.IsVisible = !SigPad.IsEmpty;
        };

        // The standard-Window example needs a desktop head; the browser / single-view head has no
        // Window, so disable the button there (the handler is guarded too).
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
        {
            OpenWindowButton.Click += OnOpenStandardWindow;
        }
        else
        {
            OpenWindowButton.IsEnabled = false;
            ToolTip.SetTip(OpenWindowButton, "Desktop only — the browser head has no windows.");
        }
    }

    // The slider's 0 / 6 end labels: vector outlines of the digits (see the XAML comment — text
    // hinting snaps glyphs to whole pixels, so TextBlocks can't be placed with the sub-pixel
    // precision needed to sit their ink on the track line). Same typeface and size the TextBlocks
    // used (the bundled DejaVu Sans at 14), bbox-normalized so the Paths center their ink.
    private void BuildSignatureStrokeLabels()
    {
        SigMinLabel.Data = DigitGeometry("0");
        SigMaxLabel.Data = DigitGeometry("6");
    }

    private static Geometry DigitGeometry(string digit)
    {
        // Resolve the font on the Application directly: this runs in the page constructor, while
        // the page is still detached, and a detached control's FindResource never reaches
        // Application.Current (it returns UnsetValue, which crashed the page on open).
        var typeface = Application.Current is { } app
            && app.TryGetResource("ContentControlThemeFontFamily", null, out var value)
            && value is FontFamily family
                ? new Typeface(family)
                : Typeface.Default;
        var text = new FormattedText(digit, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, typeface, 14, Brushes.Black);
        var geometry = text.BuildGeometry(default)!;
        var bounds = geometry.Bounds;
        geometry.Transform = new TranslateTransform(-bounds.X, -bounds.Y);
        return geometry;
    }

    // Sets the pad's nib range from the 0..6 slider: the value is the max width, the min is a
    // fraction of it so fast strokes still taper thinner (kept above 0 so a dot is always visible).
    private void ApplySignatureStroke(double value)
    {
        // Min is a fraction of the value but floored so a tap always leaves a visible dot; Max is then
        // clamped to never fall below Min (at slider 0 that keeps a thin, valid hairline rather than an
        // inverted Max < Min that would flatten the velocity taper).
        var min = Math.Max(0.5, value * 0.4);
        SigPad.MinStrokeWidth = min;
        SigPad.MaxStrokeWidth = Math.Max(value, min);
    }

    // Builds the ink-colour swatches (a coloured rounded-square inside a ring that lights up with
    // the accent when selected), matching the Accents page preset swatches, plus a trailing "+"
    // chip whose flyout hosts the shared FluidColorEditor — Add appends the picked colour as a
    // new selectable swatch.
    private void BuildSignatureInkSwatches()
    {
        foreach (var (name, color) in SignatureInks)
            SignatureColorPanel.Children.Add(CreateInkSwatch(name, color));

        SignatureColorPanel.Children.Add(CreateCustomInkButton());

        // Default to the first preset (Open Color Blue) — set both the pad ink and the lit ring from
        // the same palette entry, so there's one source of truth for the starting colour.
        SigPad.StrokeColor = SignatureInks[0].Color;
        SelectSignatureInk(_signatureRings[0]);
    }

    // The shared swatch chrome both the ink dots and the "+" chip wear: a 40x40 rounded-square ring
    // (its 2px border + 1px padding give the same 3px inset as the Accents-page swatches, so the
    // inner CornerRadius-10 square lines up) inside a transparent hand-cursor button. The ring's
    // BorderBrush is the selection indicator (lit with the accent in SelectSignatureInk).
    private static (Button Button, Border Ring) CreateSwatchChrome(Control inner, string tooltip)
    {
        var ring = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(1),
            BorderBrush = Brushes.Transparent,
            Background = Brushes.Transparent,
            Child = inner,
        };
        var button = new Button
        {
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 8, 8),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(12),
            Cursor = new Cursor(StandardCursorType.Hand),
            Content = ring,
        };
        ToolTip.SetTip(button, tooltip);
        return (button, ring);
    }

    private Button CreateInkSwatch(string name, Color color)
    {
        // The colour dot: the house rounded-square swatch with the same subtle stroke the
        // Accents page / FluidColorEditor preset swatches carry.
        var dot = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Background = new ImmutableSolidColorBrush(color),
            [!Border.BorderBrushProperty] = new DynamicResourceExtension("ControlStrongStrokeColorDefaultBrush"),
        };
        var (button, ring) = CreateSwatchChrome(dot, name);
        ring.Tag = color;   // lets AddCustomInk find an existing swatch of the same colour

        button.Click += (_, _) =>
        {
            SigPad.StrokeColor = color;
            SelectSignatureInk(ring);
        };

        _signatureRings.Add(ring);
        return button;
    }

    // The "+" chip: an outlined rounded-square swatch (same geometry as the ink swatches) whose
    // flyout hosts the shared FluidColorEditor surface behind a Cancel / Add footer — the ink is
    // only added on an explicit Add, never live.
    private Button CreateCustomInkButton()
    {
        var glyph = new TextBlock
        {
            Text = "",   // Codicon "add"
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBlock.FontFamilyProperty] = new DynamicResourceExtension("SymbolThemeFontFamily"),
            [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFillColorSecondaryBrush"),
        };
        var chip = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Background = Brushes.Transparent,
            Child = glyph,
            [!Border.BorderBrushProperty] = new DynamicResourceExtension("ControlStrongStrokeColorDefaultBrush"),
        };
        // Same swatch chrome as the ink dots so the chip aligns exactly (its ring stays unselected).
        var (button, _) = CreateSwatchChrome(chip, "Add a custom colour");

        // The same editor surface FluidColorPicker's dropdown hosts, plus the standard
        // Cancel / accent-commit footer the DateTimePicker flyout uses.
        var editor = new FluidColorEditor();
        var cancel = new Button();
        cancel[!ContentControl.ContentProperty] = new DynamicResourceExtension("STRING_CANCEL");
        var add = new Button();
        add.Classes.Add("accent");
        add[!ContentControl.ContentProperty] = new DynamicResourceExtension("STRING_ADD");
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(12, 0, 12, 4),   // aligns with the editor's previewer margins
            Children = { cancel, add },
        };
        var flyout = new Flyout
        {
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            Content = new StackPanel { Children = { editor, footer } },
        };
        button.Flyout = flyout;

        // Seed the editor with the current ink each time the flyout opens (Opened, not Click —
        // Click also fires on the toggling close).
        flyout.Opened += (_, _) => editor.Color = SigPad.StrokeColor;
        cancel.Click += (_, _) => flyout.Hide();
        add.Click += (_, _) =>
        {
            AddCustomInk(editor.Color);
            flyout.Hide();
        };
        return button;
    }

    private void AddCustomInk(Color color)
    {
        // The editor is RGB-only (alpha is disabled across the demo); normalize defensively so
        // two inks can never differ only by an invisible alpha.
        color = Color.FromRgb(color.R, color.G, color.B);

        // Same colour already in the row (preset or custom)? Just select it.
        var existing = _signatureRings.FirstOrDefault(r => r.Tag is Color c && c == color);
        if (existing is not null)
        {
            SigPad.StrokeColor = color;
            SelectSignatureInk(existing);
            return;
        }

        // Custom swatches get the canonical palette name when the colour is a preset, hex otherwise.
        var preset = AccentService.Preset.FirstOrDefault(p => p.Color == color);
        var name = preset.Name ?? $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        // Insert before the trailing "+" button so the adder stays last.
        var button = CreateInkSwatch(name, color);
        SignatureColorPanel.Children.Insert(SignatureColorPanel.Children.Count - 1, button);
        SigPad.StrokeColor = color;
        SelectSignatureInk(_signatureRings[^1]);
    }

    // Lights the selected swatch's ring with the accent — the demo's selection token (the same
    // brush the NavItem indicator pill uses) — and clears every other ring.
    private void SelectSignatureInk(Border selected)
    {
        foreach (var ring in _signatureRings)
            ring.BorderBrush = Brushes.Transparent;
        selected[!Border.BorderBrushProperty] = new DynamicResourceExtension("AccentFillColorDefaultBrush");
    }

    // A bare ButtonSpinner only raises Spin — it doesn't change its own content — so the demo
    // handles it, cycling the label through Low / Medium / High as the user spins up or down.
    private void OnSpinnerSpin(object? sender, SpinEventArgs e)
    {
        if (sender is not ButtonSpinner spinner)
            return;

        var index = Array.IndexOf(SpinnerLevels, spinner.Content as string);
        if (index < 0)
            index = 0;

        index += e.Direction == SpinDirection.Increase ? 1 : -1;
        index = Math.Clamp(index, 0, SpinnerLevels.Length - 1);
        spinner.Content = SpinnerLevels[index];
    }

    // Opens a plain Avalonia Window (OS-drawn title bar) so it can be compared with the demo's
    // custom Mica-titlebar chrome. Desktop-only — guarded for the browser head, which has no Window.
    private void OnOpenStandardWindow(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        using var iconStream = global::Avalonia.Platform.AssetLoader.Open(
            new Uri("avares://Fluid.Avalonia.Demo/Assets/logo.ico"));

        var window = new Window
        {
            Title = "Example",
            Icon = new WindowIcon(iconStream),
            Width = 380,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        // Align the body with the OS title bar (the theme's solid base surface). The dialog is modal,
        // so the theme variant can't change while it's open — resolving the current variant is fine.
        if (this.TryFindResource("SolidBackgroundFillColorBaseBrush", ActualThemeVariant, out var bg)
            && bg is IBrush baseBrush)
        {
            window.Background = baseBrush;
        }

        var acknowledge = new Button
        {
            Content = "Acknowledged",
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        acknowledge.Click += (_, _) => window.Close();

        window.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "This is a standard Avalonia Window. The Fluid.Avalonia demo itself runs "
                         + "in Fluid's custom Mica-titlebar chrome (the same look FluidWindow "
                         + "packages), while this window keeps the platform's default title bar. "
                         + "Same theme, different chrome.",
                },
                acknowledge,
            },
        };

        // Modal: ShowDialog disables the owner (the demo window) until this one is closed.
        _ = window.ShowDialog(owner);
    }
}
