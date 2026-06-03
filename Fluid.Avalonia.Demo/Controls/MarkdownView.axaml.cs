using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Fluid.Avalonia.Demo.Controls;

/// <summary>
/// Renders a Markdown document (loaded from an <c>avares://</c> asset) inside a Fluent card, using a
/// theme-aware <see cref="Markdown.Avalonia.MarkdownScrollViewer"/> style. Shared by the Home and
/// Custom pages so the markdown look is authored once. Set <see cref="Source"/> to an avares URI.
/// </summary>
public partial class MarkdownView : UserControl
{
    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<MarkdownView, string?>(nameof(Source));

    private IDisposable? _widthSub;

    public MarkdownView()
    {
        InitializeComponent();
    }

    /// <summary>The <c>avares://</c> URI of the Markdown document to render.</summary>
    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty)
            Md.Markdown = LoadMarkdown(Source);
    }

    // The MarkdownScrollViewer measures its content at its natural width and overflows the page.
    // Cap its width to the real viewport — the ancestor ScrollViewer is fixed to its slot even
    // when its content overflows — so the markdown wraps to fit instead of being clipped.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var scroller = this.FindAncestorOfType<ScrollViewer>();
        if (scroller is null)
            return;

        _widthSub = scroller.GetObservable(Visual.BoundsProperty).Subscribe(new BoundsObserver(rect =>
        {
            // viewport minus the ScrollViewer padding (72) and the card padding (56)
            var available = rect.Width - 128;
            Md.MaxWidth = available > 240 ? available : double.PositiveInfinity;
        }));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _widthSub?.Dispose();
        _widthSub = null;
    }

    private sealed class BoundsObserver(Action<Rect> onNext) : IObserver<Rect>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(Rect value) => onNext(value);
    }

    private static string LoadMarkdown(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        try
        {
            using var stream = AssetLoader.Open(new Uri(source));
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            return $"# Document unavailable\n\n{ex.Message}";
        }
    }
}
