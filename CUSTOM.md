# Custom controls

Everything below is built on top of stock Avalonia and its `FluentTheme` — composite controls and shell features authored for this project that have **no direct equivalent in vanilla Avalonia**. The standard controls shown on the other pages are Avalonia's own, only re-themed.

## Controls

### FluidColorPicker

A Fluid-styled colour dropdown — a swatch + hex button opening a flyout with glyph tabs (Color space · Preset · Sliders), Open Color preset swatches and a shared previewer. The stock `ColorPicker` is not re-templatable to this look. The flyout body is the reusable **`FluidColorEditor`** surface — any flyout can host the exact same editors (the SignaturePad's "+ add ink" flyout does), with live colour via its two-way `Color` or commit semantics via the host's own OK/Add button.

### RadialTimePicker

A concentric dual-ring time picker (Actipro-inspired, Fluent-styled): the **inner** ring is the hour, the **outer** ring the minute, with a clock face and a subtle backing disc. Each ring is effectively a Fluent slider bent into a circle — a thin rail, an accent fill from 12 o'clock, and a draggable thumb; the fill and thumb share the stock Slider brushes and move through default → hover → pressed → release together (rendered opaque so the disc never shows through). `Is24Hour` switches the hour ring to the Material 24-hour layout (0–23 at 15°/hour, even hours labelled, 0 at top, no AM/PM). The time readout sits centred inside the inner ring, with AM/PM picked by a compact **`BinarySelector`** (hidden in 24-hour mode). Exposes a single two-way `Time` value. The dial itself is the reusable **`RadialClock`** control — the picker hosts it in a flyout, but you can drop it on a page directly for an inline time picker — and its ring geometry / rendering lives in a shared `RadialDial` helper that also powers `RadialSlider` and `ProgressCircle`.

### DateTimePicker

A merged date + time field — a native-picker-style segmented box (month / day / year · hour : minute · AM-PM) that opens a flyout of **looping spinner columns** (built on Avalonia's `DateTimePickerPanel`, the same primitive the native pickers use). OK commits the combined value to `SelectedDateTime`; Cancel discards; Reset returns the time to a default. `ClockIdentifier` switches between 12-hour (with an AM/PM column) and 24-hour. The columns themselves are the reusable **`DateTimeSpinners`** control — the picker hosts them in its flyout, but you can drop them on a page directly for an inline spinner date + time picker.

### AnalogDateTimePicker

A second take on the merged date + time field whose dropdown pairs a **Calendar** (for the date) with the **RadialClock** dial (for the time), side by side — a month grid next to an analog clock face, rather than the digital spinner wheels of `DateTimePicker`. The same segmented box (month / day / year · hour : minute · AM-PM) opens the dropdown; OK commits the picked day + dial time to `SelectedDateTime`, Cancel discards, Reset returns the time to a default. `ClockIdentifier` switches the dial and field between 12-hour (with AM/PM) and 24-hour. It reuses the shared TimePicker-style field and hosts the same `RadialClock` as `RadialTimePicker`, so the dial is identical across both.

### RadialSlider

A circular slider built on the same `RadialDial` geometry: a single ring (a Fluent slider bent into a circle) whose accent fill and draggable thumb sweep clockwise from 12 o'clock to set a two-way `Value` between `Minimum` and `Maximum`, with the value shown in the centre. Rail, fill and thumb take the stock Slider brushes through default → hover → pressed, exactly like the time dial.

### ProgressCircle

A determinate radial progress indicator sharing that same ring rendering: a track ring with an accent fill arc that sweeps from 12 o'clock to show `Value` between `Minimum` and `Maximum`, with the percentage in the centre. Non-interactive, and filled with the app accent rather than the Slider thumb brush.

### BusyArea

A SukiUI-style busy overlay: wrap any content and toggle `IsBusy` to dim it behind a scrim with a progress indicator (ring, bar or circular), an optional label, and an adjustable scrim opacity.

### LabeledTextInput

A small composite that pairs a caption label with a text field, styled to match the Fluent `ColorView` inputs for consistent option panes.

### BinarySelector

A two-value segmented selector: both options show inside the track and an accent pill slides to the active one (the active label sits on the pill in white, the other dims). It **returns an object** — `Value` equals `LeftValue` when the left side is active and `RightValue` when the right is (two-way). `LeftContent` / `RightContent` override the displayed labels; left unset, each side shows its value (so `LeftValue="AM" RightValue="PM"` is enough). Fluent-aligned (app-accent pill, neutral track, Fluent text tokens). Built as a `ToggleButton` subclass — clicking a side selects it, the keyboard flips — so it keeps `IsChecked` (false = left, true = right). A compact variant is reused inside the `RadialClock` dial as its AM/PM picker. (Inspired by the dribbble "AM/PM" sliding-segment pattern.)

### BreadcrumbBar

A horizontal trail of crumbs joined by chevrons — reimplemented after **WPF-UI**'s `BreadcrumbBar` (a clickable trail where the final crumb is the emphasised current location and earlier crumbs raise `ItemClicked` — and run an optional `Command` — for back-navigation; crumbs may carry an `Icon`), and extended in the spirit of **Dirkster99**'s directory-picker breadcrumb: set a `ChildrenSelector` and each chevron *between* crumbs becomes a dropdown of that crumb's children, with the pick raising `ChildSelected`. When the trail is wider than the control it collapses WinUI-style — the leading crumbs fold behind a clickable `…` overflow chip that drops them down — and a leaf crumb (a file, or an empty folder) shows no chevron. The Navigation page shows both a width-limited string trail (demonstrating the overflow collapse plus back / forward navigation) and a directory picker seeded from the working directory (the real file system on desktop, an in-memory mock tree in the browser).

### GroupBox

A titled container — a header strip above a padded body, wrapped in a Fluent card border. Inspired by SukiUI's `GroupBox` (and the classic WPF `GroupBox` that Avalonia lacks).

### InfoBar

A status banner — a severity icon, a SemiBold `Title` over a wrapping `Message`, optional action content (the inherited `Content` slot), and a dismiss button. Reimplemented after **WPF-UI**'s `InfoBar`: five properties (`Title`, `Message`, `Severity`, `IsOpen`, `IsClosable`) where `Severity` (Informational / Success / Warning / Error) selects the Codicon glyph, the icon colour and a gentle `SystemFillColor*Background` surface tint — while the border stays one neutral hairline for every severity, exactly like WinUI 3's `InfoBarBorderBrush` (colour lives only in the tint and the icon, never a saturated perimeter). Dismissal is purely `IsOpen`-driven (the close button sets `IsOpen=false`, after raising a bubbling `CloseButtonClick` event — a Fluid addition WPF-UI doesn't have). The Status & info page shows one bar per severity, a non-closable pair, and an error bar carrying a "Retry" action button in its content slot.

### VisualRate

A row of clickable glyphs setting a 0..`Count` value. Generalised from a port of Avalonia's official **Avalonia.Samples** `RatingControlSample`: a `TemplatedControl` whose `PART_ItemsPresenter` `ItemsControl` renders one Codicon glyph (a `TextBlock` in the symbol font) per ordinal, each lit when its ordinal is `<= Value`; clicking one sets the two-way `Value`, and clicking the topmost lit glyph again unselects it (so clicking the only lit glyph clears the value to 0). Where the sample hard-coded a star `Path`, a single `Glyph` string picks which Codicon icon to use (default the filled `star-full`). Re-themed to Fluent — because one filled glyph is only recoloured between states, an unlit glyph reads as a solid neutral `ControlStrongStrokeColorDefaultBrush` shape (interior matching its edge), a lit one is an accent shade, and hover shows the next-lighter accent shade. Shown on the Basic input page with a live value readout, an adjustable count and a glyph picker.

### ContentDialog (DialogHost)

A modal dialog shown over a dimmed surface, using the **DialogHost.Avalonia** package re-themed to Fluent 2. One root `DialogHost` (`Identifier="RootDialog"`) wraps the shell; `await DialogHost.Show(content, "RootDialog")` opens it and resolves to the result the dialog's buttons pass back. The Fluent look is a local restyle (`Styles/DialogHost.axaml`): an opaque `SolidBackgroundFillColorBaseBrush` surface (the translucent card fills are meant to sit over Mica and would let the page bleed through a modal), an 8 px overlay corner radius, a soft elevation shadow, a `SurfaceStrokeColorDefaultBrush` hairline, and a smoke overlay. The dialog is an in-window overlay (no native popup), so the same code runs on desktop and in the browser. Shown on the Dialogs & flyouts page alongside the existing faux-surface example.

### SignaturePad

A freehand signature pad with a natural, velocity-driven variable-width pen — ported from **[warting/android-signaturepad](https://github.com/warting/android-signaturepad)** (MIT), itself based on gcacace/android-signaturepad (Apache-2.0). As you draw, each stroke is smoothed into cubic Béziers and the nib width follows pointer velocity — faster → thinner, slower → thicker (`strokeWidth = max(MaxStrokeWidth / (velocity + 1), MinStrokeWidth)`, with the upstream `0.9` velocity-filter smoothing) — stamping round dabs along the curve so the ink reads calligraphic rather than a constant-width line; a tap leaves a single dot. It exposes `StrokeColor`, `MinStrokeWidth` / `MaxStrokeWidth` and `VelocityFilterWeight`; `Clear()` wipes it and `IsEmpty` reports whether anything is drawn (each stroke keeps the colour it was drawn with). It derives from a plain `Control` and paints the surface, hairline and ink straight onto the `DrawingContext` in `Render` — rather than the demo's usual Canvas-of-Shapes — so a long signature (thousands of dabs) stays cheap, and the drawn fill is what makes the surface hit-testable. The Basic input page wraps it with a clear `×`, a row of ink swatches (named entries from `AccentService.Preset`, drawn in the house rounded-square swatch style with an accent selection ring) plus a `+` chip whose flyout hosts the shared `FluidColorEditor` behind a Cancel / Add footer — Add appends the picked colour to the row as a new selectable swatch — and a 0–6 nib-size slider.

## Theme & shell

### Fluid.Avalonia theme

A WinUI 3 / Fluent 2 look layered over `FluentTheme`: light/dark Fluent 2 colour tokens, lit-edge elevation borders, card surfaces, and a Fluent-tuned set of control themes (`NumericUpDown` / `ButtonSpinner`, `DataGrid`, `Calendar`, and more). The theme is **font-agnostic** — the host app supplies the typeface; the demo bundles **DejaVu Sans** for text (broad coverage: Latin plus `→ ● ▼ │`) and the **Codicons** font (VS Code, CC-BY-4.0) for symbol glyphs, so type and icons render identically on desktop and in the browser — no reliance on the Windows-only Segoe fonts.

### OS accent integration

An accent-colour service that reads the Windows accent from the registry (with cross-platform fallbacks), plus a curated palette of 20 Open Color presets and an API to apply a custom accent — wired into the Accents page and the colour picker. `UseSystemAccent()` reverts to the live OS accent and never throws — it falls back to a neutral blue on an unsupported platform — while two overloads let you instead throw a `PlatformNotSupportedException` (`UseSystemAccent(throwIfUnsupported: true)`) or supply your own fallback colour (`UseSystemAccent(Color)`).

### Localization

Runtime localization the **Semi.Avalonia / SukiUI** way, shipped **inside the `Fluid.Avalonia` theme library itself** (`Fluid.Avalonia/Locale/<code>.axaml`, currently en / pl / de / fr / es) — per-culture `ResourceDictionary` files of keyed `<x:String>` entries that `Fluid.Avalonia.Locale.LocaleManager` merges into the app and swaps at runtime, so the theme carries its own translations. The picker buttons (OK / Cancel / Reset) use `{DynamicResource STRING_*}` (so they update live on switch); the segmented field's placeholders are painted in code, so they re-read on a `LanguageChanged` event. Several **native** Avalonia controls localize too — the locale dictionaries also override Avalonia's own theme string keys: the `DatePicker` / `TimePicker` field placeholders (`StringDatePicker*Text` / `StringTimePicker*Text`) and the `TextBox` right-click Cut / Copy / Paste menu (`StringTextFlyout*Text`), which Avalonia's templates read via `{DynamicResource}`, so they resolve to our translations and update live. (The Settings page chrome is plain English — only the pickers are translated.) The app defaults to the **system language** (falling back to English) and can be changed in code (`LocaleManager.SetLanguage("de")`) or from the **Settings → Language** picker (a radio group of native language names, styled like the theme picker above it). Pure Avalonia — no third-party localization package; translations are kept to short labels (no prose).

### FluidWindow

A reusable `Window` subclass that packages the WinUI 3 window chrome — an extended client area with a custom title bar (app icon, title, a free `TitleBarContent` slot, and minimize / maximize / close caption buttons), a cross-platform translucent backdrop with a solid fallback, and a frame that follows the light/dark theme (DWM). The backdrop is driven by the shared `TransparencyService` and gated by a `TransparencyEnabled` property (Mica on Windows, vibrancy on macOS, a KWin blur on KDE/Linux; seeded from the Windows "Transparency effects" setting at construction and reconciled to a solid surface via `ActualTransparencyLevel` wherever the backdrop can't be rendered — non-KWin Linux desktops get the solid surface rather than a blur-less transparent window). Window drag and double-tap-maximize are built in; caption glyphs are vector, so it needs no symbol font; and the Windows-specific bits are guarded so it degrades gracefully off-Windows. It also seeds the OS light/dark variant synchronously at startup via the shared `SystemTheme` helper, to avoid the Linux "flash of light theme" (Avalonia resolves the freedesktop `color-scheme` portal asynchronously on X11, so a follow-OS window otherwise paints one light frame before flipping to dark). Put your content in `Content` and any title-bar widgets (search, menus…) in `TitleBarContent`.

### Window shell

A custom extended-client-area title bar (hamburger, app icon, title, window buttons) with a choice of window backdrop from **Settings → Window** — **System glass** (the OS effect: Mica on Windows / vibrancy on macOS / KWin blur on KDE, via `TransparencyService`, following the Windows transparency setting), **Acrylic** (a cross-platform, software-rendered frosted backdrop via the `Fluid.Avalonia.Acrylic` package's SkiaSharp shader — available even where the OS has no Mica/blur), or **Solid** — a dark title bar, and a data-driven `NavigationView` rail with grouped, separated sections.

### System tray menu

A desktop-only system-tray icon, wired up by the demo's `App` on the classic-desktop lifetime (so the WASM head is unaffected), whose context menu mirrors the shell navigation — Home / Custom / Playground, a **Controls** submenu of the gallery pages, then Settings / Show / Close. On **Windows**, Avalonia renders the tray's `NativeMenu` itself (the same `MenuFlyoutPresenter` as in-app menus), so it inherits the Fluid.Avalonia theme — a Fluent-styled tray menu in the spirit of WPF-UI's `NotifyIcon`; on macOS / Linux it falls back to the OS-native menu.

### Thin Fluent scrollbar

An original, SukiUI-inspired slim scrollbar theme that expands on hover, replacing Avalonia's default chrome.

### Playground

A live XAML sandbox page — type Avalonia markup and see it rendered instantly, themed by Fluid.Avalonia.
