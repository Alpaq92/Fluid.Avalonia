using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class BasicInputPage : UserControl
{
    private static readonly string[] SpinnerLevels = { "Low", "Medium", "High" };

    public BasicInputPage()
    {
        InitializeComponent();
        LevelSpinner.Spin += OnSpinnerSpin;

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
    // FluidWindow chrome. Desktop-only — guarded for the browser head, which has no Window.
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
            Content = new TextBlock
            {
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                Text = "This is a standard Avalonia Window. The Fluid.Avalonia demo itself runs in a "
                     + "FluidWindow — Fluid's custom Mica title bar — while this window keeps the "
                     + "platform's default title bar. Same theme, different chrome.",
            },
        };

        window.Show(owner);
    }
}
