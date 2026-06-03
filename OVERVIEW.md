# Fluid.Avalonia — overview & deep dive

The architecture, the demo-app internals, and the coverage/roadmap matrix — the longer-form companion to the [README](README.md).

## How it works

The project is built on one key observation:

> **Avalonia's `FluentTheme` and WinUI use the *same resource-key names*.** Avalonia's Fluent control templates were derived from the WinUI control set, so a CheckBox in Avalonia resolves brushes named `CheckBoxCheckBackgroundFillChecked`, `ToggleSwitchFillOn`, `ButtonBackgroundPointerOver`, etc. — the exact keys WinUI's `*_themeresources.xaml` files define.

That means we don't have to re-template every control. We override the **resource layer** with authentic Fluent 2 values, and Avalonia's existing templates render them.

### Architecture (the `Fluid.Avalonia` library)

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
1 · Resolve the accent — first match wins
      manual override  →  SetAccent(color)                ← preset palette · ColorPicker
      Windows          →  registry AccentPalette (7 shades)
      macOS            →  AppleAccentColor
      Linux            →  GNOME accent-color · KDE kdeglobals
      Avalonia         →  PlatformSettings.AccentColor1
      fallback         →  neutral Fluent blue
                        │
                        ▼
2 · AccentColorService  —  single color → 6 shades (FluentTheme HSL steps),
                           re-resolved live on ColorValuesChanged
                        │
                        ▼
3 · Application.Resources  —  SystemAccentColor  +  Light1–3 / Dark1–3
                        │
                        ▼   every accent brush reads these via DynamicResource
   Consumers  —  AccentFillColorDefaultBrush · ToggleSwitchFillOn ·
                 CheckBoxCheckBackgroundFillChecked · accent Button · …
