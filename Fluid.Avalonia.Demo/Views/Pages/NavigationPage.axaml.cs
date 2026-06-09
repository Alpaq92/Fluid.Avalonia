using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Fluid.Avalonia.Demo.Controls;

namespace Fluid.Avalonia.Demo.Views.Pages;

/// <summary>A folder node in the directory-picker BreadcrumbBar: a display name + its full path.</summary>
public sealed class DirectoryCrumb
{
    public DirectoryCrumb(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }

    public string FullPath { get; }

    // The crumb (and each dropdown entry) renders this.
    public override string ToString() => Name;
}

public partial class NavigationPage : UserControl
{
    private BreadcrumbBar? _dirBar;
    private TextBlock? _dirReadout;
    private bool _mock;

    public NavigationPage()
    {
        AvaloniaXamlLoader.Load(this);

        // Simple string trail — click an earlier crumb to navigate back.
        var crumbs = this.FindControl<BreadcrumbBar>("Crumbs");
        var readout = this.FindControl<TextBlock>("CrumbReadout");
        if (crumbs is not null)
        {
            var full = new[] { "Home", "Documents", "Projects", "Avalonia", "Fluid.Avalonia", "Demo", "Views", "Spec.docx" };

            // The chevron dropdown offers the next segment, so you can step forward again after going
            // back. The final crumb (the file) has no next segment, so its chevron stays inert.
            crumbs.ChildrenSelector = crumb =>
            {
                var i = Array.IndexOf(full, crumb as string);
                return i >= 0 && i + 1 < full.Length ? new[] { full[i + 1] } : null;
            };

            crumbs.ItemClicked += (_, e) =>
            {
                // Navigate back: trim the trail to the clicked crumb, which becomes the current location.
                crumbs.ItemsSource = full.Take(e.Index + 1).ToArray();
                if (readout is not null)
                    readout.Text = $"Navigated to “{e.Item}” (level {e.Index}).";
            };

            crumbs.ChildSelected += (_, e) =>
            {
                // Navigate forward: extend the trail to the chosen next segment.
                var i = Array.IndexOf(full, e.Child as string);
                if (i >= 0)
                {
                    crumbs.ItemsSource = full.Take(i + 1).ToArray();
                    if (readout is not null)
                        readout.Text = $"Navigated forward to “{e.Child}” (level {i}).";
                }
            };

            // Set the source last, so the ChildrenSelector + handlers are in place when the first
            // containers are prepared (the trail starts in dropdown mode, with the leaf chevron hidden).
            crumbs.ItemsSource = full;
        }

        SetupDirectoryBreadcrumb();
    }

    // A directory-picker breadcrumb seeded from the working directory: click a crumb to jump up the
    // tree, or click a chevron to drop down into that folder's subfolders (the Dirkster99 feature).
    private void SetupDirectoryBreadcrumb()
    {
        _dirBar = this.FindControl<BreadcrumbBar>("DirCrumbs");
        _dirReadout = this.FindControl<TextBlock>("DirReadout");
        if (_dirBar is null)
            return;

        _dirBar.ChildrenSelector = GetSubfolders;
        _dirBar.ItemClicked += (_, e) => { if (e.Item is DirectoryCrumb d) NavigateTo(d); };
        _dirBar.ChildSelected += (_, e) => { if (e.Child is DirectoryCrumb c) NavigateTo(c); };

        NavigateTo(StartCrumb());
    }

    // Working directory on desktop; an in-memory mock tree in the browser (WASM has no file system).
    private DirectoryCrumb StartCrumb()
    {
        if (!OperatingSystem.IsBrowser())
        {
            try
            {
                var path = Directory.GetCurrentDirectory();
                return new DirectoryCrumb(LeafName(path), path);
            }
            catch
            {
                // No file-system access — fall through to the mock tree below.
            }
        }

        _mock = true;
        return new DirectoryCrumb("Fluid.Avalonia", "This PC|Documents|Projects|Fluid.Avalonia");
    }

    // Rebuild the trail from the root down to the target crumb, and refresh the path readout.
    private void NavigateTo(DirectoryCrumb target)
    {
        var trail = _mock ? BuildMockTrail(target.FullPath) : BuildRealTrail(target.FullPath);
        _dirBar!.ItemsSource = trail;
        if (_dirReadout is not null)
            _dirReadout.Text = _mock ? target.FullPath.Replace("|", "  ›  ") : target.FullPath;
    }

    private static List<DirectoryCrumb> BuildRealTrail(string fullPath)
    {
        var stack = new Stack<DirectoryInfo>();
        try
        {
            for (var d = new DirectoryInfo(fullPath); d is not null; d = d.Parent)
                stack.Push(d);
        }
        catch
        {
            return new List<DirectoryCrumb> { new(LeafName(fullPath), fullPath) };
        }

        // Stack enumerates top (root, pushed last) first, giving a root -> leaf trail.
        return stack
            .Select(d => new DirectoryCrumb(
                string.IsNullOrEmpty(d.Name)
                    ? d.FullName
                    : d.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                d.FullName))
            .ToList();
    }

    private static List<DirectoryCrumb> BuildMockTrail(string fullPath)
    {
        var parts = fullPath.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var crumbs = new List<DirectoryCrumb>(parts.Length);
        for (var i = 0; i < parts.Length; i++)
            crumbs.Add(new DirectoryCrumb(parts[i], string.Join('|', parts.Take(i + 1))));
        return crumbs;
    }

    // The children of a crumb, shown in its chevron dropdown.
    private IEnumerable<DirectoryCrumb>? GetSubfolders(object? crumb)
    {
        if (crumb is not DirectoryCrumb d)
            return null;

        if (_mock)
        {
            return MockChildren.TryGetValue(d.Name, out var names)
                ? names.Select(n => new DirectoryCrumb(n, d.FullPath + "|" + n))
                : Enumerable.Empty<DirectoryCrumb>();
        }

        try
        {
            return new DirectoryInfo(d.FullPath)
                .EnumerateDirectories()
                .Where(sub => (sub.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                .OrderBy(sub => sub.Name, StringComparer.OrdinalIgnoreCase)
                .Take(256)
                .Select(sub => new DirectoryCrumb(sub.Name, sub.FullName))
                .ToList();
        }
        catch
        {
            // Inaccessible directory (permissions, removed, etc.) — show nothing.
            return Enumerable.Empty<DirectoryCrumb>();
        }
    }

    private static string LeafName(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrEmpty(name) ? path : name;
    }

    // Mock folder tree for the browser demo (no file system in WASM).
    private static readonly Dictionary<string, string[]> MockChildren = new()
    {
        ["This PC"] = new[] { "Local Disk (C:)", "Documents", "Downloads", "Pictures", "Music" },
        ["Local Disk (C:)"] = new[] { "Program Files", "Users", "Windows" },
        ["Users"] = new[] { "Public", "You" },
        ["Documents"] = new[] { "Projects", "Notes", "Reports", "Invoices" },
        ["Projects"] = new[] { "Fluid.Avalonia", "MenYou", "Playground", "Archive" },
        ["Fluid.Avalonia"] = new[] { "Fluid.Avalonia", "Fluid.Avalonia.Demo", "docs", "assets" },
        ["Downloads"] = new[] { "installers", "archives" },
        ["Pictures"] = new[] { "Screenshots", "Wallpapers" },
    };
}
