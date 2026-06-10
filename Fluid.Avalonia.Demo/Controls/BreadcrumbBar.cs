using System.Collections;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>Raised when a crumb is clicked, carrying the crumb's data item and its position in the trail.</summary>
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

/// <summary>Raised when a child is chosen from a crumb's chevron dropdown (the directory-picker feature).</summary>
public sealed class BreadcrumbBarChildSelectedEventArgs : EventArgs
{
    public BreadcrumbBarChildSelectedEventArgs(object? parent, int parentIndex, object? child)
    {
        Parent = parent;
        ParentIndex = parentIndex;
        Child = child;
    }

    /// <summary>The crumb whose chevron dropdown was opened.</summary>
    public object? Parent { get; }

    /// <summary>That crumb's index in the trail.</summary>
    public int ParentIndex { get; }

    /// <summary>The child item the user picked from the dropdown.</summary>
    public object? Child { get; }
}

/// <summary>
/// A horizontal breadcrumb trail of crumbs joined by chevrons. Reimplemented after WPF-UI's
/// <c>BreadcrumbBar</c> (MIT) — every crumb except the current one is interactive and raises
/// <see cref="ItemClicked"/> (and runs <see cref="Command"/>) so the host can navigate back to that
/// level — extended, in the spirit of Dirkster99's directory-picker breadcrumb (MIT), so the chevron
/// *between* crumbs becomes a dropdown of that crumb's children (<see cref="ChildrenSelector"/> +
/// <see cref="ChildSelected"/>), and with WinUI-style overflow: when the trail is wider than the
/// control, the leading crumbs collapse behind a clickable <c>…</c> that drops them down.
/// </summary>
[TemplatePart("PART_Overflow", typeof(Border))]
public class BreadcrumbBar : ItemsControl
{
    /// <summary>An optional command invoked (with the clicked crumb's data) after <see cref="ItemClicked"/>.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<BreadcrumbBar, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Supplies the children shown in a crumb's chevron dropdown. Given a crumb's data item it returns
    /// that item's children (or null/empty for none). Evaluated lazily each time a dropdown opens.
    /// When null, chevrons are plain separators (and the final crumb has none), matching WPF-UI.
    /// </summary>
    public Func<object?, IEnumerable?>? ChildrenSelector { get; set; }

    /// <summary>Raised when an interactive crumb is clicked.</summary>
    public event EventHandler<BreadcrumbBarItemClickedEventArgs>? ItemClicked;

    /// <summary>Raised when a child is chosen from a crumb's chevron dropdown.</summary>
    public event EventHandler<BreadcrumbBarChildSelectedEventArgs>? ChildSelected;

