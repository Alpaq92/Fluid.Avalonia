using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Fluid.Avalonia.Locale;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();

        var current = Application.Current?.RequestedThemeVariant;
        if (current == ThemeVariant.Light) LightRadio.IsChecked = true;
        else if (current == ThemeVariant.Dark) DarkRadio.IsChecked = true;
        else SystemRadio.IsChecked = true;

        LightRadio.IsCheckedChanged += OnThemeRadioChanged;
        DarkRadio.IsCheckedChanged += OnThemeRadioChanged;
        SystemRadio.IsCheckedChanged += OnThemeRadioChanged;

        // Title-bar element visibility — reflect and drive the shell (MainView).
        if (Shell is { } w)
        {
            ShowMenuSwitch.IsChecked = w.ShowMenuButton;
            ShowIconSwitch.IsChecked = w.ShowAppIcon;
            ShowTitleSwitch.IsChecked = w.ShowTitleText;
            ShowPageNavSwitch.IsChecked = w.ShowPageNav;
        }

        ShowMenuSwitch.IsCheckedChanged += (_, _) => { if (Shell is { } m) m.ShowMenuButton = ShowMenuSwitch.IsChecked == true; };
        ShowIconSwitch.IsCheckedChanged += (_, _) => { if (Shell is { } m) m.ShowAppIcon = ShowIconSwitch.IsChecked == true; };
        ShowTitleSwitch.IsCheckedChanged += (_, _) => { if (Shell is { } m) m.ShowTitleText = ShowTitleSwitch.IsChecked == true; };
        ShowPageNavSwitch.IsCheckedChanged += (_, _) => { if (Shell is { } m) m.ShowPageNav = ShowPageNavSwitch.IsChecked == true; };

        // Window backdrop — lives on the desktop Window (the browser head has no window, so it's blocked
        // there). System glass needs OS compositor support, so it's disabled where unavailable (e.g. a
        // non-KWin Linux session) — but Liquid glass is software-rendered and stays available there, which
        // is exactly why it's offered.
        if (DesktopWindow is not { } win)
        {
            SystemGlassRadio.IsEnabled = LiquidGlassRadio.IsEnabled = SolidBackdropRadio.IsEnabled = false;
            ToolTip.SetTip(SystemGlassRadio, "Desktop only — the browser head has no window backdrop.");
        }
        else
        {
            if (!TransparencyService.IsBackdropSupported(win))
            {
                SystemGlassRadio.IsEnabled = false;
                ToolTip.SetTip(SystemGlassRadio,
                    "Unavailable — this desktop environment can't render a system backdrop (no compositor blur). Use Liquid glass instead.");
                if (win.Backdrop == DemoBackdrop.SystemGlass)
                    win.Backdrop = DemoBackdrop.Solid;
            }

            (win.Backdrop switch
            {
                DemoBackdrop.SystemGlass => SystemGlassRadio,
                DemoBackdrop.LiquidGlass => LiquidGlassRadio,
                _ => SolidBackdropRadio,
            }).IsChecked = true;

            SystemGlassRadio.IsCheckedChanged += (_, _) => { if (SystemGlassRadio.IsChecked == true) win.Backdrop = DemoBackdrop.SystemGlass; };
            LiquidGlassRadio.IsCheckedChanged += (_, _) => { if (LiquidGlassRadio.IsChecked == true) win.Backdrop = DemoBackdrop.LiquidGlass; };
            SolidBackdropRadio.IsCheckedChanged += (_, _) => { if (SolidBackdropRadio.IsChecked == true) win.Backdrop = DemoBackdrop.Solid; };
        }

        // Language picker — one radio per bundled language (native name), styled like the theme picker
        // above; switches the app language live via the localizer. The initial checked state reflects
        // the current (system-default) language.
        foreach (var opt in LanguageOptions)
        {
            var radio = new RadioButton
            {
                GroupName = "language",
                Content = opt.Name,
                Tag = opt.Code,
                IsChecked = opt.Code == LocaleManager.CurrentLanguage,
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && radio.Tag is string code)
                    LocaleManager.SetLanguage(code);
            };
            LanguagePanel.Children.Add(radio);
        }
    }

    // The bundled languages, each shown by its native name (conventional for a language picker).
    private static readonly LanguageOption[] LanguageOptions =
    {
        new("en", "English"),
        new("pl", "Polski"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("es", "Español"),
    };

    // The shell (MainView) hosts the title-bar toggles, on both the desktop and browser heads.
    private static MainView? Shell => Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime d => d.MainWindow?.Content as MainView,
        ISingleViewApplicationLifetime s => s.MainView as MainView,
        _ => null,
    };

    // The desktop window that owns the translucent backdrop (null in the browser single-view head).
    private static MainWindow? DesktopWindow =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d
            ? d.MainWindow as MainWindow
            : null;

    private void OnThemeRadioChanged(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is null)
            return;

        if (LightRadio.IsChecked == true)
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
        else if (DarkRadio.IsChecked == true)
            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
        else if (SystemRadio.IsChecked == true)
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
    }
}

/// <summary>One selectable app language: code (e.g. "pl") and its native display name.</summary>
public sealed class LanguageOption(string code, string name)
{
    public string Code { get; } = code;
    public string Name { get; } = name;
}
