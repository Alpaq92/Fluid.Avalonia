using Avalonia;
using Avalonia.Controls;
using Fluid.Avalonia.Demo.Models;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Fluid.Avalonia.Demo.Views;

public partial class MainView : UserControl
{
    private const string MaximizeGlyph = "";
    private const string RestoreGlyph = "";

    private Window? _window;

    // Below this width (DIPs) the nav pane collapses into an overlay drawer instead of an always-open
    // inline pane, so the page content isn't crushed into a sliver on phones / narrow windows.
    private const double NavCollapseWidth = 720;
    private bool? _navIsNarrow;

    public MainView()
    {
        InitializeComponent();

        // The app title is the project's namespace / assembly name (Fluid.Avalonia.Demo), not a
        // hardcoded literal — so it tracks the project name automatically.
        AppTitle.Text = typeof(App).Namespace;

        // Title-bar controls. Window drag is handled on the bar's empty areas; the buttons handle
        // their own clicks. The caption buttons act on the hosting Window (desktop only).
        Hamburger.Click += (_, _) => Split.IsPaneOpen = !Split.IsPaneOpen;
        PrevPageBtn.Click += (_, _) => StepPage(-1);
        NextPageBtn.Click += (_, _) => StepPage(+1);
        MinBtn.Click += (_, _) => { if (_window is { } w) w.WindowState = WindowState.Minimized; };
        MaxBtn.Click += (_, _) => ToggleMaximize();
        CloseBtn.Click += (_, _) => _window?.Close();
        TitleBar.PointerPressed += OnTitleBarPressed;
        TitleBar.DoubleTapped += (_, _) => ToggleMaximize();

        // Navigation (data-driven from the catalog).
        NavList.ItemsSource = GalleryCatalog.Items;
        NavList.ContainerPrepared += OnNavContainerPrepared;
        NavList.SelectionChanged += OnNavChanged;
        FooterNav.SelectionChanged += OnFooterChanged;

        // Collapse the pane to an overlay drawer on narrow viewports (see OnEffectiveViewportChanged).
        EffectiveViewportChanged += OnEffectiveViewportChanged;

        // Functional search over the catalog (real pages only — no separators).
        SearchBox.ItemsSource = GalleryCatalog.Pages;
        SearchBox.ItemFilter = (search, item) =>
            item is GalleryItem galleryItem &&
            (string.IsNullOrWhiteSpace(search) ||
             galleryItem.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase));
        SearchBox.ItemSelector = (_, item) =>
            item is GalleryItem galleryItem ? galleryItem.Title : string.Empty;
        SearchBox.SelectionChanged += OnSearchSelected;

