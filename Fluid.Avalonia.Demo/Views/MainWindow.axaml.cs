using Avalonia.Controls;
using Fluid.Avalonia.Demo.Interop;
using Avalonia.Styling;

namespace Fluid.Avalonia.Demo.Views;

/// <summary>How the window paints its backdrop.</summary>
public enum DemoBackdrop
{
    /// <summary>The OS's own backdrop — Mica (Windows), vibrancy (macOS), a KWin blur (KDE).</summary>
    SystemGlass,

    /// <summary>A cross-platform frosted acrylic backdrop rendered in software — works on any
    /// platform, even where the OS offers no Mica / blur.</summary>
    Acrylic,

    /// <summary>An opaque, solid window.</summary>
    Solid,
}

public partial class MainWindow : Window
{
    private DemoBackdrop _backdrop;

    public MainWindow()
    {
        // Kill the Linux "flash of light theme": Avalonia resolves the OS dark scheme asynchronously on
        // X11, so a follow-OS window paints light for one frame then flips to dark. Seed the right variant
        // synchronously here, before this window first paints (no-op on Windows/macOS, which resolve it
        // synchronously). The Settings "System" option still hands back to follow-OS when picked.
        SystemTheme.SeedStartupVariant();

        InitializeComponent();

        // Extend content into the title-bar area and draw our own chrome. BorderOnly keeps the
        // resize border/shadow but drops Avalonia's drawn title + caption buttons, so only the
        // MainView's custom title bar shows (no doubled title).
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 48;
        WindowDecorations = WindowDecorations.BorderOnly;

        // Backdrop, seeded from the Windows "Transparency effects" setting: system glass (Mica / vibrancy /
        // KWin blur) when on, else solid. The Settings page also offers Acrylic — a cross-platform,
        // software-rendered alternative. The choice can change at runtime.
        _backdrop = TransparencyService.IsOsTransparencyEnabled() ? DemoBackdrop.SystemGlass : DemoBackdrop.Solid;
        ApplyBackdrop();

        ActualThemeVariantChanged += (_, _) =>
        {
            ApplyTitleBarTheme();
            // Re-resolve the (concrete, theme-derived) window background when the variant flips —
            // e.g. Linux detecting the OS dark scheme after the window opened, or the Settings theme
            // switch. Without this the window keeps the stale base while text + content flip, giving
            // a light surface under light text (the unreadable look on Linux Mint dark).
            TransparencyService.ReconcileBackground(this);
        };
    }

    /// <summary>How the window paints its backdrop (driven by the Settings page's backdrop radios).</summary>
    public DemoBackdrop Backdrop
    {
        get => _backdrop;
        set
        {
            _backdrop = value;
            ApplyBackdrop();
        }
    }

    // System glass requests the OS backdrop (Mica / vibrancy / KWin blur); the granted level resolves
    // asynchronously, so the background reconcile happens in OnPropertyChanged when ActualTransparencyLevel
    // changes (an eager reconcile here would read a stale level). Acrylic and Solid use an opaque
    // window — Acrylic then shows its own SkiaSharp frosted layer (AcrylicBase + AcrylicLayer) over it,
    // which needs no OS support, so it works on every platform.
    private void ApplyBackdrop()
    {
        var acrylic = _backdrop == DemoBackdrop.Acrylic;
        AcrylicBase.IsVisible = acrylic;
        AcrylicLayer.IsVisible = acrylic;
        TransparencyService.Apply(this, _backdrop == DemoBackdrop.SystemGlass);
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
