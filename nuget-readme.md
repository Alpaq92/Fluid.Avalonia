# Avalonia.Fluid

[![NuGet](https://img.shields.io/nuget/v/Avalonia.Fluid.svg?label=NuGet&color=blue)](https://www.nuget.org/packages/Avalonia.Fluid)
[![Downloads](https://img.shields.io/nuget/dt/Avalonia.Fluid.svg?label=Downloads&color=blue)](https://www.nuget.org/packages/Avalonia.Fluid)
[![CI](https://img.shields.io/github/actions/workflow/status/Alpaq92/Avalonia.Fluid/ci.yml?branch=main&label=CI)](https://github.com/Alpaq92/Avalonia.Fluid/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Alpaq92/Avalonia.Fluid/blob/main/LICENSE)

A **Fluent 2 / WinUI 3-inspired theme for [Avalonia](https://github.com/AvaloniaUI/Avalonia)** with its own identity, adapting to your system accent color. Built on Avalonia's `FluentTheme` with ported Fluent 2 design tokens — lit-edge elevation, card surfaces, a Mica-ready window look, and a Fluent-tuned set of control themes.

![Avalonia.Fluid demo — the Home page split diagonally between the light and dark themes](https://raw.githubusercontent.com/Alpaq92/Avalonia.Fluid/main/screenshot.png)

## Install

```bash
dotnet add package Avalonia.Fluid
```

## Usage

Add the theme in `App.axaml`:

```xml
<Application xmlns:fluid="clr-namespace:Avalonia.Fluid;assembly=Avalonia.Fluid" ...>
    <Application.Styles>
        <fluid:FluidTheme />
    </Application.Styles>
</Application>
```

The accent color is read from the OS automatically (Windows registry, with macOS / Linux / Avalonia fallbacks). Apply or override it at runtime via `Avalonia.Fluid.AccentColorService`.

> **Fonts:** the theme is font-agnostic — supply your own text and symbol fonts via `ContentControlThemeFontFamily` / `SymbolThemeFontFamily`. (The demo bundles DejaVu Sans for text and Codicons for icons.)

## Links

- 📖 [Documentation](https://github.com/Alpaq92/Avalonia.Fluid)
- 🌐 [Live demo (WebAssembly)](https://alpaq92.github.io/Avalonia.Fluid/)
- 🐛 [Issues](https://github.com/Alpaq92/Avalonia.Fluid/issues)
- 📝 [Changelog](https://github.com/Alpaq92/Avalonia.Fluid/blob/main/CHANGELOG.md)

[MIT](https://github.com/Alpaq92/Avalonia.Fluid/blob/main/LICENSE). Built on Avalonia's MIT-licensed `FluentTheme`; Fluent 2 design tokens ported from the MIT-licensed [microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml).
