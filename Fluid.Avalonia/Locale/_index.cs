using Avalonia.Controls;

namespace Fluid.Avalonia.Locale;

// One ResourceDictionary subclass per culture — the x:Class target of the matching Locale/<code>.axaml.
// (Same pattern as Semi.Avalonia's Locale/_index.cs and SukiUI's per-file Locale code-behind.)
// The lowercase culture-code names are deliberate (they must match each <code>.axaml's x:Class),
// so silence CS8981 "type name contains only lower-cased ascii characters".
#pragma warning disable CS8981
public class en : ResourceDictionary;

public class pl : ResourceDictionary;

public class de : ResourceDictionary;

public class fr : ResourceDictionary;

public class es : ResourceDictionary;
#pragma warning restore CS8981
