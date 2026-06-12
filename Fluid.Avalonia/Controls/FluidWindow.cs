using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Fluid.Avalonia.Interop;

namespace Fluid.Avalonia;

/// <summary>
/// A <see cref="Window"/> with Fluid.Avalonia's WinUI 3-style chrome: an extended client area with a
/// custom title bar (app icon, title, a free content slot, and minimize / maximize / close caption
/// buttons), a translucent backdrop with a solid fallback, and a frame that follows the app's light/dark
/// theme. The backdrop is cross-platform via <see cref="TransparencyService"/> (Mica on Windows,
/// vibrancy on macOS, a KWin blur on KDE/Linux) and is gated by <see cref="TransparencyEnabled"/>, which
/// is seeded from the Windows "Transparency effects" setting at construction. Windows-specific bits are guarded
/// and degrade gracefully on macOS / Linux. Put your window content in <see cref="ContentControl.Content"/>
/// as usual; drop title-bar widgets (search, menus…) into <see cref="TitleBarContent"/>.
/// </summary>
public class FluidWindow : Window
{
    public static readonly StyledProperty<object?> TitleBarContentProperty =
        AvaloniaProperty.Register<FluidWindow, object?>(nameof(TitleBarContent));

    public static readonly StyledProperty<bool> TransparencyEnabledProperty =
        AvaloniaProperty.Register<FluidWindow, bool>(nameof(TransparencyEnabled), defaultValue: true);

    public static readonly StyledProperty<bool> ShowIconProperty =
        AvaloniaProperty.Register<FluidWindow, bool>(nameof(ShowIcon), true);

    public static readonly StyledProperty<bool> ShowTitleProperty =
        AvaloniaProperty.Register<FluidWindow, bool>(nameof(ShowTitle), true);

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<FluidWindow, double>(nameof(TitleBarHeight), 48d);

    public static readonly StyledProperty<IImage?> IconSourceProperty =
        AvaloniaProperty.Register<FluidWindow, IImage?>(nameof(IconSource));

    /// <summary>Content shown in the title bar between the title and the caption buttons (e.g. a search box).</summary>
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    /// <summary>Whether the title-bar app icon is shown.</summary>
    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <summary>Whether the title-bar title text is shown (bound to <see cref="Window.Title"/>).</summary>
    public bool ShowTitle
    {
        get => GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    /// <summary>Height of the custom title bar (also the extended-client-area title-bar hint).</summary>
    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    /// <summary>The image drawn as the title-bar icon (independent of the OS/taskbar <see cref="Window.Icon"/>).</summary>
    public IImage? IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <summary>Whether to use the translucent backdrop (Mica / vibrancy / blur) versus a solid window.
    /// Seeded from the Windows "Transparency effects" setting at construction; set it to override at runtime.
    /// Where the compositor can't render the backdrop, the window stays solid regardless.</summary>
    public bool TransparencyEnabled
    {
        get => GetValue(TransparencyEnabledProperty);
        set => SetValue(TransparencyEnabledProperty, value);
    }

    private InputElement? _titleBar;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    public FluidWindow()
    {
        // Draw our own chrome inside an extended client area. BorderOnly keeps the resize border and
        // shadow but drops Avalonia's drawn title + caption buttons, so only the templated bar shows.
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = TitleBarHeight;
        WindowDecorations = WindowDecorations.BorderOnly;

        // Follow the Windows "Transparency effects" setting at startup (overridable via TransparencyEnabled);
        // request the platform's translucent backdrop, reconciled to a solid surface where unavailable.
        SetCurrentValue(TransparencyEnabledProperty, TransparencyService.IsOsTransparencyEnabled());
        TransparencyService.Apply(this, TransparencyEnabled);

        ActualThemeVariantChanged += (_, _) => ApplyTitleBarTheme();
    }

    protected override Type StyleKeyOverride => typeof(FluidWindow);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_titleBar is not null)
        {
            _titleBar.PointerPressed -= OnTitleBarPressed;
            _titleBar.DoubleTapped -= OnTitleBarDoubleTapped;
        }
        if (_minimizeButton is not null) _minimizeButton.Click -= OnMinimizeClick;
        if (_maximizeButton is not null) _maximizeButton.Click -= OnMaximizeClick;
        if (_closeButton is not null) _closeButton.Click -= OnCloseClick;

        _titleBar = e.NameScope.Find<InputElement>("PART_TitleBar");
        _minimizeButton = e.NameScope.Find<Button>("PART_MinimizeButton");
        _maximizeButton = e.NameScope.Find<Button>("PART_MaximizeButton");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");

        if (_titleBar is not null)
        {
            _titleBar.PointerPressed += OnTitleBarPressed;
            _titleBar.DoubleTapped += OnTitleBarDoubleTapped;
        }
        if (_minimizeButton is not null) _minimizeButton.Click += OnMinimizeClick;
        if (_maximizeButton is not null) _maximizeButton.Click += OnMaximizeClick;
        if (_closeButton is not null) _closeButton.Click += OnCloseClick;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ApplyTitleBarTheme();
        TransparencyService.ReconcileBackground(this);
        UpdateMaximizedPseudoClass();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty)
            UpdateMaximizedPseudoClass();
        else if (change.Property == TitleBarHeightProperty)
            ExtendClientAreaTitleBarHeightHint = TitleBarHeight;
        else if (change.Property == TransparencyEnabledProperty)
            TransparencyService.Apply(this, TransparencyEnabled);
        else if (change.Property == ActualTransparencyLevelProperty)
            TransparencyService.ReconcileBackground(this);
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e) => ToggleMaximize();

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) => ToggleMaximize();

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    // Drives the ":maximized" pseudo-class so the maximize/restore glyph can swap in the template.
    private void UpdateMaximizedPseudoClass() =>
        PseudoClasses.Set(":maximized", WindowState == WindowState.Maximized);

    private void ApplyTitleBarTheme()
    {
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        Win11.SetTitleBarTheme(hwnd, ActualThemeVariant == ThemeVariant.Dark);
    }
}
