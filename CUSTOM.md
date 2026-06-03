# Custom controls

Everything below is built on top of stock Avalonia and its `FluentTheme` — composite controls and shell features authored for this project that have **no direct equivalent in vanilla Avalonia**. The standard controls shown on the other pages are Avalonia's own, only re-themed.

## Controls

### FluentColorPicker

A Fluent-styled colour dropdown — a swatch + hex button opening a flyout with glyph tabs (Color space · Preset · Sliders), Metro preset swatches and a shared previewer. The stock `ColorPicker` is not re-templatable to this look.

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

A SukiUI-style busy overlay: wrap any content and toggle `IsBusy` to dim it behind a scrim with a progress indicator (ring or bar), an optional label, and an adjustable scrim opacity.

### LabeledTextInput

A small composite that pairs a caption label with a text field, styled to match the Fluent `ColorView` inputs for consistent option panes.

### BinarySelector

A two-value segmented selector: both options show inside the track and an accent pill slides to the active one (the active label sits on the pill in white, the other dims). It **returns an object** — `Value` equals `LeftValue` when the left side is active and `RightValue` when the right is (two-way). `LeftContent` / `RightContent` override the displayed labels; left unset, each side shows its value (so `LeftValue="AM" RightValue="PM"` is enough). Fluent-aligned (app-accent pill, neutral track, Fluent text tokens). Built as a `ToggleButton` subclass — clicking a side selects it, the keyboard flips — so it keeps `IsChecked` (false = left, true = right). A compact variant is reused inside the `RadialClock` dial as its AM/PM picker. (Inspired by the dribbble "AM/PM" sliding-segment pattern.)

### BreadcrumbBar

A horizontal trail of crumbs joined by chevrons; the final crumb is the current location (emphasised, inert) while earlier crumbs are clickable and raise `ItemClicked` for back-navigation. Ported from WinUI / FluentAvalonia.

### GroupBox

A titled container — a header strip above a padded body, wrapped in a Fluent card border. Inspired by SukiUI's `GroupBox` (and the classic WPF `GroupBox` that Avalonia lacks).

## Theme & shell

### Fluid.Avalonia theme

A WinUI 3 / Fluent 2 look layered over `FluentTheme`: light/dark Fluent 2 colour tokens, lit-edge elevation borders, card surfaces, and a Fluent-tuned set of control themes (`NumericUpDown` / `ButtonSpinner`, `DataGrid`, `Calendar`, and more). The theme is **font-agnostic** — the host app supplies the typeface; the demo bundles **DejaVu Sans** for text (broad coverage: Latin plus `→ ● ▼ │`) and the **Codicons** font (VS Code, MIT) for symbol glyphs, so type and icons render identically on desktop and in the browser — no reliance on the Windows-only Segoe fonts.

### OS accent integration

An accent-colour service that reads the Windows accent from the registry (with cross-platform fallbacks), plus a curated palette of 20 Metro presets and an API to apply a custom accent — wired into the Accents page and the colour picker.

### Localization

Runtime localization the **Semi.Avalonia / SukiUI** way, shipped **inside the `Fluid.Avalonia` theme library itself** (`Fluid.Avalonia/Locale/<code>.axaml`, currently en / pl / de / fr / es) — per-culture `ResourceDictionary` files of keyed `<x:String>` entries that `Fluid.Avalonia.Locale.LocaleManager` merges into the app and swaps at runtime, so the theme carries its own translations. The picker buttons (OK / Cancel / Reset) use `{DynamicResource STRING_*}` (so they update live on switch); the segmented field's placeholders are painted in code, so they re-read on a `LanguageChanged` event. Several **native** Avalonia controls localize too — the locale dictionaries also override Avalonia's own theme string keys: the `DatePicker` / `TimePicker` field placeholders (`StringDatePicker*Text` / `StringTimePicker*Text`) and the `TextBox` right-click Cut / Copy / Paste menu (`StringTextFlyout*Text`), which Avalonia's templates read via `{DynamicResource}`, so they resolve to our translations and update live. (The Settings page chrome is plain English — only the pickers are translated.) The app defaults to the **system language** (falling back to English) and can be changed in code (`LocaleManager.SetLanguage("de")`) or from the **Settings → Language** picker (a radio group of native language names, styled like the theme picker above it). Pure Avalonia — no third-party localization package; translations are kept to short labels (no prose).

### Window shell

A custom extended-client-area title bar (hamburger, app icon, title, window buttons) with a Mica backdrop and dark title bar, and a data-driven `NavigationView` rail with grouped, separated sections.

### Thin Fluent scrollbar

An original, SukiUI-inspired slim scrollbar theme that expands on hover, replacing Avalonia's default chrome.

### Playground

A live XAML sandbox page — type Avalonia markup and see it rendered instantly, themed by Fluid.Avalonia.
