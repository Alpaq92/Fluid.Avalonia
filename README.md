# Avalonia.Fluid

A theme for [Avalonia](https://avaloniaui.net/) **12** (.NET 8): a **Fluent 2 inspired** look with
its own identity — drawing on WinUI 3 for its tokens and metrics, then adapting automatically to
whatever accent color the user has set (Windows, with macOS and Linux fallbacks).

It ships as a single `Styles` object you drop into your app:

```
<Application xmlns:fluid="clr-namespace:Avalonia.Fluid;assembly=Avalonia.Fluid">
  <Application.Styles>
    <fluid:FluidTheme />
  </Application.Styles>
</Application>
```

The repository also contains **Avalonia.Fluid.Demo**, a demo app that mirrors the structure of
Microsoft's **WinUI 3 Gallery** (data-driven navigation, per-item pages, a Settings page) so you
can compare the result side by side.

## Live demo

The demo runs in the browser via Avalonia's **WebAssembly** head, deployed to **GitHub Pages**:

> **Live demo →** **<https://alpaq92.github.io/Avalonia.Fluid/>** *(deployed from the WebAssembly head on every push to `main`)*

The solution is split into a shared library plus per-platform heads:

| Project | Role |
| --- | --- |
| `Avalonia.Fluid.Demo` | Shared gallery library (App, Views, Controls, pages, assets). |
| `Avalonia.Fluid.Demo.Desktop` | Desktop head — the Mica window + custom title bar. |
| `Avalonia.Fluid.Demo.Browser` | WebAssembly head — hosts the shared `MainView` as the single top-level. |

The desktop window's content was factored into a shared `MainView` so both heads reuse the exact
same shell; Windows-only bits (Mica, the `WM_SETICON` taskbar fix, the registry accent read) are
guarded and simply don't run in the browser. `.github/workflows/pages.yml` publishes the Browser
head and deploys it on every push to `main` — just set **Settings → Pages → Source = "GitHub
Actions"** once. Build it locally with:

```
dotnet workload install wasm-tools
dotnet publish Avalonia.Fluid.Demo.Browser -c Release
```

---

## What it is

- **A WinUI 3 look for Avalonia.** Fluent 2 color tokens, a WinUI type ramp (rendered in the bundled,
  cross-platform **DejaVu Sans** font), 4 px / 8 px corner radii, the "lit-edge" control border,
  drop-shadow elevation, and Mica window backdrop. Symbol glyphs come from the bundled **Codicons**
  icon font, so both text and icons render identically on desktop and in the browser.
- **Live accent integration, on every OS.** The accent is read from the host where possible —
  the full seven-shade **Windows** `AccentPalette`, the **macOS** `AppleAccentColor`, and the
  **Linux** GNOME (`accent-color`) / KDE (`kdeglobals`) / Cinnamon (Mint theme name) accent — and flows into every accented
  control, updating instantly when the user changes it. Where no OS accent is available, apps can
  pick from a **Metro-inspired preset palette** (20 swatches) or set any color manually (e.g.
  with a `ColorPicker`) via `AccentColorService.SetAccent` / `UseSystemAccent`.
- **Cross-platform & self-contained.** One library (`Avalonia.Fluid`) targeting `net8.0` with no
  third-party theme dependencies — it layers on Avalonia's built-in `FluentTheme`. Platform
  specifics (registry / `defaults` / `gsettings` accent readers, Mica, dark title bar) are guarded
  and degrade gracefully everywhere.

## How it works

The project is built on one key observation:

> **Avalonia's `FluentTheme` and WinUI use the *same resource-key names*.**
> Avalonia's Fluent control templates were derived from the WinUI control set, so a CheckBox in
> Avalonia resolves brushes named `CheckBoxCheckBackgroundFillChecked`, `ToggleSwitchFillOn`,
> `ButtonBackgroundPointerOver`, etc. — the exact keys WinUI's `*_themeresources.xaml` files
> define.

That means we don't have to re-template every control. We override the **resource layer** with
authentic Fluent 2 values, and Avalonia's existing templates render them.

### Architecture (the `Avalonia.Fluid` library)

| Layer | File | What it does |
|-------|------|--------------|
| Theme entry point | `FluidTheme.axaml(.cs)` | A `Styles` that hosts `<FluentTheme/>` and merges everything below. Its own `Styles.Resources` are resolved *before* the nested FluentTheme, so every override wins. Calls `AccentColorService.Apply`. |
| Color tokens | `Resources/Fluent2Colors.axaml` | The full WinUI 3 / Fluent 2 token set (`TextFillColor*`, `ControlFillColor*`, `ControlAltFillColor*`, `SubtleFillColor*`, `CardBackgroundFillColor*`, `LayerFillColor*`, `SolidBackgroundFillColor*`, `*StrokeColor*`, `AccentFillColor*`, `SystemFillColor*`, Acrylic/Smoke) as both `Color` and `*Brush`, split into `Default` (light) / `Dark` theme dictionaries. Values come from WinUI's `Common_themeresources`. |
| Elevation | `Resources/Elevation.axaml` | The WinUI "lit-edge" gradient borders (`ControlElevationBorderBrush`, `AccentControlElevationBorderBrush`, …) and `BoxShadows` for card / flyout / dialog / tooltip elevation. |
| Metrics & type | `Resources/ThemeResources.axaml` | `ControlCornerRadius` 4 / `OverlayCornerRadius` 8 and the full type ramp (Caption → Display) sizes and line heights. The font-family slots default to `$Default` (the theme is **font-agnostic**); the **demo** bundles **DejaVu Sans** for text and **Codicons** for `SymbolThemeFontFamily` glyphs — both cross-platform, so desktop and browser match. |
| Accent | `AccentColorService.cs` | Resolves the accent in order — a manual override (`SetAccent`, e.g. a preset or picked color) → the live OS accent (Windows `AccentPalette` 7-shade ramp; macOS `AppleAccentColor`; Linux GNOME `accent-color` / KDE `kdeglobals` / Cinnamon Mint theme name) → Avalonia `PlatformSettings.AccentColor1` → a neutral fallback — and publishes `SystemAccentColor` + `…Light1-3` / `…Dark1-3` into `Application.Resources`, subscribing to `ColorValuesChanged` for live updates. Single-color sources get the six shades derived with FluentTheme's HSL steps. Exposes a rainbow-ordered `Presets` palette (20 Metro hues + neutrals) and `SetAccent` / `UseSystemAccent` for manual selection. |
| Control port | `Controls/Button.axaml` | A faithful port of WinUI's `Button_themeresources.xaml` as a full `ControlTheme` (per-state brushes, `11,5,11,6` padding, 1 px border, 0.083 s background transition, accent variant). |
| Control colors | `Controls/ControlColors.axaml` | A port of the WinUI `*_themeresources.xaml` color maps for CheckBox, RadioButton, ToggleSwitch, Slider and ToggleButton — recoloring them to exact Fluent 2 values through Avalonia's templates. |
| Scrollbar | `Controls/ScrollBar.axaml` | A SukiUI-style thin overlay `ScrollBar` `ControlTheme` — a near-invisible track with a thin rounded thumb that widens + darkens on hover (animated), authored on Fluent 2 tokens. |
| Calendar | `Controls/Calendar.axaml` | Semi.Avalonia-style `Calendar` / `CalendarItem` / `CalendarDayButton` / `CalendarButton` themes — a roomy month grid with circular day cells, an accent-filled selected day and an accent ring for today, an opaque popup surface, and Codicon chevron nav glyphs. Also a retemplated `CalendarDatePicker` whose drop-down button is a Codicon chevron-down (the popup hosts the themed `Calendar` via the `PART_Calendar` part). |
| NumericUpDown | `Controls/NumericUpDown.axaml` | A WinUI NumberBox-style `ButtonSpinner` theme (also used by `NumericUpDown`): a single bordered field matching our TextBox, compact side-by-side chevron spin buttons, and an accent border on focus. |
| Typography / cards | `Controls/Typography.axaml`, `Controls/Card.axaml` | Type-ramp `TextBlock` classes and a reusable `Border.Card` surface. |

### Accent flow

```text
  Source (first match wins)
    manual override   SetAccent(color)         ← preset palette · ColorPicker
    Windows           HKCU\…\Explorer\Accent → AccentPalette (7 shades)
    macOS             defaults read -g AppleAccentColor
    Linux             gsettings …accent-color · ~/.config/kdeglobals
    Avalonia          PlatformSettings.AccentColor1
    fallback          neutral Fluent blue
  │
  ▼
  AccentColorService     single colors → 6 shades via FluentTheme HSL steps
  │                      · live ColorValuesChanged
  ▼
  Application.Resources  SystemAccentColor  +  …Light1-3  /  …Dark1-3
  │
  ▼  (every accent brush resolves these via DynamicResource)
  Consumers              AccentFillColorDefaultBrush · ToggleSwitchFillOn ·
                         CheckBoxCheckBackgroundFillChecked · accent Button · …
```

Because every accent brush is a `DynamicResource`, changing the OS accent, picking a preset, or
toggling light/dark re-resolves the whole UI with no restart.

## Why

This started as a request for an Avalonia theme that looks like WinUI 3, using
[Romzetron.Avalonia](https://github.com/Romzetron/Romzetron.Avalonia) as a reference for solution
structure and the semantic-brush styling architecture, with three deliberate differences from it:

1. **No baked-in accent** — adapt to whatever accent the user has set in their OS (Windows, macOS
   or Linux). A Metro-inspired preset palette and manual `ColorPicker` selection are offered as
   *options*, not as a hard-coded default.
2. **Avalonia 12 / .NET 8.**
3. **As close to WinUI 3 as possible** — no per-control "set the color explicitly" override
   mechanism; theming is purely token-driven, the way Fluent 2 works.

Rather than hand-porting ~70 control templates (and fighting Avalonia 12 template changes), the
theme layers on Avalonia's `FluentTheme` and overrides the token/resource layer. This keeps it
small, robust on Avalonia 12, and faithful to Fluent 2.

## The demo app (`Avalonia.Fluid.Demo`)

A WinUI 3 Gallery-style shell, using the Gallery's **data-driven** structure:

- **Custom title bar** — the client area is extended into the title bar (Avalonia 12
  `ExtendClientAreaToDecorationsHint` + the `WindowDecorationProperties.ElementRole="TitleBar"`
  chrome role, the technique used by [SukiUI](https://github.com/kikipoulet/SukiUI)). It hosts a
  working hamburger (toggles the pane), the app icon, the title, a centered search box, and custom
  minimize / maximize / close buttons — all over the Mica backdrop.
- **Reaching the bottom of a page** — three things conspired to clip the last item, all fixed: the
  content inset lives on the scrolled content's `Margin`, **not** `ScrollViewer.Padding` (Avalonia
  subtracts that padding from the scrollable extent, leaving the last padding-worth of content
  unreachable); the window starts **centered** at a height that fits the work area; and the root is
  inset by the window's `OffScreenMargin` so a maximized, extended-client-area window doesn't run
  under the taskbar.
- **Functional search** — an `AutoCompleteBox` in the title bar bound to the catalog; picking a
  suggestion navigates to that page.
- **`Models/GalleryCatalog.cs`** — the catalog of navigable entries (title, description, icon
  glyph, page factory). This is the equivalent of the Gallery's `ControlInfoDataSource`.
- **NavigationView** — a `SplitView` whose item list is bound to the catalog, with Codicon
  glyphs, an accent selection pill, and Settings pinned to the bottom.
- **`Views/ItemPage`** — the standard host for a catalog entry: a title + description header above
  the entry's content (the Gallery's `ItemPage`).
- **`Controls/ControlExample`** — the Gallery's example card: description, live example, an
  optional options pane, and a collapsible **Source code** footer.
- **Pages** under `Views/Pages/`:
  - **Home** — renders this `README.md` live at runtime with
    [Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia) (the file is bundled as an
    Avalonia resource and loaded via `AssetLoader`, not hardcoded).
  - **Custom** — renders [`CUSTOM.md`](CUSTOM.md) the same way: a catalogue of every non-vanilla
    control and theme/shell feature authored for this project.
  - **Playground** — a live XAML sandbox (in the spirit of SukiUI's Playground): type Avalonia
    markup in an editor and see it rendered instantly in a preview pane, themed by Avalonia.Fluid,
    with a graceful parse-error panel. Rendered via `AvaloniaRuntimeXamlLoader`
    (`Avalonia.Markup.Xaml.Loader`).
  - **Accents** — try the live system accent, pick from the outlined Metro-inspired preset
    palette (20 swatches), or set any color under Fluent-styled **Color Space** (spectrum + hue) /
    **Sliders** tabs; mode checkboxes pick the active source and a live preview re-tints instantly.
  - Basic input, Collections, Date & time, Dialogs & flyouts, Layout, Media, Menus & toolbars,
    Navigation, Scrolling, Status & info, Text — plus a **Settings** page with a
    light/dark/system theme switch (which also has toggles to show/hide the title-bar menu button,
    icon and title). Between them the demo exercises the full Avalonia control set, including
    `ToggleSplitButton`, `NumericUpDown`, `ButtonSpinner`, `Label`, `ColorPicker` / `ColorSpectrum`,
    `ContextMenu`, `TabStrip`, `Carousel` + `PipsPager`, `HeaderedContentControl`, `PathIcon` and an
    `AutoCompleteBox` (autocomplete over country names) —
    plus a few demo-only composites: a
    `BusyArea` overlay (ring / bar / circular indicators), a `FluentColorPicker` dropdown, a
    `LabeledTextInput`, a `BinarySelector` (an AM/PM-style two-value segmented selector — both options
    shown, an accent pill slides to the active one, and `Value` returns the chosen object — on the
    Basic input page), a `RadialTimePicker` (an Actipro-inspired, Fluent-styled concentric dual-ring
    time slider — inner ring = hour, outer ring = minute, each a Fluent slider bent into a circle —
    in 12-hour and `Is24Hour` Material modes — factored into a reusable `RadialClock` dial), a
    `DateTimePicker` (a segmented date + time field that opens looping spinner columns — the reusable
    `DateTimeSpinners` — 12- and 24-hour) and a `AnalogDateTimePicker` (the same field, but its
    dropdown pairs a `Calendar` with that radial dial, 12- and 24-hour) — all on the Date & time page
    (the spinner columns and the radial+Calendar combo are also shown inline there). The dial's ring
    geometry is factored into a shared `RadialDial` helper that powers two more
    controls: a `RadialSlider` (a circular slider, on the Basic input page) and a `ProgressCircle` (a
    determinate radial progress ring, on the Status & info page). Plus
    a couple of controls ported and re-themed to Fluent 2: a `BreadcrumbBar` (from WinUI /
    FluentAvalonia) and a `GroupBox` titled card (from SukiUI), both catalogued on the demo's
    **Custom** page. The Status & info page also shows the `InfoBadge` control from the
    [Avalonia.Labs](https://github.com/AvaloniaUI/Avalonia.Labs) package (a dot / number status capsule
    with severity classes) rather than a hand-rolled one.
- A Mica window. The custom title bar uses `SystemDecorations="BorderOnly"` so Avalonia's own
  drawn title/caption buttons are suppressed (no duplicated title) while the resize border is kept.
- **Localization** done the way [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia) /
  [SukiUI](https://github.com/kikipoulet/SukiUI) do it — and, like them, it lives **in the `Avalonia.Fluid`
  theme library itself** (`Avalonia.Fluid/Locale/<code>.axaml`: en / pl / de / fr / es): per-culture
  `ResourceDictionary` files of keyed `<x:String>`, merged and swapped at runtime by
  `Avalonia.Fluid.Locale.LocaleManager`. **Pure Avalonia, no localization package.** The custom pickers' OK / Cancel /
  Reset labels and their field placeholders switch language live — and so do several **native** Avalonia
  controls, by overriding Avalonia's own theme string keys: the `DatePicker` / `TimePicker` field
  placeholders (`StringDatePicker*Text` / `StringTimePicker*Text`) and the `TextBox` right-click
  Cut / Copy / Paste menu (`StringTextFlyout*Text`), which its templates read via `{DynamicResource}`.
  Defaults to the system language (falling back to English); change it in code (`LocaleManager.SetLanguage`)
  or from
  **Settings → Language** — a radio group of native language names, styled like the theme picker. (The
  Settings page chrome itself is plain English.)
- **App icon** — an original mark (`Assets/logo.svg` → `logo.png` / `logo.ico`): a blue rounded
  square with the [Material Design Icons](https://pictogrammers.com/library/mdi/icon/code-array/)
  *code-array* brackets in white. The `.ico` is a crisp multi-size icon (16–256 px) whose frames are
  each rendered natively (anti-aliased) rather than down-scaled from one bitmap, and the title-bar
  `Image` downsamples with `BitmapInterpolationMode=HighQuality`, so it stays sharp at every size.

## Building & running

Requires the **.NET 8 SDK** (pinned via `global.json`).

```
dotnet build
dotnet run --project Avalonia.Fluid.Demo
```

The accent is read natively on Windows, macOS and Linux (GNOME / KDE / Cinnamon), falling back to Avalonia's
platform accent elsewhere — and can always be overridden with a preset or a picked color. Mica and
the dark title bar are Windows-only; other platforms use a solid base background.

## Inspirations

- **[Fluent 2 Design System](https://fluent2.microsoft.design/)** — type ramp, color tokens,
  elevation and materials guidance.
- **[microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml)** (MIT) — the canonical
  WinUI 3 control theme resources (`src/controls/dev/CommonStyles/*_themeresources.xaml`) and
  `Common_themeresources` color values were ported from here.
- **[WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery)** (MIT) — the structural
  reference for the demo app (data-driven catalog, NavigationView shell, ItemPage, ControlExample).
- **[Romzetron.Avalonia](https://github.com/Romzetron/Romzetron.Avalonia)** — solution structure
  and the file-per-control / semantic-brush styling architecture.
- **[FluentAvalonia](https://github.com/amwx/FluentAvalonia)** — cross-checked our approach.
- **[SukiUI](https://github.com/kikipoulet/SukiUI)** — reference for the custom window / title-bar
  technique, the thin overlay scrollbar look, and the Playground page concept.
- **[Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)** — reference for the roomy
  circular-day Calendar styling.
- **[Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia)** — renders the Home page.
- **[Material Design Icons](https://pictogrammers.com/library/mdi/)** (Apache-2.0) — the
  *code-array* glyph used in the app icon.
- **[Avalonia](https://github.com/AvaloniaUI/Avalonia)** — the `FluentTheme` we build on.

## Coverage & roadmap

**Done:** Fluent 2 token system (light/dark) · cross-platform accent (Windows 7-shade / macOS /
Linux, live) + Metro presets + manual `ColorPicker` override · bundled DejaVu Sans text + Codicons glyphs
(cross-platform) · 4/8
corner radii · lit-edge borders · elevation shadows · Mica + theme-following title bar · full Button
template port · color-accurate CheckBox / RadioButton / ToggleSwitch / Slider / ToggleButton ·
SukiUI-style thin overlay scrollbar · Semi-style (opaque) Calendar · audited ColorView (clean
ColorSpectrum + hue Color Space tab; Sliders tab with flat Fluent channel sliders, de-greyed labels,
no leftover tab-pipe/alpha) · ListBox-aligned DataGrid (matching bordered surface + base-accent
selection) · data-driven demo with Home / Playground / Accents + control pages + Settings.

### What's ported from the WinUI repos (recheck)

Every control theme in microsoft-ui-xaml's
[`CommonStyles`](https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles)
is a `*_themeresources.xaml` whose keys Avalonia's FluentTheme already consumes, so porting is just
remapping those keys to our tokens. Done in full for **Button**, and color-mapped for **CheckBox /
RadioButton / ToggleSwitch / Slider / ToggleButton**. Everything else (ComboBox, TextBox, ListView,
Expander, Flyout, ScrollBar, Pivot/TabView, CalendarView, …) currently inherits Fluent styling via
Avalonia's matching templates plus our token layer; each can be made pixel-exact by the same
mechanical remap into `Controls/ControlColors.axaml`.

**DataGrid is not a WinUI control** — it isn't in microsoft-ui-xaml (Microsoft ships it via the
Windows Community Toolkit). Avalonia provides it as the separate `Avalonia.Controls.DataGrid`
package (latest 12.0.0), which the demo references and themes through our Fluent 2 tokens; see the
**Collections** page. (It needs the `ControlAltFillColor*` tokens present, which the theme now
defines, otherwise its theme throws a `Color`→`IBrush` cast on Avalonia 12.0.4.)

### Controls in Avalonia but not in WinUI

Avalonia has a few controls with no direct WinUI 3 counterpart; they still pick up our Fluent
styling automatically. Closest WinUI analogue in parentheses:

- **Carousel** (≈ FlipView) — *note: animating its page transition currently trips an Avalonia
  12.0.4 `Color`→`IBrush` bug, so it's omitted from the demo.*
- **TransitioningContentControl** (≈ implicit page transitions) — same caveat.
- **TabStrip** (≈ Pivot headers) · **ButtonSpinner** (≈ NumberBox spinner) ·
  **AutoCompleteBox** (≈ AutoSuggestBox) · **NumericUpDown** (≈ NumberBox) ·
  **MaskedTextBox** (no WinUI equivalent) · **TreeDataGrid** (Avalonia-specific).

**Not yet:** full template ports for every control · real acrylic blur on flyouts (currently the
solid acrylic fallback) · a high-contrast `SystemColor*` theme dictionary · per-control source-code
extraction in the demo.

## Credits

Every third-party project this solution bundles, depends on, or references, with its license:

| Asset | Used for | License |
| --- | --- | --- |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) + `FluentTheme` | The UI framework this theme builds on | MIT |
| [DejaVu Sans](https://dejavu-fonts.github.io/) | Bundled UI **text** font — covers Latin plus the arrows / geometric / box-drawing glyphs (`→ ● ▼ │`) on every platform | Free (Bitstream Vera / DejaVu license) |
| [Codicons](https://github.com/microsoft/vscode-codicons) | Bundled **icon** font — the UI's symbol glyphs (`SymbolThemeFontFamily`: window buttons, chevrons, nav icons) | MIT (© Microsoft) |
| [Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia) | Renders the Home (`README.md`) and Custom (`CUSTOM.md`) pages | MIT |
| [Avalonia.Labs](https://github.com/AvaloniaUI/Avalonia.Labs) | `InfoBadge` control on the Status & info page | MIT |
| [Avalonia.Controls.DataGrid](https://github.com/AvaloniaUI/Avalonia) / [.ColorPicker](https://github.com/AvaloniaUI/Avalonia) | DataGrid and ColorView / ColorSpectrum | MIT |
| [Avalonia.Markup.Xaml.Loader](https://github.com/AvaloniaUI/Avalonia) | Compiles the **Playground** page's live XAML at runtime | MIT |
| [Microsoft.Win32.Registry](https://www.nuget.org/packages/Microsoft.Win32.Registry) | Reading the Windows accent colour from the registry | MIT |
| [Material Design Icons](https://pictogrammers.com/library/mdi/) | The *code-array* glyph in the app icon | Apache 2.0 |
| [List-of-US-States](https://github.com/jasonong/List-of-US-States) | Sample data for the Collections `ListBox` | Public data |
| [ohana-api](https://github.com/codeforamerica/ohana-api) sample addresses | Sample data for the Collections `DataGrid` | Code for America |
| [Metro color palette](https://www.w3schools.com/colors/colors_metro.asp) | The 20 accent preset hues | Reference |

## License

[MIT](LICENSE). Ported color values and resource structures originate from the MIT-licensed
[microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml) and
[WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery) projects, © Microsoft Corporation.