    private Border? _overflow;
    private ListBox? _overflowList;
    private int _firstVisible;   // number of leading crumbs the panel has collapsed behind the ellipsis

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
            var hasDropdown = ChildrenSelector is not null;
            var isLast = index == ItemCount - 1;
            crumb.Owner = this;
            crumb.Index = index;
            crumb.SetHasDropdown(hasDropdown);
            crumb.IsLast = isLast;
            // Set the leaf state on this container directly: it may not yet be in GetRealizedContainers()
            // when RefreshContainerStates runs during its own preparation, so relying on that alone would
            // leave the final crumb's chevron showing.
            crumb.SetLeaf(isLast && hasDropdown && !HasChildren(item));
        }

        RefreshContainerStates();
    }

    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        if (container is BreadcrumbBarItem crumb)
            crumb.Index = newIndex;
        RefreshContainerStates();
    }

    private void RefreshContainerStates()
    {
        var last = ItemCount - 1;
        var hasDropdown = ChildrenSelector is not null;
        foreach (var c in GetRealizedContainers())
            if (c is BreadcrumbBarItem crumb)
            {
                var isLast = crumb.Index == last;
                crumb.IsLast = isLast;
                crumb.SetHasDropdown(hasDropdown);
                // The current (last) crumb only shows its chevron if it has children to drop into; a
                // leaf — a file, or an empty folder — shows none. Earlier crumbs always have the next
                // crumb as a child, so only the last one needs testing.
                crumb.SetLeaf(isLast && hasDropdown && !HasChildren(ItemFromContainer(crumb) ?? crumb.Content));
            }
    }

    private bool HasChildren(object? data)
    {
        if (ChildrenSelector?.Invoke(data) is not { } children)
            return false;
        foreach (var _ in children)
            return true;
        return false;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_overflow is not null)
            _overflow.PointerReleased -= OnOverflowPressed;
        if (_overflowList is not null)
            _overflowList.SelectionChanged -= OnOverflowSelectionChanged;

        _overflow = e.NameScope.Find<Border>("PART_Overflow");
        if (_overflow is not null)
        {
            _overflow.PointerReleased += OnOverflowPressed;
            if (FlyoutBase.GetAttachedFlyout(_overflow) is Flyout flyout && flyout.Content is ListBox list)
            {
                _overflowList = list;
                _overflowList.SelectionChanged += OnOverflowSelectionChanged;
            }
        }
    }

    internal void RaiseItemClicked(BreadcrumbBarItem crumb) =>
        RaiseItemClicked(ItemFromContainer(crumb) ?? crumb.Content, crumb.Index);

    private void RaiseItemClicked(object? data, int index)
    {
        ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(data, index));
        if (Command is { } cmd && cmd.CanExecute(data))
            cmd.Execute(data);
    }

    internal IEnumerable? GetChildrenFor(BreadcrumbBarItem crumb) =>
        ChildrenSelector?.Invoke(ItemFromContainer(crumb) ?? crumb.Content);

    internal void RaiseChildSelected(BreadcrumbBarItem crumb, object? child) =>
        ChildSelected?.Invoke(this, new BreadcrumbBarChildSelectedEventArgs(
            ItemFromContainer(crumb) ?? crumb.Content, crumb.Index, child));

    // Called by BreadcrumbOverflowPanel after each arrange: how many leading crumbs it collapsed.
    internal void SetOverflow(int firstVisible)
    {
        if (_firstVisible == firstVisible && (_overflow is null || _overflow.IsVisible == firstVisible > 0))
            return;
        _firstVisible = firstVisible;
        if (_overflow is not null)
            _overflow.IsVisible = firstVisible > 0;
    }

    private void OnOverflowPressed(object? sender, PointerReleasedEventArgs e)
    {
        if (_overflow is null || _overflowList is null || _firstVisible <= 0 ||
            e.InitialPressMouseButton != MouseButton.Left)
            return;

        var hidden = new List<object?>();
        for (var i = 0; i < _firstVisible && i < Items.Count; i++)
            hidden.Add(Items[i]);

        _overflowList.SelectedItem = null;
        _overflowList.ItemsSource = hidden;
        FlyoutBase.ShowAttachedFlyout(_overflow);
        e.Handled = true;
    }

    private void OnOverflowSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_overflowList?.SelectedItem is not { } picked)
            return;

        var index = -1;
        for (var i = 0; i < Items.Count; i++)
            if (Equals(Items[i], picked)) { index = i; break; }

        _overflowList.SelectedItem = null;
        if (_overflow is not null)
            FlyoutBase.GetAttachedFlyout(_overflow)?.Hide();
        RaiseItemClicked(picked, index);
    }
}

/// <summary>A single crumb in a <see cref="BreadcrumbBar"/>.</summary>
[PseudoClasses(":last", ":dropdown", ":leaf")]
[TemplatePart("PART_CrumbBorder", typeof(Border))]
[TemplatePart("PART_Chevron", typeof(Border))]
public class BreadcrumbBarItem : ContentControl
{
    /// <summary>Optional content shown before the crumb text (WPF-UI parity).</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<BreadcrumbBarItem, object?>(nameof(Icon));

    /// <summary>Spacing around <see cref="Icon"/>.</summary>
    public static readonly StyledProperty<Thickness> IconMarginProperty =
        AvaloniaProperty.Register<BreadcrumbBarItem, Thickness>(nameof(IconMargin));

    private bool _isLast;
    private bool _hasDropdown;
    private Border? _crumbBorder;
    private Border? _chevron;
    private ListBox? _childList;

    internal BreadcrumbBar? Owner { get; set; }

    internal int Index { get; set; }

