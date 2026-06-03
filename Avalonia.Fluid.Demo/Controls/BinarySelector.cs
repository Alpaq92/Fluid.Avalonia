using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace Avalonia.Fluid.Demo.Controls;

/// <summary>
/// A two-value segmented selector: two options shown side by side with an accent pill sliding to the
/// active one. It returns an object — <see cref="Value"/> equals <see cref="LeftValue"/> when the left
/// side is active and <see cref="RightValue"/> when the right is. <see cref="LeftContent"/> /
/// <see cref="RightContent"/> override the displayed labels; when unset, each side shows its value.
/// Subclasses <see cref="ToggleButton"/> so it keeps the <c>:checked</c> state (false = left, true =
/// right), click and keyboard handling; clicking a side selects it, the keyboard flips. The Fluent
/// look is in Styles/BinarySelector.axaml.
/// </summary>
[TemplatePart("PART_LeftContent", typeof(ContentPresenter))]
[TemplatePart("PART_RightContent", typeof(ContentPresenter))]
public class BinarySelector : ToggleButton
{
    public static readonly StyledProperty<object?> LeftValueProperty =
        AvaloniaProperty.Register<BinarySelector, object?>(nameof(LeftValue));

    public static readonly StyledProperty<object?> RightValueProperty =
        AvaloniaProperty.Register<BinarySelector, object?>(nameof(RightValue));

    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<BinarySelector, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<BinarySelector, object?>(nameof(RightContent));

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<BinarySelector, object?>(
            nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The value the left side represents.</summary>
    public object? LeftValue { get => GetValue(LeftValueProperty); set => SetValue(LeftValueProperty, value); }

    /// <summary>The value the right side represents.</summary>
    public object? RightValue { get => GetValue(RightValueProperty); set => SetValue(RightValueProperty, value); }

    /// <summary>Optional label for the left side; falls back to <see cref="LeftValue"/> when unset.</summary>
    public object? LeftContent { get => GetValue(LeftContentProperty); set => SetValue(LeftContentProperty, value); }

    /// <summary>Optional label for the right side; falls back to <see cref="RightValue"/> when unset.</summary>
    public object? RightContent { get => GetValue(RightContentProperty); set => SetValue(RightContentProperty, value); }

    /// <summary>The selected value: <see cref="LeftValue"/> when the left side is active, otherwise
    /// <see cref="RightValue"/> (two-way).</summary>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private ContentPresenter? _left, _right;
    private bool? _pressedRight;   // which half a pointer pressed (so a click selects that side)
    private bool _sync;            // guards the Value <-> IsChecked sync from re-entering

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _left = e.NameScope.Find("PART_LeftContent") as ContentPresenter;
        _right = e.NameScope.Find("PART_RightContent") as ContentPresenter;
        UpdateContent();
        SyncValueFromChecked();   // seed Value from the initial side
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed && Bounds.Width > 0)
            _pressedRight = pt.Position.X > Bounds.Width / 2;   // click a side to select it
        base.OnPointerPressed(e);
    }

    // ToggleButton calls this on click / Space / Enter. A pointer click selects the pressed side; the
    // keyboard (no pointer) flips. Setting IsChecked drives the pill + the Value sync below.
    protected override void Toggle()
    {
        var right = _pressedRight ?? IsChecked != true;
        _pressedRight = null;
        SetCurrentValue(IsCheckedProperty, right);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsCheckedProperty || e.Property == LeftValueProperty || e.Property == RightValueProperty)
        {
            SyncValueFromChecked();
            UpdateContent();
        }
        else if (e.Property == ValueProperty)
        {
            SyncCheckedFromValue();
        }
        else if (e.Property == LeftContentProperty || e.Property == RightContentProperty)
        {
            UpdateContent();
        }
    }

    // The active side is the source of truth; Value follows it.
    private void SyncValueFromChecked()
    {
        if (_sync)
            return;
        _sync = true;
        try { SetCurrentValue(ValueProperty, IsChecked == true ? RightValue : LeftValue); }
        finally { _sync = false; }
    }

    // When Value is set/bound externally, move the pill to the matching side.
    private void SyncCheckedFromValue()
    {
        if (_sync)
            return;
        _sync = true;
        try { SetCurrentValue(IsCheckedProperty, Equals(Value, RightValue)); }
        finally { _sync = false; }
    }

    private void UpdateContent()
    {
        if (_left is not null)
            _left.Content = LeftContent ?? LeftValue;
        if (_right is not null)
            _right.Content = RightContent ?? RightValue;
    }
}
