using System;
using Avalonia.Controls;

namespace Avalonia.Fluid.Demo.Views.Pages;

public partial class BasicInputPage : UserControl
{
    private static readonly string[] SpinnerLevels = { "Low", "Medium", "High" };

    public BasicInputPage()
    {
        InitializeComponent();
        LevelSpinner.Spin += OnSpinnerSpin;
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
}
