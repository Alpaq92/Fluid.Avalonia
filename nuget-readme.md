# Fluid.Avalonia

[![NuGet](https://img.shields.io/nuget/v/Fluid.Avalonia.svg?label=NuGet&color=blue)](https://www.nuget.org/packages/Fluid.Avalonia)
[![Downloads](https://img.shields.io/nuget/dt/Fluid.Avalonia.svg?label=Downloads&color=blue)](https://www.nuget.org/packages/Fluid.Avalonia)
[![CI](https://img.shields.io/github/actions/workflow/status/Alpaq92/Fluid.Avalonia/ci.yml?branch=main&label=CI)](https://github.com/Alpaq92/Fluid.Avalonia/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Alpaq92/Fluid.Avalonia/blob/main/LICENSE)

A **Fluent 2 / WinUI 3-inspired theme for [Avalonia](https://github.com/AvaloniaUI/Avalonia)** with its own identity, adapting to your system accent color. Built on Avalonia's `FluentTheme` with ported Fluent 2 design tokens — lit-edge elevation, card surfaces, a Mica-ready window look, and a Fluent-tuned set of control themes.

![Fluid.Avalonia demo — the Accents page split diagonally between the light and dark themes](https://raw.githubusercontent.com/Alpaq92/Fluid.Avalonia/main/screenshot.png)

## Install

```bash
dotnet add package Fluid.Avalonia
```

## Usage

Add the theme in `App.axaml`:

```xml
<Application xmlns:fluid="clr-namespace:Fluid.Avalonia;assembly=Fluid.Avalonia" ...>
    <Application.Styles>
        <fluid:FluidTheme />
    </Application.Styles>
</Application>
```

The accent color is read from the OS automatically (Windows registry, with macOS / Linux / Avalonia fallbacks). Apply or override it at runtime via `Fluid.Avalonia.AccentColorService`.

> **Fonts:** the theme is font-agnostic — supply your own text and symbol fonts via `ContentControlThemeFontFamily` / `SymbolThemeFontFamily`. (The demo bundles DejaVu Sans for text and Codicons for icons.)

## Links

- 📖 [Documentation](https://github.com/Alpaq92/Fluid.Avalonia)
- 🌐 [Live demo (WebAssembly)](https://alpaq92.github.io/Fluid.Avalonia/)
- 🐛 [Issues](https://github.com/Alpaq92/Fluid.Avalonia/issues)
- 📝 [Changelog](https://github.com/Alpaq92/Fluid.Avalonia/blob/main/CHANGELOG.md)

[MIT](https://github.com/Alpaq92/Fluid.Avalonia/blob/main/LICENSE). Built on Avalonia's MIT-licensed `FluentTheme`; Fluent 2 design tokens ported from the MIT-licensed [microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml).
