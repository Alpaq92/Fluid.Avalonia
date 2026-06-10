using Avalonia.Controls;
using Fluid.Avalonia.Demo.Models;

namespace Fluid.Avalonia.Demo.Views.Pages;

public partial class CollectionsPage : UserControl
{
    public CollectionsPage()
    {
        InitializeComponent();

        // Sample address data from Code for America's Ohana API (see Models/SampleAddresses).
        Grid.AutoGenerateColumns = true;
        Grid.ItemsSource = SampleAddresses.All;
        Grid.SelectedIndex = 1; // show the accent row selection (mirrors the ListBox)
    }
}
