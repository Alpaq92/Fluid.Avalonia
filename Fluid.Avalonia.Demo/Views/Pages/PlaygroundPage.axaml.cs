using Avalonia.Controls;
#if !NATIVE_AOT
using Avalonia.Markup.Xaml;
#endif
using Avalonia.Media;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class PlaygroundPage : UserControl
{
    private const string Sample =
        "<StackPanel Spacing=\"14\" Width=\"300\">\n" +
        "  <TextBlock Classes=\"Subtitle\" Text=\"Hello, Fluid.Avalonia!\" />\n" +
        "  <TextBlock Classes=\"Body\" TextWrapping=\"Wrap\"\n" +
        "             Text=\"Edit the XAML on the left — it renders live, themed by Fluid.Avalonia.\" />\n" +
        "  <Button Classes=\"accent\" Content=\"Accent button\" />\n" +
        "  <CheckBox Content=\"A checkbox\" IsChecked=\"True\" />\n" +
        "  <ToggleSwitch IsChecked=\"True\" />\n" +
        "  <Slider Minimum=\"0\" Maximum=\"100\" Value=\"40\" />\n" +
        "  <ProgressBar Value=\"60\" />\n" +
        "</StackPanel>";

    public PlaygroundPage()
    {
        InitializeComponent();
        Editor.Text = Sample;
        Editor.TextChanged += (_, _) => Render();
        Render();
    }

    private void Render() => Preview.Content = BuildPreview(Editor.Text);

    private static Control BuildPreview(string? xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml))
            return Hint("Type Avalonia XAML on the left to see it rendered here.");

#if NATIVE_AOT
        return string.Equals(xaml, Sample, StringComparison.Ordinal)
            ? new PlaygroundSamplePreview()
            : Hint("Runtime XAML preview is unavailable in NativeAOT builds.");
#else
        // Provide the Avalonia namespaces if the snippet doesn't declare its own.
        var markup = xaml.Contains("xmlns")
            ? xaml
            : "<StackPanel xmlns=\"https://github.com/avaloniaui\" " +
              "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" + xaml + "</StackPanel>";

        try
        {
            return AvaloniaRuntimeXamlLoader.Parse<Control>(markup);
        }
        catch (Exception ex)
        {
            return ErrorBox(ex.Message);
        }
#endif
    }

    private static Control Hint(string text) => new TextBlock
    {
        Text = text,
        Opacity = 0.6,
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 320,
        TextAlignment = TextAlignment.Center,
    };

    private static Control ErrorBox(string message)
    {
        var red = Color.Parse("#E81123");
        var panel = new StackPanel { Spacing = 6, MaxWidth = 420 };
        panel.Children.Add(new TextBlock
        {
            Text = "Parse error",
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(red),
        });
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

        return new Border
        {
            Padding = new Thickness(14, 12),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x80, red.R, red.G, red.B)),
            Background = new SolidColorBrush(Color.FromArgb(0x1F, red.R, red.G, red.B)),
            Child = panel,
        };
    }
}
