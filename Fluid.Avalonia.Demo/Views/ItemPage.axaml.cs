using Avalonia.Controls;
using Fluid.Avalonia.Demo.Models;
using Avalonia.Markup.Xaml;

namespace Fluid.Avalonia.Demo.Views;

/// <summary>
/// Standard host for a catalog entry: a title and description header above the entry's content,
/// mirroring the WinUI 3 Gallery's ItemPage.
/// </summary>
public partial class ItemPage : UserControl
{
    public ItemPage() => InitializeComponent();

    public ItemPage(GalleryItem item) : this()
    {
        TitleText.Text = item.Title;
        DescText.Text = item.Description;
        Host.Content = item.CreatePage();
    }
}
