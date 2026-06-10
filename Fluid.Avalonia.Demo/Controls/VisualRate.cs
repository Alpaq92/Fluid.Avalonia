using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>One item in a <see cref="VisualRate"/>: its 1-based ordinal, the glyph to draw and
/// whether it is lit (ordinal &lt;= the control's Value). Raises change notifications so the template's
/// <c>Classes.selected</c> binding re-evaluates as the value changes.</summary>
public sealed class VisualRateItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public VisualRateItem(int ordinal, string glyph)
    {
        Ordinal = ordinal;
        Glyph = glyph;
    }

    /// <summary>The 1-based position of this item.</summary>
    public int Ordinal { get; }

    /// <summary>The Codicon character this item renders (a copy of the control's <see cref="VisualRate.Glyph"/>).</summary>
    public string Glyph { get; }

    /// <summary>True when this item is lit (its ordinal is &lt;= the control's Value).</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// A row of clickable glyphs setting a 0..N value. Generalised from a port of Avalonia's official
/// Avalonia.Samples <c>RatingControlSample</c> (MIT): a TemplatedControl whose PART_ItemsPresenter
/// ItemsControl renders one Codicon glyph (a <see cref="TextBlock"/> in the SymbolThemeFontFamily) per
/// ordinal; clicking one sets <see cref="Value"/> to that ordinal, and clicking the topmost lit glyph
/// again unselects it (stepping Value down — so clicking the only lit glyph clears to 0). Where the
/// sample hard-coded a star
/// <c>Path</c>, <see cref="Glyph"/> lets any single Codicon glyph be chosen (default the filled
/// <c>star-full</c>, U+EB59). Re-themed to Fluent: because one filled glyph is recoloured between states,
/// an unlit glyph's interior matches its edge (a solid neutral <c>ControlStrongStrokeColorDefaultBrush</c>)
/// and a lit one is the accent. Each item carries its own <see cref="VisualRateItem.IsSelected"/>
/// flag (lit when ordinal &lt;= Value), so the template uses a plain <c>Classes.selected</c> binding
/// rather than the sample's MultiBinding (which the Avalonia 12 compiled-XAML loader rejects as a
/// property element). The look is in Styles/VisualRate.axaml.
/// </summary>
[TemplatePart("PART_ItemsPresenter", typeof(ItemsControl))]
public class VisualRate : TemplatedControl
{
    public static readonly StyledProperty<int> CountProperty =
        AvaloniaProperty.Register<VisualRate, int>(
            nameof(Count), defaultValue: 5, coerce: CoerceCount);

    public static readonly DirectProperty<VisualRate, int> ValueProperty =
        AvaloniaProperty.RegisterDirect<VisualRate, int>(
            nameof(Value), o => o.Value, (o, v) => o.Value = v,
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    public static readonly StyledProperty<string> GlyphProperty =
        AvaloniaProperty.Register<VisualRate, string>(
            nameof(Glyph), defaultValue: ""); // Codicon star-full

    public static readonly DirectProperty<VisualRate, IEnumerable<VisualRateItem>> ItemsProperty =
        AvaloniaProperty.RegisterDirect<VisualRate, IEnumerable<VisualRateItem>>(
            nameof(Items), o => o.Items);

    private int _value;
    private IEnumerable<VisualRateItem> _items;
    private ItemsControl? _itemsPresenter;

    public VisualRate() => _items = BuildItems(Count);

    /// <summary>How many glyphs to show (the maximum selectable value). Coerced to at least 1.</summary>
    public int Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public int Value
    {
        get => _value;
        // The range is 0..Count: 0 = nothing selected (the click-to-unselect floor).
        set => SetAndRaise(ValueProperty, ref _value, Math.Max(0, value));
    }

    /// <summary>The Codicon character every item renders (default the filled <c>star-full</c>, U+EB59).</summary>
    public string Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>The item source for the PART_ItemsPresenter ItemsControl, one per ordinal.</summary>
    public IEnumerable<VisualRateItem> Items
    {
        get => _items;
        private set => SetAndRaise(ItemsProperty, ref _items, value);
    }

    private static int CoerceCount(AvaloniaObject sender, int value) => Math.Max(1, value);

    private List<VisualRateItem> BuildItems(int count)
    {
        var glyph = Glyph;
        var list = new List<VisualRateItem>(count);
        for (var i = 1; i <= count; i++)
            list.Add(new VisualRateItem(i, glyph) { IsSelected = i <= Value });
        return list;
    }

    private void UpdateItems() => Items = BuildItems(Count);

    private void UpdateSelection()
    {
        foreach (var item in _items)
            item.IsSelected = item.Ordinal <= Value;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Count and Glyph both rebuild the item list (the glyph is copied onto each item);
        // Value only relights the existing items.
        if (change.Property == CountProperty || change.Property == GlyphProperty)
            UpdateItems();
        else if (change.Property == ValueProperty)
            UpdateSelection();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_itemsPresenter is not null)
            _itemsPresenter.PointerReleased -= ItemsPresenter_PointerReleased;
        _itemsPresenter = e.NameScope.Find<ItemsControl>("PART_ItemsPresenter");
        if (_itemsPresenter is not null)
            _itemsPresenter.PointerReleased += ItemsPresenter_PointerReleased;
    }

    private void ItemsPresenter_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // The ItemTemplate renders a TextBlock per item (not a star Path), so hit-test on that.
        // Clicking the topmost lit glyph (ordinal == Value) unselects it — stepping Value down to
        // ordinal - 1 (0 when it was the only lit one); any other glyph selects up/down to itself.
        if (e.Source is TextBlock tb && tb.DataContext is VisualRateItem item)
            Value = item.Ordinal == Value ? item.Ordinal - 1 : item.Ordinal;
    }

    protected override void UpdateDataValidation(
        AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        if (property == ValueProperty)
            DataValidationErrors.SetError(this, error);
    }
}
