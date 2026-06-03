using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace Avalonia.Fluid.Locale;

/// <summary>
/// Localization the Semi.Avalonia / SukiUI way: one <see cref="ResourceDictionary"/> per culture
/// (Locale/&lt;code&gt;.axaml) merged into the application, swapped at runtime. <c>{DynamicResource
/// STRING_*}</c> references update live on swap; code that paints text itself subscribes to
/// <see cref="LanguageChanged"/>. Defaults to the system language (falling back to English). No
/// third-party localization package.
/// </summary>
public static class LocaleManager
{
    /// <summary>Bundled language codes (each maps to Locale/&lt;code&gt;.axaml).</summary>
    public static readonly string[] Supported = { "en", "pl", "de", "fr", "es" };

    private static ResourceDictionary? _current;

    /// <summary>The active language code.</summary>
    public static string CurrentLanguage { get; private set; } = "en";

    /// <summary>Raised after the active language changes — for text painted in code-behind.</summary>
    public static event Action? LanguageChanged;

    /// <summary>Apply the current system language, falling back to English.</summary>
    public static void UseSystemDefault() =>
        SetLanguage(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

    /// <summary>Switch the active language; an unsupported code falls back to English.</summary>
    public static void SetLanguage(string code)
    {
        if (Application.Current is not { } app)
            return;

        if (!Supported.Contains(code))
            code = "en";

        ResourceDictionary dict = code switch
        {
            "pl" => new pl(),
            "de" => new de(),
            "fr" => new fr(),
            "es" => new es(),
            _ => new en(),
        };

        if (_current is not null)
            app.Resources.MergedDictionaries.Remove(_current);
        app.Resources.MergedDictionaries.Add(dict);
        _current = dict;
        CurrentLanguage = code;

        LanguageChanged?.Invoke();
    }
}
