using System.Runtime.InteropServices;

namespace Fluid.Avalonia.Demo.Interop;

/// <summary>
/// Minimal DWM interop so the native title bar follows the app's light/dark theme,
/// matching WinUI 3 windows. No-op on non-Windows hosts.
/// </summary>
internal static class Win11
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeOld = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static void SetTitleBarTheme(IntPtr hwnd, bool dark)
    {
        if (hwnd == IntPtr.Zero || !OperatingSystem.IsWindows())
            return;

        var flag = dark ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref flag, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeOld, ref flag, sizeof(int));
    }
}
