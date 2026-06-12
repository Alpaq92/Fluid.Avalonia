using Avalonia.Controls;
using Fluid.Avalonia.Demo.Interop;
using Avalonia.Styling;

namespace Fluid.Avalonia.Demo.Views;

public partial class MainWindow : Window
{
    private bool _transparencyEnabled;

    public MainWindow()
    {
        InitializeComponent();

        // Extend content into the title-bar area and draw our own chrome. BorderOnly keeps the
        // resize border/shadow but drops Avalonia's drawn title + caption buttons, so only the
        // MainView's custom title bar shows (no doubled title).
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 48;
        WindowDecorations = WindowDecorations.BorderOnly;

        // Translucent backdrop (Mica / vibrancy / blur), seeded from the Windows "Transparency effects"
        // setting; the Settings page exposes a switch that overrides this at runtime.
        _transparencyEnabled = TransparencyService.IsOsTransparencyEnabled();
        TransparencyService.Apply(this, _transparencyEnabled);

        ActualThemeVariantChanged += (_, _) => ApplyTitleBarTheme();
    }

    /// <summary>Whether the window uses the translucent backdrop (driven by the Settings switch).</summary>
    public bool TransparencyEnabled
    {
        get => _transparencyEnabled;
        set
        {
            _transparencyEnabled = value;
            // Apply only sets the hint; the granted level resolves asynchronously, so the background
            // reconcile happens in OnPropertyChanged when ActualTransparencyLevel changes (mirroring
            // the library's FluidWindow). An eager reconcile here would read a stale level and leave
            // the window see-through after toggling off (or solid after toggling on).
            TransparencyService.Apply(this, value);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ApplyTitleBarTheme();
        TransparencyService.ReconcileBackground(this);
        ApplyOffScreenMargin();
        ApplyWin32TaskbarIcon();
    }

    // Avalonia's WindowIcon → native HICON conversion can produce a broken taskbar icon on Windows.
    // Build the HICON with Windows' own ICO loader (LoadImage) from the embedded logo and push it via
    // WM_SETICON, which the taskbar / Alt-Tab read directly. No-op off Windows.
    private void ApplyWin32TaskbarIcon()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            if (TryGetPlatformHandle()?.Handle is not { } hwnd || hwnd == IntPtr.Zero)
                return;

            // The .ico is embedded as an avares resource; LoadImage needs a file path, so spill it.
            var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "avalonia-fluid-demo.ico");
            using (var src = global::Avalonia.Platform.AssetLoader.Open(new Uri("avares://Fluid.Avalonia.Demo/Assets/logo.ico")))
            using (var dst = System.IO.File.Create(tmp))
                src.CopyTo(dst);

            const uint IMAGE_ICON = 1, LR_LOADFROMFILE = 0x10;
            const uint WM_SETICON = 0x80;
            var big = LoadImage(IntPtr.Zero, tmp, IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
            var small = LoadImage(IntPtr.Zero, tmp, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
            if (big != IntPtr.Zero) SendMessage(hwnd, WM_SETICON, (IntPtr)1, big);   // ICON_BIG
            if (small != IntPtr.Zero) SendMessage(hwnd, WM_SETICON, (IntPtr)0, small); // ICON_SMALL
        }
        catch
        {
            // Best-effort cosmetic fix; ignore failures.
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cx, int cy, uint fuLoad);

    // With a custom (extended) client area, a maximized window overflows the work area — its edges
    // run under the taskbar/screen edges, cutting off the bottom of the content. OffScreenMargin is
    // exactly that overflow; insetting the shell by it keeps everything on-screen (it's 0 when normal).
    private void ApplyOffScreenMargin() => Shell.Margin = OffScreenMargin;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty || change.Property == OffScreenMarginProperty)
            ApplyOffScreenMargin();
        else if (change.Property == ActualTransparencyLevelProperty)
            TransparencyService.ReconcileBackground(this);   // the deferred reconcile for the toggle
    }

    private void ApplyTitleBarTheme()
    {
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        Win11.SetTitleBarTheme(hwnd, ActualThemeVariant == ThemeVariant.Dark);
    }
}
