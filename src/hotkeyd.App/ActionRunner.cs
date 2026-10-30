using System.Diagnostics;
using System.IO;
using Hotkeyd.Core;

namespace Hotkeyd.App;

/// <summary>
/// Carries out an <see cref="ActionSpec"/>. This is where the side effects
/// live -- starting a process, opening a URL -- kept out of the core so the
/// core stays pure and testable.
///
/// A failure here is returned, not thrown past the caller: a mistyped path
/// should put a message on the dashboard, never crash the app that is holding
/// every other hotkey.
/// </summary>
public static class ActionRunner
{
    public static (bool ok, string message) Run(ActionSpec action)
    {
        try
        {
            switch (action.Kind)
            {
                case ActionKind.LaunchOrFocus:
                    if (FocusExisting(action.Target)) return (true, "öne getirildi");
                    Start(action.Target, action.Arguments);
                    return (true, "başlatıldı");

                case ActionKind.Open:
                    Process.Start(new ProcessStartInfo(action.Target) { UseShellExecute = true });
                    return (true, "açıldı");

                case ActionKind.Command:
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c " + action.Target)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    });
                    return (true, "çalıştırıldı");

                default:
                    return (false, "bilinmeyen eylem");
            }
        }
        catch (Exception error)
        {
            return (false, error.Message);
        }
    }

    private static void Start(string path, string arguments)
    {
        var info = new ProcessStartInfo(path) { UseShellExecute = true };
        if (!string.IsNullOrWhiteSpace(arguments)) info.Arguments = arguments;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            info.WorkingDirectory = directory;
        Process.Start(info);
    }

    /// <summary>
    /// If a process started from the same executable is already running with a
    /// main window, bring it to the front instead of starting a second copy.
    /// This is a best effort: processes with no window, or matched by a
    /// different path, simply fall through to a fresh launch.
    /// </summary>
    private static bool FocusExisting(string path)
    {
        if (!File.Exists(path)) return false;
        var name = Path.GetFileNameWithoutExtension(path);
        foreach (var process in Process.GetProcessesByName(name))
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                NativeFocus.Bring(process.MainWindowHandle);
                return true;
            }
        }
        return false;
    }
}
