using System.Runtime.InteropServices;
using System.Windows.Interop;
using Hotkeyd.Core;

namespace Hotkeyd.App;

/// <summary>The outcome of trying to register one binding with the OS.</summary>
public sealed record Registered(Binding Binding, bool Ok, string Detail);

/// <summary>
/// Owns the live registrations against the operating system's hotkey facility
/// and raises <see cref="Fired"/> when one is pressed.
///
/// It leans on the OS on purpose. <c>RegisterHotKey</c> tells Windows which
/// combinations to watch, and Windows sends a single <c>WM_HOTKEY</c> message
/// only when one of exactly those combinations is pressed. hotkeyd therefore
/// never installs a keyboard hook and never sees any key the user did not
/// bind -- there is nothing here that could record typing.
///
/// Registration is measured, not assumed. Another program may already own a
/// combination; the OS then refuses it, the refusal is captured per binding,
/// and the rest still register. Nothing is reported as active unless the OS
/// accepted it.
/// </summary>
public sealed class HotkeyHost : IDisposable
{
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // MOD_NOREPEAT stops a held-down combination from firing over and over.
    private const uint ModNoRepeat = 0x4000;

    private readonly IntPtr _handle;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Binding> _byId = new();
    private int _nextId = 1;

    public event Action<Binding>? Fired;

    public HotkeyHost(HwndSource source)
    {
        _source = source;
        _handle = source.Handle;
        _source.AddHook(WndProc);
    }

    /// <summary>
    /// Drop every current registration and register the Ready rows of a plan.
    /// Returns one result per attempted row, so the UI can show which the OS
    /// accepted and which another program had already taken.
    /// </summary>
    public IReadOnlyList<Registered> Apply(IEnumerable<PlanRow> plan)
    {
        Clear();
        var results = new List<Registered>();
        foreach (var row in plan)
        {
            if (row.State != BindingState.Ready || row.Hotkey is not { } hotkey)
                continue;

            var id = _nextId++;
            var ok = RegisterHotKey(_handle, id,
                (uint)hotkey.Modifiers | ModNoRepeat, hotkey.Key.Code);
            if (ok)
            {
                _byId[id] = row.Binding;
                results.Add(new Registered(row.Binding, true, ""));
            }
            else
            {
                var code = Marshal.GetLastWin32Error();
                results.Add(new Registered(row.Binding, false,
                    $"Windows '{hotkey}' kaydını reddetti (başka bir program " +
                    $"kullanıyor olabilir, hata {code})"));
            }
        }
        return results;
    }

    public void Clear()
    {
        foreach (var id in _byId.Keys) UnregisterHotKey(_handle, id);
        _byId.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && _byId.TryGetValue(wParam.ToInt32(), out var binding))
        {
            handled = true;
            Fired?.Invoke(binding);
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Clear();
        _source.RemoveHook(WndProc);
    }
}
