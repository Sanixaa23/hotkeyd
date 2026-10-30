using Microsoft.Win32;

namespace Hotkeyd.App;

/// <summary>
/// Whether hotkeyd starts when you log in, through the per-user Run key in the
/// registry (HKEY_CURRENT_USER). That key needs no administrator rights and
/// affects only the current account, which is the right scope for a tool that
/// holds one person's hotkeys. It is never written unless the user asks.
/// </summary>
public static class Startup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "hotkeyd";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is not null;
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
        {
            var exe = Environment.ProcessPath
                      ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe is null) throw new InvalidOperationException("kendi yolu bulunamadı");
            key.SetValue(ValueName, $"\"{exe}\" --tray");
        }
        else if (key.GetValue(ValueName) is not null)
        {
            key.DeleteValue(ValueName);
        }
    }
}
