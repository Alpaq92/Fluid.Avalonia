# Changelog

## [1.7.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.6.4...v1.7.0) (2026-06-16)


### Features

* block the transparency toggle where no backdrop is possible ([bb17d70](https://github.com/Alpaq92/Fluid.Avalonia/commit/bb17d70e1d5187de5d8e3a07e13268a6e7dc21e0))

## [1.6.4](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.6.3...v1.6.4) (2026-06-16)


### Bug Fixes

* bind the window background to the theme resource so it can't go stale ([08d985c](https://github.com/Alpaq92/Fluid.Avalonia/commit/08d985cc429ae58f0aa45caede7a2e29eb26cad8))

## [1.6.3](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.6.2...v1.6.3) (2026-06-16)


### Bug Fixes

* re-resolve the window background when the theme variant changes ([f9d3a0b](https://github.com/Alpaq92/Fluid.Avalonia/commit/f9d3a0b382f3f1c7a57772219735def4bbe70f9c))

## [1.6.2](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.6.1...v1.6.2) (2026-06-12)


### Bug Fixes

* **demo:** give the SignaturePad a distinct Open Color signing-surface tone ([8dfdf35](https://github.com/Alpaq92/Fluid.Avalonia/commit/8dfdf358991edd8631885ff3799e9bed89eb80ba))

## [1.6.1](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.6.0...v1.6.1) (2026-06-12)


### Bug Fixes

* **demo:** address deep-review findings on the SignaturePad batch ([6260dab](https://github.com/Alpaq92/Fluid.Avalonia/commit/6260dab289ab480a37edc8938e1f0c9c1c5cdc72))

## [1.6.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.5.2...v1.6.0) (2026-06-12)


### Features

* **demo:** SignaturePad control with a Fluid custom-ink picker ([af9b144](https://github.com/Alpaq92/Fluid.Avalonia/commit/af9b1445f2dca79273497b158f62d44f20e19cca))


### Bug Fixes

* keep non-KWin Linux and pre-API-31 Android windows opaque - TransparencyService ([af9b144](https://github.com/Alpaq92/Fluid.Avalonia/commit/af9b1445f2dca79273497b158f62d44f20e19cca))

## [1.5.2](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.5.1...v1.5.2) (2026-06-11)


### Bug Fixes

* **demo:** add a favicon to the WASM live demo ([f294de8](https://github.com/Alpaq92/Fluid.Avalonia/commit/f294de847afe22677fdacc8e211b31d360055b13))

## [1.5.1](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.5.0...v1.5.1) (2026-06-11)


### Bug Fixes

* keep the WASM live demo from getting stuck on the splash after a redeploy ([4da462f](https://github.com/Alpaq92/Fluid.Avalonia/commit/4da462f204979e7d0a4d39fc8a817449d31d55df))

## [1.5.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.4.0...v1.5.0) (2026-06-10)


### Features

* cross-platform window transparency toggle (TransparencyService + FluidWindow.TransparencyEnabled) ([a90d2d8](https://github.com/Alpaq92/Fluid.Avalonia/commit/a90d2d83e3051465761d173d71fff4c3cca8da73))

## [1.4.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.3.0...v1.4.0) (2026-06-10)


### Features

* InfoBar, ContentDialog (DialogHost.Avalonia), VisualRate, live AccentChanged event ([53fae62](https://github.com/Alpaq92/Fluid.Avalonia/commit/53fae622656a9ce0028cd1204c5aa9a38164bb85))

## [1.3.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.2.1...v1.3.0) (2026-06-09)


### Features

* BreadcrumbBar (directory picker + overflow collapse), system-tray menu, UseSystemAccent overloads ([ec2b81a](https://github.com/Alpaq92/Fluid.Avalonia/commit/ec2b81a8ef9d413578f9c531de606b6d06b95eb7))

## [1.2.1](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.2.0...v1.2.1) (2026-06-09)


### Refactoring

* rename AccentColorService to AccentService (Presets -&gt; Preset) ([a812dd3](https://github.com/Alpaq92/Fluid.Avalonia/commit/a812dd353e4c135a8d1ad8490a2952b402b6db83))

## [1.2.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.1.1...v1.2.0) (2026-06-09)


### Features

* switch accent presets to the Open Color palette ([f505e52](https://github.com/Alpaq92/Fluid.Avalonia/commit/f505e527398de1deef053ab09b5d2fbf7d84ec43))

## [1.1.1](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.1.0...v1.1.1) (2026-06-04)


### Documentation

* clarify the modal-window example and note it in OVERVIEW ([e986204](https://github.com/Alpaq92/Fluid.Avalonia/commit/e986204c5433e22192acc2cd574403794076b169))

## [1.1.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.0.3...v1.1.0) (2026-06-04)


### Features

* add a reusable FluidWindow control ([d3422a4](https://github.com/Alpaq92/Fluid.Avalonia/commit/d3422a4ce26978a978a76e7a7486ef50356f5a13))
* **demo:** add a standard-Window example on the Basic input page ([16086c3](https://github.com/Alpaq92/Fluid.Avalonia/commit/16086c31816ad920a04eda1f52d71f29442e284f))
* **demo:** make the example window a modal dialog with a close button ([a339057](https://github.com/Alpaq92/Fluid.Avalonia/commit/a339057f792bb82f4d9438b73645aaf5bfcdb8bf))


### Bug Fixes

* **theme:** align a plain Window's background with the OS title bar ([574fc3c](https://github.com/Alpaq92/Fluid.Avalonia/commit/574fc3cbd4bd4751f9611433bff989c5f1fe4c70))

## [1.0.3](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.0.2...v1.0.3) (2026-06-04)


### Bug Fixes

* **theme:** keep the NumericUpDown field uniform on hover/focus ([a39e8cd](https://github.com/Alpaq92/Fluid.Avalonia/commit/a39e8cd8c7c3e3d4dd9e63d8b7a47e806b68e474))

## [1.0.2](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.0.1...v1.0.2) (2026-06-04)


### Bug Fixes

* **theme:** lift the dark-mode solid background a step brighter ([bf25cce](https://github.com/Alpaq92/Fluid.Avalonia/commit/bf25cce8f15ef5dd9179b76621503886f65eeb23))

## [1.0.1](https://github.com/Alpaq92/Fluid.Avalonia/compare/v1.0.0...v1.0.1) (2026-06-04)


### Bug Fixes

* add a package icon and flesh out the NuGet readme ([f717914](https://github.com/Alpaq92/Fluid.Avalonia/commit/f7179145dacb324dec96d515c2eb77c9aac4fadd))

## [1.0.0](https://github.com/Alpaq92/Fluid.Avalonia/compare/v0.1.2...v1.0.0) (2026-06-03)


### Documentation

* add the NuGet install step ahead of the 1.0.0 release ([938261c](https://github.com/Alpaq92/Fluid.Avalonia/commit/938261cde21f9bdb9a2674bb9085adbb94898ff1))

## [0.1.2](https://github.com/Alpaq92/Fluid.Avalonia/compare/v0.1.1...v0.1.2) (2026-06-03)


### Bug Fixes

* **demo:** include the Label target in the example source snippet ([9f59360](https://github.com/Alpaq92/Fluid.Avalonia/commit/9f59360bc2f52c06ce17f29774f74378a55788e3))

## [0.1.1](https://github.com/Alpaq92/Fluid.Avalonia/compare/v0.1.0...v0.1.1) (2026-06-03)


### Bug Fixes

* **demo:** disable IL trimming for the WASM head so the live demo boots ([6122a5e](https://github.com/Alpaq92/Fluid.Avalonia/commit/6122a5e693791e061a3bb4d4853bec92835ca2cd))
* **demo:** lighten Markdown tables to header-underline + zebra ([f72e7fc](https://github.com/Alpaq92/Fluid.Avalonia/commit/f72e7fc1a933dbec207cd2ae44b17d542ea26b1f))
* **demo:** make the WASM splash follow the system light/dark scheme ([baae4dc](https://github.com/Alpaq92/Fluid.Avalonia/commit/baae4dc5937b52b9e0b0cf9734c52e04ddc026d4))
* **deps:** Bump the nuget-minor-and-patch group with 1 update ([#1](https://github.com/Alpaq92/Fluid.Avalonia/issues/1)) ([e4980a8](https://github.com/Alpaq92/Fluid.Avalonia/commit/e4980a868787aaaf190bd8e7dd585562b16c51b2))
* **theme:** vertically center native DatePicker/TimePicker segment text ([9a8087f](https://github.com/Alpaq92/Fluid.Avalonia/commit/9a8087f02fdca10aa193c491a7412eb09687c790))
* **theme:** vertically center picker segment text with any font ([2fd732d](https://github.com/Alpaq92/Fluid.Avalonia/commit/2fd732d95bbc1f817ca63d2d1354ae724d3f69ee))
