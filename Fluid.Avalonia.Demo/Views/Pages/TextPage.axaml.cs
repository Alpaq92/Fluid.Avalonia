using Avalonia.Controls;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class TextPage : UserControl
{
    public TextPage()
    {
        InitializeComponent();
        FruitBox.ItemsSource = new[] { "Apple", "Apricot", "Banana", "Blueberry", "Cherry" };
    }
}