```

Because every accent brush is a `DynamicResource`, changing the OS accent, picking a preset, or toggling light/dark re-resolves the whole UI with no restart.

## The demo app (`Fluid.Avalonia.Demo`)

A WinUI 3 Gallery-style shell, using the Gallery's **data-driven** structure:

- **Custom title bar** — the client area is extended into the title bar (Avalonia 12 `ExtendClientAreaToDecorationsHint` + the `WindowDecorationProperties.ElementRole="TitleBar"` chrome role, the technique used by [SukiUI](https://github.com/kikipoulet/SukiUI)). It hosts a working hamburger (toggles the pane), the app icon, the title, a centered search box, and custom minimize / maximize / close buttons — all over the Mica backdrop.
- **Reaching the bottom of a page** — three things conspired to clip the last item, all fixed: the content inset lives on the scrolled content's `Margin`, **not** `ScrollViewer.Padding` (Avalonia subtracts that padding from the scrollable extent, leaving the last padding-worth of content unreachable); the window starts **centered** at a height that fits the work area; and the root is inset by the window's `OffScreenMargin` so a maximized, extended-client-area window doesn't run under the taskbar.
- **Functional search** — an `AutoCompleteBox` in the title bar bound to the catalog; picking a suggestion navigates to that page.
- **`Models/GalleryCatalog.cs`** — the catalog of navigable entries (title, description, icon glyph, page factory). This is the equivalent of the Gallery's `ControlInfoDataSource`.
- **NavigationView** — a `SplitView` whose item list is bound to the catalog, with Codicon glyphs, an accent selection pill, and Settings pinned to the bottom.
- **`Views/ItemPage`** — the standard host for a catalog entry: a title + description header above the entry's content (the Gallery's `ItemPage`).
- **`Controls/ControlExample`** — the Gallery's example card: description, live example, an optional options pane, and a collapsible **Source code** footer.
- **Pages** under `Views/Pages/`:
  - **Home** — renders the `README.md` live at runtime with [Markdown.Avalonia](https://github.com/whistyun/Markdown.Avalonia) (the file is bundled as an Avalonia resource and loaded via `AssetLoader`, not hardcoded).
  - **Custom** — renders [`CUSTOM.md`](CUSTOM.md) the same way: a catalogue of every non-vanilla control and theme/shell feature authored for this project.
  - **Playground** — a live XAML sandbox (in the spirit of SukiUI's Playground): type Avalonia markup in an editor and see it rendered instantly in a preview pane, themed by Fluid.Avalonia, with a graceful parse-error panel. Rendered via `AvaloniaRuntimeXamlLoader` (`Avalonia.Markup.Xaml.Loader`).
  - **Accents** — try the live system accent, pick from the outlined Metro-inspired preset palette (20 swatches), or set any color under Fluent-styled **Color Space** (spectrum + hue) / **Sliders** tabs; mode checkboxes pick the active source and a live preview re-tints instantly.
  - Basic input, Collections, Date & time, Dialogs & flyouts, Layout, Media, Menus & toolbars, Navigation, Scrolling, Status & info, Text — plus a **Settings** page with a light/dark/system theme switch (which also has toggles to show/hide the title-bar menu button, icon and title). Between them the demo exercises the full Avalonia control set, including `ToggleSplitButton`, `NumericUpDown`, `ButtonSpinner`, `Label`, `ColorPicker` / `ColorSpectrum`, `ContextMenu`, `TabStrip`, `Carousel` + `PipsPager`, `HeaderedContentControl`, `PathIcon` and an `AutoCompleteBox` (autocomplete over country names) — plus a few demo-only composites: a `BusyArea` overlay (ring / bar / circular indicators), a `FluentColorPicker` dropdown, a `LabeledTextInput`, a `BinarySelector` (an AM/PM-style two-value segmented selector — both options shown, an accent pill slides to the active one, and `Value` returns the chosen object — on the Basic input page), a `RadialTimePicker` (an Actipro-inspired, Fluent-styled concentric dual-ring time slider — inner ring = hour, outer ring = minute, each a Fluent slider bent into a circle — in 12-hour and `Is24Hour` Material modes — factored into a reusable `RadialClock` dial), a `DateTimePicker` (a segmented date + time field that opens looping spinner columns — the reusable `DateTimeSpinners` — 12- and 24-hour) and a `AnalogDateTimePicker` (the same field, but its dropdown pairs a `Calendar` with that radial dial, 12- and 24-hour) — all on the Date & time page (the spinner columns and the radial+Calendar combo are also shown inline there). The dial's ring geometry is factored into a shared `RadialDial` helper that powers two more controls: a `RadialSlider` (a circular slider, on the Basic input page) and a `ProgressCircle` (a determinate radial progress ring, on the Status & info page). Plus a couple of controls ported and re-themed to Fluent 2: a `BreadcrumbBar` (from WinUI / FluentAvalonia) and a `GroupBox` titled card (from SukiUI), both catalogued on the demo's **Custom** page. The Status & info page also shows the `InfoBadge` control from the [Avalonia.Labs](https://github.com/AvaloniaUI/Avalonia.Labs) package (a dot / number status capsule with severity classes) rather than a hand-rolled one.
- A Mica window. The custom title bar uses `SystemDecorations="BorderOnly"` so Avalonia's own drawn title/caption buttons are suppressed (no duplicated title) while the resize border is kept.
- **Localization** done the way [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia) / [SukiUI](https://github.com/kikipoulet/SukiUI) do it — and, like them, it lives **in the `Fluid.Avalonia` theme library itself** (`Fluid.Avalonia/Locale/<code>.axaml`: en / pl / de / fr / es): per-culture `ResourceDictionary` files of keyed `<x:String>`, merged and swapped at runtime by `Fluid.Avalonia.Locale.LocaleManager`. **Pure Avalonia, no localization package.** The custom pickers' OK / Cancel / Reset labels and their field placeholders switch language live — and so do several **native** Avalonia controls, by overriding Avalonia's own theme string keys: the `DatePicker` / `TimePicker` field placeholders (`StringDatePicker*Text` / `StringTimePicker*Text`) and the `TextBox` right-click Cut / Copy / Paste menu (`StringTextFlyout*Text`), which its templates read via `{DynamicResource}`. Defaults to the system language (falling back to English); change it in code (`LocaleManager.SetLanguage`) or from **Settings → Language** — a radio group of native language names, styled like the theme picker. (The Settings page chrome itself is plain English.)
- **App icon** — an original mark (`Assets/logo.svg` → `logo.png` / `logo.ico`): a blue rounded square with the [Material Design Icons](https://pictogrammers.com/library/mdi/icon/code-array/) *code-array* brackets in white. The `.ico` is a crisp multi-size icon (16–256 px) whose frames are each rendered natively (anti-aliased) rather than down-scaled from one bitmap, and the title-bar `Image` downsamples with `BitmapInterpolationMode=HighQuality`, so it stays sharp at every size.

## Coverage & roadmap

**Done:** Fluent 2 token system (light/dark) · cross-platform accent (Windows 7-shade / macOS / Linux, live) + Metro presets + manual `ColorPicker` override · bundled DejaVu Sans text + Codicons glyphs (cross-platform) · 4/8 corner radii · lit-edge borders · elevation shadows · Mica + theme-following title bar · full Button template port · color-accurate CheckBox / RadioButton / ToggleSwitch / Slider / ToggleButton · SukiUI-style thin overlay scrollbar · Semi-style (opaque) Calendar · audited ColorView (clean ColorSpectrum + hue Color Space tab; Sliders tab with flat Fluent channel sliders, de-greyed labels, no leftover tab-pipe/alpha) · ListBox-aligned DataGrid (matching bordered surface + base-accent selection) · data-driven demo with Home / Playground / Accents + control pages + Settings.

### What's ported from the WinUI repos (recheck)

Every control theme in microsoft-ui-xaml's [`CommonStyles`](https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles) is a `*_themeresources.xaml` whose keys Avalonia's FluentTheme already consumes, so porting is just remapping those keys to our tokens. Done in full for **Button**, and color-mapped for **CheckBox / RadioButton / ToggleSwitch / Slider / ToggleButton**. Everything else (ComboBox, TextBox, ListView, Expander, Flyout, ScrollBar, Pivot/TabView, CalendarView, …) currently inherits Fluent styling via Avalonia's matching templates plus our token layer; each can be made pixel-exact by the same mechanical remap into `Controls/ControlColors.axaml`.

**DataGrid is not a WinUI control** — it isn't in microsoft-ui-xaml (Microsoft ships it via the Windows Community Toolkit). Avalonia provides it as the separate `Avalonia.Controls.DataGrid` package (latest 12.0.0), which the demo references and themes through our Fluent 2 tokens; see the **Collections** page. (It needs the `ControlAltFillColor*` tokens present, which the theme now defines, otherwise its theme throws a `Color`→`IBrush` cast on Avalonia 12.0.4.)

### Controls in Avalonia but not in WinUI

Avalonia has a few controls with no direct WinUI 3 counterpart; they still pick up our Fluent styling automatically. Closest WinUI analogue in parentheses:

- **Carousel** (≈ FlipView) — *note: animating its page transition currently trips an Avalonia 12.0.4 `Color`→`IBrush` bug, so it's omitted from the demo.*
- **TransitioningContentControl** (≈ implicit page transitions) — same caveat.
- **TabStrip** (≈ Pivot headers) · **ButtonSpinner** (≈ NumberBox spinner) · **AutoCompleteBox** (≈ AutoSuggestBox) · **NumericUpDown** (≈ NumberBox) · **MaskedTextBox** (no WinUI equivalent) · **TreeDataGrid** (Avalonia-specific).

**Not yet:** full template ports for every control · real acrylic blur on flyouts (currently the solid acrylic fallback) · a high-contrast `SystemColor*` theme dictionary · per-control source-code extraction in the demo.
