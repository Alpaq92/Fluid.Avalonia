using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DialogHostAvalonia;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class DialogsPage : UserControl
{
    public DialogsPage() => AvaloniaXamlLoader.Load(this);

    // Open the app-wide DialogHost (declared in MainView, Identifier="RootDialog") and await the
    // button the user picks. DialogHost.Show works the same on the desktop and browser heads.
    private async void OnShowDialog(object? sender, RoutedEventArgs e)
    {
        var result = await DialogHost.Show(BuildSaveDialog(), "RootDialog");
        if (this.FindControl<TextBlock>("DialogReadout") is { } readout)
            readout.Text = result switch
            {
                "Save" => "You chose: Save.",
                "Discard" => "You chose: Don't save.",
                "Cancel" => "You chose: Cancel.",
                _ => "Dismissed without a choice.",
            };
    }

    // Build a Fluent ContentDialog body in code so a fresh instance is shown each time (no reparenting
    // of a cached control). The buttons close the host with their result via DialogHost.Close.
    private Control BuildSaveDialog()
    {
        var title = new TextBlock { Text = "Save changes?", Classes = { "Subtitle" } };

        var message = new TextBlock
        {
            Text = "Your document has unsaved changes. Save them before closing?",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "Body" },
        };
        if (this.TryFindResource("TextFillColorSecondaryBrush", ActualThemeVariant, out var fg) && fg is IBrush brush)
            message.Foreground = brush;

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children =
            {
                DialogButton("Save", "Save", accent: true),
                DialogButton("Don't save", "Discard", accent: false),
                DialogButton("Cancel", "Cancel", accent: false),
            },
        };

        // Wrap in a Border for the Fluent ContentDialog padding (24px), matching the faux example.
        // Padding goes on the content, NOT DialogHost.Padding — the host wraps the whole shell, so
        // padding it could inset the entire page rather than just the dialog.
        return new Border
        {
            Width = 380,
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 16,
                Children = { title, message, buttons },
            },
        };
    }

    private static Button DialogButton(string text, string result, bool accent)
    {
        var button = new Button { Content = text };
        if (accent)
            button.Classes.Add("accent");
        button.Click += (_, _) => DialogHost.Close("RootDialog", result);
        return button;
    }
}
