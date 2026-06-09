using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Fluid.Avalonia.Demo.Models;
using Fluid.Avalonia.Demo.Views;
using Fluid.Avalonia.Locale;

namespace Fluid.Avalonia.Demo;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Localization (Semi.Avalonia / SukiUI style): merge the system-language ResourceDictionary
        // (Locale/<code>.axaml) now, before any UI resolves {DynamicResource STRING_*}. Falls back to
        // English; change it at runtime via LocaleManager.SetLanguage (e.g. the Settings page).
        LocaleManager.UseSystemDefault();

        // Ensure the accent palette is published now that the platform is fully ready.
        Fluid.Avalonia.AccentService.Apply(this);

        switch (ApplicationLifetime)
        {
            // Desktop: a Mica window with our custom title bar, hosting the shell — plus a tray icon.
            case IClassicDesktopStyleApplicationLifetime desktop:
                var window = new MainWindow();
                desktop.MainWindow = window;
                SetupTrayIcon(desktop, window);
                break;
            // Browser (WASM) and other single-view hosts: the shell is the top level itself.
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    // A desktop-only system-tray icon with a Fluent-themed context menu. On Windows, Avalonia renders
    // the tray menu itself (the same MenuFlyoutPresenter as in-app menus), so it inherits the
    // Fluid.Avalonia menu styling; on macOS/Linux it falls back to the OS-native menu. The menu
    // mirrors the shell navigation, plus window actions. Guarded to desktop, so the browser is unaffected.
    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        void ShowWindow()
        {
            window.Show();
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
        }

        // A menu item that restores the window and navigates the shell to a catalog page.
        NativeMenuItem Nav(GalleryItem page)
        {
            var item = new NativeMenuItem { Header = page.Title };
            item.Click += (_, _) => { ShowWindow(); window.Shell.NavigateTo(page); };
            return item;
        }

        var menu = new NativeMenu();

        // Home / Custom / Playground.
        menu.Add(Nav(GalleryCatalog.Items[0]));               // Home
        menu.Add(Nav(GalleryCatalog.Pages.First(p => p.Title == "Custom")));
        menu.Add(Nav(GalleryCatalog.Pages.First(p => p.Title == "Playground")));
        menu.Add(new NativeMenuItemSeparator());

        // The control-demo pages (Accents … Text) as a submenu.
        var controlsSub = new NativeMenu();
        foreach (var page in GalleryCatalog.Pages.Where(p => p.Title is not ("Home" or "Custom" or "Playground")))
            controlsSub.Add(Nav(page));
        menu.Add(new NativeMenuItem { Header = "Controls", Menu = controlsSub });
        menu.Add(new NativeMenuItemSeparator());

        // Settings + window actions.
        menu.Add(Nav(GalleryCatalog.Settings));

        var show = new NativeMenuItem { Header = "Show" };
        show.Click += (_, _) => ShowWindow();
        menu.Add(show);

        var close = new NativeMenuItem { Header = "Close" };
        close.Click += (_, _) => desktop.Shutdown();
        menu.Add(close);

        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Fluid.Avalonia.Demo/Assets/logo.ico"))),
            ToolTipText = typeof(App).Namespace,
            Menu = menu,
            IsVisible = true,
        };
        tray.Clicked += (_, _) => ShowWindow();   // left-clicking the tray icon restores the window

        TrayIcon.SetIcons(this, new TrayIcons { tray });
    }
}