        NavList.SelectedIndex = 0;
        UpdateLeadingInset();
        UpdatePageNavState();
    }

    // The ordered page sequence the prev/next buttons walk: the real nav pages, then Settings.
    private int CurrentPageIndex()
    {
        var pages = GalleryCatalog.Pages;
        if (FooterNav.SelectedItem is not null)
            return pages.Count; // Settings sits after the last nav page
        if (NavList.SelectedItem is GalleryItem item && !item.IsSeparator)
        {
            for (var i = 0; i < pages.Count; i++)
                if (ReferenceEquals(pages[i], item))
                    return i;
        }
        return -1;
    }

    // Move forward/back one page in that sequence (clamped at the ends).
    private void StepPage(int direction)
    {
        var pages = GalleryCatalog.Pages;
        var last = pages.Count; // index of Settings
        var current = CurrentPageIndex();
        if (current < 0)
            current = 0;

        var target = Math.Clamp(current + direction, 0, last);
        if (target == current)
            return;

        if (target == last)
            FooterNav.SelectedIndex = 0;          // navigate to Settings
        else
            NavList.SelectedItem = pages[target]; // navigate to a nav page
    }

    // Disable a button when there's nowhere left to go in that direction.
    private void UpdatePageNavState()
    {
        var current = CurrentPageIndex();
        PrevPageBtn.IsEnabled = current > 0;
        NextPageBtn.IsEnabled = current >= 0 && current < GalleryCatalog.Pages.Count;
    }

    /// <summary>Show/hide the title-bar menu (hamburger) button. Driven by the Settings page.</summary>
    public bool ShowMenuButton { get => Hamburger.IsVisible; set { Hamburger.IsVisible = value; UpdateLeadingInset(); } }

    /// <summary>Show/hide the title-bar app icon.</summary>
    public bool ShowAppIcon { get => AppIcon.IsVisible; set { AppIcon.IsVisible = value; UpdateLeadingInset(); } }

    /// <summary>Show/hide the title-bar title text.</summary>
    public bool ShowTitleText { get => AppTitle.IsVisible; set { AppTitle.IsVisible = value; UpdateLeadingInset(); } }

    /// <summary>Show/hide the title-bar page prev/next buttons. Driven by the Settings page.</summary>
    public bool ShowPageNav { get => PageNavButtons.IsVisible; set => PageNavButtons.IsVisible = value; }

    /// <summary>Navigate the shell to a catalog page — used by the desktop tray menu. Settings routes
    /// through the footer nav; every other page selects in the main nav list.</summary>
    public void NavigateTo(GalleryItem item)
    {
        if (ReferenceEquals(item, GalleryCatalog.Settings))
            FooterNav.SelectedIndex = 0;
        else
            NavList.SelectedItem = item;
    }

    // Keep a comfortable left inset on whichever leading title-bar element is first visible: when the
    // hamburger (which normally provides that inset) is hidden, the icon — or the title — would
    // otherwise sit flush against the window edge.
    private void UpdateLeadingInset()
    {
        const double firstInset = 14, gap = 6;
        var seen = false;

        void Place(Control c, double whenFirst)
        {
            if (!c.IsVisible)
                return;
            c.Margin = new Thickness(seen ? gap : whenFirst, 0, 0, 0);
            seen = true;
        }

        Place(Hamburger, 4); // the hamburger keeps its tight 4px when it leads
        Place(AppIcon, firstInset);
        Place(AppTitle, firstInset);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The caption buttons / window drag only make sense with a hosting desktop Window;
        // in single-view (Browser) there is none, so hide them.
        _window = TopLevel.GetTopLevel(this) as Window;
        CaptionButtons.IsVisible = _window is not null;
        if (_window is not null)
        {
            _window.Title = typeof(App).Namespace; // keep the OS/taskbar title in sync with the project name
            UpdateMaxGlyph();
            _window.PropertyChanged += OnHostWindowPropertyChanged;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
        {
            _window.PropertyChanged -= OnHostWindowPropertyChanged;
            _window = null;
        }
    }

    private void OnHostWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty)
            UpdateMaxGlyph();
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_window is { } w && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            w.BeginMoveDrag(e);
    }

    private void ToggleMaximize()
    {
        if (_window is { } w)
            w.WindowState = w.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void UpdateMaxGlyph() =>
        MaxGlyph.Text = _window?.WindowState == WindowState.Maximized ? RestoreGlyph : MaximizeGlyph;

    // Collapse the nav pane into an overlay drawer on narrow (phone) viewports so the page content
    // isn't squeezed into a sliver; restore the always-open inline pane when there's room again. Only
    // act on an actual narrow<->wide transition, so we never fight a manual hamburger toggle in between.
    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var width = e.EffectiveViewport.Width;
        if (width <= 0)
            return;

        var narrow = width < NavCollapseWidth;
        if (_navIsNarrow == narrow)
            return;
        _navIsNarrow = narrow;

        Split.DisplayMode = narrow ? SplitViewDisplayMode.Overlay : SplitViewDisplayMode.Inline;
        Split.IsPaneOpen = !narrow;

        // In the overlay drawer the pane floats over the content, so it must be opaque (a style keys the
        // solid background off this class) or the page shows through it. The content should also sit
        // flush and use the full width, dropping the desktop pane-seam border + wide insets.
        if (narrow)
            Split.Classes.Add("narrowNav");
        else
            Split.Classes.Remove("narrowNav");

        ContentBorder.BorderThickness = narrow ? new Thickness(0) : new Thickness(1, 1, 0, 0);
        ContentBorder.CornerRadius = narrow ? new CornerRadius(0) : new CornerRadius(8, 0, 0, 0);
        ContentHost.Margin = narrow ? new Thickness(18, 20, 18, 24) : new Thickness(36, 28, 44, 28);
    }

    // In the narrow overlay mode, dismiss the drawer after navigating so the chosen page is visible.
    private void CloseNavIfOverlay()
    {
        if (_navIsNarrow == true)
            Split.IsPaneOpen = false;
    }

    private void OnNavChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not GalleryItem item || item.IsSeparator)
            return;

        FooterNav.SelectedItem = null;
        ContentHost.Content = new ItemPage(item);
        UpdatePageNavState();
        CloseNavIfOverlay();
    }

    // Give separator rows a thin, non-interactive container theme; real rows keep the nav theme.
    private void OnNavContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        if (e.Container is not ListBoxItem item)
            return;

        var isSeparator = e.Index >= 0 && e.Index < GalleryCatalog.Items.Count
            && GalleryCatalog.Items[e.Index].IsSeparator;
        var key = isSeparator ? "NavSeparatorItem" : "NavItem";
        if (this.TryFindResource(key, out var theme) && theme is ControlTheme controlTheme)
            item.Theme = controlTheme;
    }

    private void OnFooterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FooterNav.SelectedItem is null)
            return;

        NavList.SelectedItem = null;
        ContentHost.Content = new ItemPage(GalleryCatalog.Settings);
        UpdatePageNavState();
        CloseNavIfOverlay();
    }

    private void OnSearchSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (SearchBox.SelectedItem is GalleryItem item)
            NavList.SelectedItem = item; // syncs the nav pill and navigates
    }
}
