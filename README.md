# Fluid.Avalonia

<p align="center">
  <a href="https://www.nuget.org/packages/Fluid.Avalonia"><img src="https://img.shields.io/nuget/v/Fluid.Avalonia.svg?label=NuGet&color=blue" alt="NuGet version" /></a>
  <a href="https://www.nuget.org/packages/Fluid.Avalonia"><img src="https://img.shields.io/nuget/dt/Fluid.Avalonia.svg?label=Downloads&color=blue" alt="NuGet downloads" /></a>
  <a href="https://github.com/Alpaq92/Fluid.Avalonia/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/Alpaq92/Fluid.Avalonia/ci.yml?branch=main&label=CI" alt="CI" /></a>
  <a href="https://github.com/Alpaq92/Fluid.Avalonia/actions/workflows/release.yml"><img src="https://img.shields.io/github/actions/workflow/status/Alpaq92/Fluid.Avalonia/release.yml?branch=main&label=Release" alt="Release" /></a>
  <a href="https://alpaq92.github.io/Fluid.Avalonia/"><img src="https://img.shields.io/badge/demo-live-success" alt="Live demo" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License: MIT" /></a>
</p>

A **Fluent 2-inspired** Avalonia theme with its own identity, adapting to your system accent color — read from Windows, with macOS and Linux fallbacks. Built on [Avalonia](https://avaloniaui.net/) **12** (.NET 8) and its `FluentTheme` with ported Fluent 2 design tokens, it brings authentic WinUI tokens and metrics.

![Fluid.Avalonia — the demo's Accents page, split diagonally between the light and dark themes](https://raw.githubusercontent.com/Alpaq92/Fluid.Avalonia/main/screenshot.png)

Install from NuGet:

```
dotnet add package Fluid.Avalonia
```

It ships as a single `Styles` object you drop into your app:

```
<Application xmlns:fluid="clr-namespace:Fluid.Avalonia;assembly=Fluid.Avalonia">
  <Application.Styles>
    <fluid:FluidTheme />
  </Application.Styles>
</Application>
```

The accent adapts to the OS automatically, but you can also drive it from code — `AccentService` exposes the built-in preset palette plus a small apply / reset API:

```
using System.Linq;
using Fluid.Avalonia;

// the 20 built-in Open Color presets, each with a Name + Color
foreach (var p in AccentService.Preset)
    Console.WriteLine($"{p.Name}: {p.Color}");

// apply one as the app accent…
var teal = AccentService.Preset.First(p => p.Name == "Teal");
AccentService.SetAccent(teal.Color);

// …or hand back to the live OS accent
AccentService.UseSystemAccent();
```

The repository also contains **Fluid.Avalonia.Demo**, a demo app that mirrors the structure of Microsoft's **WinUI 3 Gallery** (data-driven navigation, per-item pages, a Settings page) so you can compare the result side by side.

> **Deep dive:** the architecture and how the resource layering works, the demo-app internals, and the full coverage / roadmap matrix all live in **[OVERVIEW.md](OVERVIEW.md)**.

## Live demo

The demo runs in the browser via Avalonia's **WebAssembly** head, deployed to **GitHub Pages**:

> **Live demo →** **<https://alpaq92.github.io/Fluid.Avalonia/>** *(deployed from the WASM head on every push to `main`)*

The solution is split into a shared library plus per-platform heads:

| Project | Role |
| --- | --- |
| `Fluid.Avalonia.Demo` | Shared gallery library (App, Views, Controls, pages, assets). |
| `Fluid.Avalonia.Demo.Desktop` | Desktop head — the Mica window + custom title bar. |
| `Fluid.Avalonia.Demo.Browser` | WebAssembly head — hosts the shared `MainView` as the single top-level. |

The desktop window's content was factored into a shared `MainView` so both heads reuse the exact same shell; Windows-only bits (Mica, the `WM_SETICON` taskbar fix, the registry accent read) are guarded and simply don't run in the browser. `.github/workflows/pages.yml` publishes the Browser head and deploys it on every push to `main` — just set **Settings → Pages → Source = "GitHub Actions"** once. Build it locally with:

```
dotnet workload install wasm-tools
dotnet publish Fluid.Avalonia.Demo.Browser -c Release
```

---

## What it is?

- **A WinUI 3 look for Avalonia.** Fluent 2 color tokens, a WinUI type ramp (rendered in the bundled, cross-platform **DejaVu Sans** font), 4 px / 8 px corner radii, the "lit-edge" control border, drop-shadow elevation, and Mica window backdrop. Symbol glyphs come from the bundled **Codicons** icon font, so both text and icons render identically on desktop and in the browser.
- **Live accent integration, on every OS.** The accent is read from the host where possible — the full seven-shade **Windows** `AccentPalette`, the **macOS** `AppleAccentColor`, and the **Linux** GNOME (`accent-color`) / KDE (`kdeglobals`) / Cinnamon (Mint theme name) accent — and flows into every accented control, updating instantly when the user changes it. Where no OS accent is available, apps can pick from the **Open Color preset palette** (20 swatches) or set any color manually (e.g. with a `ColorPicker`) via `AccentService.SetAccent` / `UseSystemAccent`.
- **Cross-platform & self-contained.** One library (`Fluid.Avalonia`) targeting `net8.0` with no third-party theme dependencies — it layers on Avalonia's built-in `FluentTheme`. Platform specifics (registry / `defaults` / `gsettings` accent readers, Mica, dark title bar) are guarded and degrade gracefully everywhere.

## Why?

This started as a quest to align Avalonia with WinUI 3 — and what began as migrating the visual tokens soon grew into something more, as enhancements and new controls were added along the way. It draws on [Romzetron.Avalonia](https://github.com/Romzetron/Romzetron.Avalonia) as a reference for solution structure and the semantic-brush styling architecture, with three deliberate differences from it:

1. **No baked-in accent** — adapt to whatever accent the user has set in their OS (Windows, macOS or Linux). An Open Color preset palette and manual `ColorPicker` selection are offered as *options*, not as a hard-coded default.
2. **Avalonia 12 / .NET 8.**
3. **As close to WinUI 3 as possible** — no per-control "set the color explicitly" override mechanism; theming is purely token-driven, the way Fluent 2 works.

Rather than hand-porting ~70 control templates (and fighting Avalonia 12 template changes), the theme layers on Avalonia's `FluentTheme` and overrides the token/resource layer. This keeps it small, robust on Avalonia 12, and faithful to Fluent 2.

## Custom controls

The demo isn't only re-themed stock controls. A number of **composite controls and shell features were built specifically for this project**, with no direct equivalent in vanilla Avalonia — the `RadialTimePicker` and its reusable `RadialClock` dial, the segmented `DateTimePicker` / `DateTimeSpinners` and `AnalogDateTimePicker`, `RadialSlider`, `ProgressCircle`, `BinarySelector`, `FluidColorPicker`, `BreadcrumbBar`, `GroupBox`, the reusable `FluidWindow` shell, and more.

Each one is catalogued — with what it is and a live example — in **[CUSTOM.md](CUSTOM.md)**, a detailed reference for every custom control and feature authored for Fluid.Avalonia (also rendered live on the demo's **Custom** page).

## Building & running

Requires the **.NET 8 SDK** (pinned via `global.json`).

```
dotnet build
dotnet run --project Fluid.Avalonia.Demo
```

The accent is read natively on Windows, macOS and Linux (GNOME / KDE / Cinnamon), falling back to Avalonia's platform accent elsewhere — and can always be overridden with a preset or a picked color. Mica and the dark title bar are Windows-only; other platforms use a solid base background.

## Inspirations

- **[Fluent 2 Design System](https://fluent2.microsoft.design/)** — type ramp, color tokens, elevation and materials guidance.
- **[microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml)** (MIT) — the canonical WinUI 3 control theme resources (`src/controls/dev/CommonStyles/*_themeresources.xaml`) and `Common_themeresources` color values were ported from here.
- **[WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery)** (MIT) — the structural reference for the demo app (data-driven catalog, NavigationView shell, ItemPage, ControlExample).
- **[Romzetron.Avalonia](https://github.com/Romzetron/Romzetron.Avalonia)** — solution structure and the file-per-control / semantic-brush styling architecture.
- **[FluentAvalonia](https://github.com/amwx/FluentAvalonia)** — cross-checked our approach.
- **[SukiUI](https://github.com/kikipoulet/SukiUI)** — reference for the custom window / title-bar technique, the thin overlay scrollbar look, and the Playground page concept.
- **[Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)** — reference for the roomy circular-day Calendar styling.
- **[Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia)** — renders the Home page.
- **[Material Design Icons](https://pictogrammers.com/library/mdi/)** (Apache-2.0) — the *code-array* glyph used in the app icon.
- **[Open Color](https://yeun.github.io/open-color/)** (MIT) — the colors behind the 20 built-in accent presets.
- **[Avalonia](https://github.com/AvaloniaUI/Avalonia)** — the `FluentTheme` we build on.

## Credits

The full list of third-party projects this solution bundles, depends on, or references — each with what it's used for and its license — lives in **[CREDITS.md](CREDITS.md)**.

## License

[MIT](LICENSE). Ported color values and resource structures originate from the MIT-licensed [microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml) and [WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery) projects, © Microsoft Corporation.
