using Avalonia.Controls;
using Fluid.Avalonia.Demo.Controls;
using Avalonia.Markup.Xaml;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class NavigationPage : UserControl
{
    public NavigationPage()
    {
        AvaloniaXamlLoader.Load(this);

        // Seed the BreadcrumbBar trail and report clicks on earlier crumbs.
        var crumbs = this.FindControl<BreadcrumbBar>("Crumbs");
        var readout = this.FindControl<TextBlock>("CrumbReadout");
        if (crumbs is not null)
        {
            crumbs.ItemsSource = new[] { "Home", "Documents", "Design", "Spec.docx" };
            crumbs.ItemClicked += (_, e) =>
            {
                if (readout is not null)
                    readout.Text = $"Navigated to “{e.Item}” (level {e.Index}).";
            };
        }
    }
}
