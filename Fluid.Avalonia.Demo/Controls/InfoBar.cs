using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>Status severity for <see cref="InfoBar"/> (matches WPF-UI's InfoBarSeverity ordering).</summary>
public enum InfoBarSeverity
{
    Informational = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>
/// A status banner — a severity icon, a SemiBold Title + wrapping Message, optional action content
/// (the inherited Content slot), and a close button. Reimplemented after WPF-UI's <c>InfoBar</c> (MIT):
/// a templated <see cref="ContentControl"/> whose severity selects the icon glyph, icon colour and
/// surface colours via :severity-* pseudo-classes; dismissal is purely <see cref="IsOpen"/>-driven
/// (the close button sets IsOpen=false). The Fluent look is in Styles/InfoBar.axaml.
/// </summary>
[TemplatePart("PART_CloseButton", typeof(Button))]
[PseudoClasses(":open", ":closable", ":no-title",
    ":severity-informational", ":severity-success", ":severity-warning", ":severity-error")]
public class InfoBar : ContentControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<InfoBar, string>(nameof(Title), defaultValue: "");

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<InfoBar, string>(nameof(Message), defaultValue: "");

    public static readonly StyledProperty<InfoBarSeverity> SeverityProperty =
        AvaloniaProperty.Register<InfoBar, InfoBarSeverity>(
            nameof(Severity), defaultValue: InfoBarSeverity.Informational);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<InfoBar, bool>(nameof(IsOpen), defaultValue: false);

    public static readonly StyledProperty<bool> IsClosableProperty =
        AvaloniaProperty.Register<InfoBar, bool>(nameof(IsClosable), defaultValue: true);

    /// <summary>Raised (bubbling) before the bar closes itself; nothing in WPF-UI, a Fluid nicety.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CloseButtonClickEvent =
        RoutedEvent.Register<InfoBar, RoutedEventArgs>(nameof(CloseButtonClick), RoutingStrategies.Bubble);

    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public InfoBarSeverity Severity { get => GetValue(SeverityProperty); set => SetValue(SeverityProperty, value); }
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public bool IsClosable { get => GetValue(IsClosableProperty); set => SetValue(IsClosableProperty, value); }

    public event EventHandler<RoutedEventArgs> CloseButtonClick
    {
        add => AddHandler(CloseButtonClickEvent, value);
        remove => RemoveHandler(CloseButtonClickEvent, value);
    }

    private Button? _closeButton;

    public InfoBar()
    {
        UpdateSeverity(Severity);
        PseudoClasses.Set(":open", IsOpen);
        PseudoClasses.Set(":closable", IsClosable);
        PseudoClasses.Set(":no-title", string.IsNullOrEmpty(Title));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_closeButton is not null)
            _closeButton.Click -= OnCloseClick;
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
        if (_closeButton is not null)
            _closeButton.Click += OnCloseClick;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CloseButtonClickEvent, this));
        SetCurrentValue(IsOpenProperty, false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == SeverityProperty)
            UpdateSeverity(Severity);
        else if (e.Property == IsOpenProperty)
            PseudoClasses.Set(":open", IsOpen);
        else if (e.Property == IsClosableProperty)
            PseudoClasses.Set(":closable", IsClosable);
        else if (e.Property == TitleProperty)
            PseudoClasses.Set(":no-title", string.IsNullOrEmpty(Title));
    }

    private void UpdateSeverity(InfoBarSeverity s)
    {
        PseudoClasses.Set(":severity-informational", s == InfoBarSeverity.Informational);
        PseudoClasses.Set(":severity-success", s == InfoBarSeverity.Success);
        PseudoClasses.Set(":severity-warning", s == InfoBarSeverity.Warning);
        PseudoClasses.Set(":severity-error", s == InfoBarSeverity.Error);
    }
}