    /// <summary>True for the final crumb — the current location, shown emphasised (styled via :last).</summary>
    public bool IsLast
    {
        get => _isLast;
        set
        {
            if (_isLast == value)
                return;
            _isLast = value;
            PseudoClasses.Set(":last", value);
        }
    }

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Thickness IconMargin
    {
        get => GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    internal void SetHasDropdown(bool value)
    {
        _hasDropdown = value;
        PseudoClasses.Set(":dropdown", value);
    }

    internal void SetLeaf(bool value) => PseudoClasses.Set(":leaf", value);

    // The crumb part navigates unless it's the inert current location (last + no dropdown).
    private bool IsCrumbInteractive => !IsLast || _hasDropdown;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_crumbBorder is not null)
            _crumbBorder.PointerReleased -= OnCrumbPointerReleased;
        _crumbBorder = e.NameScope.Find<Border>("PART_CrumbBorder");
        if (_crumbBorder is not null)
            _crumbBorder.PointerReleased += OnCrumbPointerReleased;

        if (_chevron is not null)
            _chevron.PointerReleased -= OnChevronPointerReleased;
        if (_childList is not null)
            _childList.SelectionChanged -= OnChildSelectionChanged;

        _chevron = e.NameScope.Find<Border>("PART_Chevron");
        if (_chevron is not null)
        {
            _chevron.PointerReleased += OnChevronPointerReleased;
            if (FlyoutBase.GetAttachedFlyout(_chevron) is Flyout flyout && flyout.Content is ListBox list)
            {
                _childList = list;
                _childList.SelectionChanged += OnChildSelectionChanged;
            }
        }
    }

    private void OnCrumbPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (IsCrumbInteractive && e.InitialPressMouseButton == MouseButton.Left)
        {
            Owner?.RaiseItemClicked(this);
            e.Handled = true;
        }
    }

    private void OnChevronPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_hasDropdown || _chevron is null || e.InitialPressMouseButton != MouseButton.Left)
            return;

        var children = Owner?.GetChildrenFor(this)?.Cast<object?>().ToList();
        if (children is null || children.Count == 0)
            return; // no subfolders / a leaf (e.g. a file) — nothing to drop into

        if (_childList is not null)
        {
            _childList.SelectedItem = null;
            _childList.ItemsSource = children;
        }

        FlyoutBase.ShowAttachedFlyout(_chevron);
        e.Handled = true;
    }

    private void OnChildSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_childList?.SelectedItem is { } child)
        {
            _childList.SelectedItem = null;
            if (_chevron is not null)
                FlyoutBase.GetAttachedFlyout(_chevron)?.Hide();
            Owner?.RaiseChildSelected(this, child);
        }
    }
}

/// <summary>
/// The items panel for <see cref="BreadcrumbBar"/>: lays the crumbs out on a single line and, when
/// they don't all fit, collapses the leading ones (arranged off-screen so they keep their measured
/// width and the layout stays stable) and reserves space at the start for the control's ellipsis.
/// </summary>
public class BreadcrumbOverflowPanel : Panel
{
    /// <summary>Width reserved at the left for the "…" overflow chip when crumbs are collapsed.</summary>
    private const double EllipsisReserve = 54;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        // Want the full width, but never report more than offered so the control stays bounded.
        var w = double.IsInfinity(availableSize.Width) ? width : Math.Min(width, availableSize.Width);
        return new Size(w, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var n = Children.Count;
        var firstVisible = 0;

        double total = 0;
        for (var i = 0; i < n; i++)
            total += Children[i].DesiredSize.Width;

        // If the whole trail doesn't fit, collapse leading crumbs (keep at least the last one) and
        // leave room for the ellipsis chip.
        if (n > 1 && total > finalSize.Width)
        {
            var budget = finalSize.Width - EllipsisReserve;
            double acc = 0;
            firstVisible = n - 1;
            for (var i = n - 1; i >= 0; i--)
            {
                acc += Children[i].DesiredSize.Width;
                if (acc <= budget)
                    firstVisible = i;
                else
                    break;
            }
        }

        var x = firstVisible > 0 ? EllipsisReserve : 0;
        for (var i = 0; i < n; i++)
        {
            var child = Children[i];
            if (i < firstVisible)
            {
                // Collapsed: arrange off-screen (not zero-size) so it keeps its measured width.
                child.Arrange(new Rect(-100000, 0, child.DesiredSize.Width, finalSize.Height));
            }
            else
            {
                child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                x += child.DesiredSize.Width;
            }
        }

        this.GetVisualAncestors().OfType<BreadcrumbBar>().FirstOrDefault()?.SetOverflow(firstVisible);
        return finalSize;
    }
}
