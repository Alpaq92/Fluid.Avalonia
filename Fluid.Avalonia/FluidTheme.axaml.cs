using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Fluid.Avalonia;

/// <summary>
/// A WinUI 3-styled theme for Avalonia. It layers WinUI 3 typography and metrics over
/// FluentTheme and drives the accent color from the current Windows accent palette.
///
/// Usage:
/// <code>
/// &lt;Application.Styles&gt;
///     &lt;winui:FluidTheme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// </summary>
public class FluidTheme : Styles
{
    public FluidTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);

        if (Application.Current is { } app)
            AccentService.Apply(app);
    }
}
