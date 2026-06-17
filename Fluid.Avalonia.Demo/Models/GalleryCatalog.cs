using Avalonia.Controls;
using Fluid.Avalonia.Demo.Views.Pages;

namespace Fluid.Avalonia.Demo.Models;

/// <summary>
/// A navigable entry in the gallery: a title, a one-line description, a Codicons (bundled icon
/// font) glyph for the nav rail, and a factory that builds the page shown for it.
/// </summary>
public sealed record GalleryItem(string Title, string Description, string Glyph, Func<Control> CreatePage)
{
    /// <summary>A non-selectable divider row in the nav rail (not a real page).</summary>
    public bool IsSeparator { get; init; }

    /// <summary>A shared separator sentinel for the nav list.</summary>
    public static GalleryItem Separator { get; } =
        new("", "", "", () => new Control()) { IsSeparator = true };
}

/// <summary>
/// The data that drives the NavigationView and the per-item pages — the data-driven catalog
/// pattern used by the WinUI 3 Gallery.
/// </summary>
public static class GalleryCatalog
{
    public static IReadOnlyList<GalleryItem> Items { get; } = new GalleryItem[]
    {
        new("Home", "Project overview, rendered live from README.md.", "", () => new HomePage()),
        GalleryItem.Separator,
        new("Accents", "See how the theme adapts to the OS accent — or pick a preset / custom one.", "", () => new AccentsPage()),
        new("Basic input", "Buttons, toggles, sliders and other controls that collect input from the user.", "", () => new BasicInputPage()),
        new("Collections", "Display and let users work with sets of data in lists, grids and trees.", "", () => new CollectionsPage()),
        new("Date & time", "Let users view and set date and time values.", "", () => new DateTimePage()),
        new("Dialogs & flyouts", "Surface contextual information and ask the user to confirm an action.", "", () => new DialogsPage()),
        new("Layout", "Arrange, group and size content with panels and surfaces.", "", () => new LayoutPage()),
        new("Media", "Present and play audio, video and images.", "", () => new MediaPage()),
        new("Menus & toolbars", "Expose commands and actions in menus and command bars.", "", () => new MenusPage()),
        new("Navigation", "Move between pages and organize the app's content.", "", () => new Views.Pages.NavigationPage()),
        new("Scrolling", "Pan and scroll content that is larger than the viewport.", "", () => new ScrollingPage()),
        new("Status & info", "Communicate progress, status and notifications to the user.", "", () => new StatusInfoPage()),
        new("Text", "Display, format and edit text.", "", () => new TextPage()),
        GalleryItem.Separator,
        new("Custom", "The composite controls and shell features this project adds on top of vanilla Avalonia.", "", () => new CustomPage()),
        new("Playground", "A live XAML sandbox — type Avalonia markup and see it rendered instantly, themed by Fluid.Avalonia.", "", () => new PlaygroundPage()),
    };

    /// <summary>The real pages only (no separators) — used by search.</summary>
    public static IReadOnlyList<GalleryItem> Pages { get; } =
        Items.Where(i => !i.IsSeparator).ToList();

    public static GalleryItem Settings { get; } =
        new("Settings", "Personalize the appearance of the app.", "", () => new SettingsPage());
}
