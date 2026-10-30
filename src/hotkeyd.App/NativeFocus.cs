using System.Runtime.InteropServices;

namespace Hotkeyd.App;

/// <summary>Bringing another program's window to the front.</summary>
internal static class NativeFocus
{
    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    public static void Bring(IntPtr hWnd)
    {
        if (IsIconic(hWnd)) ShowWindow(hWnd, SwRestore);
        SetForegroundWindow(hWnd);
    }
}
