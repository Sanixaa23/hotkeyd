using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Hotkeyd.App;

/// <summary>
/// Turns a plain WPF window into a translucent, frosted one by asking the
/// desktop compositor (DWM) for an acrylic backdrop. This is the real material
/// behind the Apple-style "vibrancy": the blur is the operating system's, not
/// a picture of one, so whatever is behind the window shows through softly.
///
/// It is a best effort. On Windows 10 the acrylic attribute is simply ignored;
/// the window then falls back to the light frost tint the theme paints over it,
/// which still reads as a pale, airy surface rather than breaking.
/// </summary>
internal static class Backdrop
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Margins { public int L, R, T, B; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins m);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    // DWM attribute ids (Windows 11).
    private const int UseImmersiveDarkMode = 20;
    private const int WindowCornerPreference = 33;
    private const int SystemBackdropType = 38;

    private const int CornerRound = 2;
    private const int BackdropAcrylic = 3;   // DWMSBT_TRANSIENTWINDOW

    public static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        if (source?.CompositionTarget is { } target)
            target.BackgroundColor = Colors.Transparent;

        // Let the backdrop fill the whole client area, not just the border.
        var sheet = new Margins { L = -1, R = -1, T = -1, B = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref sheet);

        var light = 0;
        DwmSetWindowAttribute(hwnd, UseImmersiveDarkMode, ref light, sizeof(int));

        var round = CornerRound;
        DwmSetWindowAttribute(hwnd, WindowCornerPreference, ref round, sizeof(int));

        var acrylic = BackdropAcrylic;
        DwmSetWindowAttribute(hwnd, SystemBackdropType, ref acrylic, sizeof(int));
    }
}
