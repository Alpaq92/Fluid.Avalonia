using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>Raised when a (non-final) crumb is clicked, carrying the crumb's data and position.</summary>
public sealed class BreadcrumbBarItemClickedEventArgs : EventArgs
{
    public BreadcrumbBarItemClickedEventArgs(object? item, int index)
    {
        Item = item;
        Index = index;
    }

    /// <summary>The clicked crumb's data item.</summary>
    public object? Item { get; }

    /// <summary>The clicked crumb's index in the trail (0 = root).</summary>
    public int Index { get; }
}

/// <summary>
/// A horizontal trail of crumbs separated by chevrons — WinUI / FluentAvalonia's BreadcrumbBar.
/// Every crumb except the last is interactive; clicking one raises <see cref="ItemClicked"/> so the
/// host can navigate back to that level. Bind <see cref="ItemsControl.ItemsSource"/> (and optionally
/// supply an <see cref="ItemsControl.ItemTemplate"/>) just like any other items control.
/// </summary>
public class BreadcrumbBar : ItemsControl
{
    /// <summary>Raised when an interactive crumb is clicked.</summary>
    public event EventHandler<BreadcrumbBarItemClickedEventArgs>? ItemClicked;

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new BreadcrumbBarItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return item is not BreadcrumbBarItem;
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is BreadcrumbBarItem crumb)
        {
            crumb.Owner = this;
            crumb.Index = index;
            crumb.IsLast = index == ItemCount - 1;
        }

        RefreshLastStates();
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        if (container is BreadcrumbBarItem crumb)
            crumb.Index = newIndex;
        RefreshLastStates();
    }

    // Keep exactly one crumb (the highest index) flagged as the current one.
    private void RefreshLastStates()
    {
        var last = ItemCount - 1;
        foreach (var c in GetRealizedContainers())
            if (c is BreadcrumbBarItem crumb)
                crumb.IsLast = crumb.Index == last;
    }

    internal void RaiseItemClicked(BreadcrumbBarItem crumb) =>
        ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(
            ItemFromContainer(crumb) ?? crumb.Content, crumb.Index));
}

/// <summary>A single crumb in a <see cref="BreadcrumbBar"/>.</summary>
[PseudoClasses(":last")]
public class BreadcrumbBarItem : ContentControl
{
    public static readonly DirectProperty<BreadcrumbBarItem, bool> IsLastProperty =
        AvaloniaProperty.RegisterDirect<BreadcrumbBarItem, bool>(
            nameof(IsLast), o => o.IsLast, (o, v) => o.IsLast = v);

    private bool _isLast;

    internal BreadcrumbBar? Owner { get; set; }

    internal int Index { get; set; }

    /// <summary>True for the final crumb — the current location, shown emphasised and non-interactive.</summary>
    public bool IsLast
    {
        get => _isLast;
        set
        {
            if (SetAndRaise(IsLastProperty, ref _isLast, value))
                PseudoClasses.Set(":last", value);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        // Only earlier crumbs navigate; the current (last) crumb is inert.
        if (!IsLast && e.InitialPressMouseButton == MouseButton.Left)
        {
            Owner?.RaiseItemClicked(this);
            e.Handled = true;
        }
    }
}
