using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Fluid.Locale;
using Avalonia.Fluid.Demo.Views;

namespace Avalonia.Fluid.Demo;

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
        Avalonia.Fluid.AccentColorService.Apply(this);

        switch (ApplicationLifetime)
        {
            // Desktop: a Mica window with our custom title bar, hosting the shell.
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            // Browser (WASM) and other single-view hosts: the shell is the top level itself.
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
