using System.Text;

namespace Hotkeyd.Core;

/// <summary>
/// The modifier keys, with the exact bit values Win32 <c>RegisterHotKey</c>
/// expects (MOD_ALT, MOD_CONTROL, MOD_SHIFT, MOD_WIN). Keeping the values
/// equal to the OS constants means the number stored here is the number
/// passed to the API -- there is no second table to drift out of step.
/// </summary>
[Flags]
public enum Modifiers
{
    None = 0,
    Alt = 0x1,
    Control = 0x2,
    Shift = 0x4,
    Win = 0x8,
}

/// <summary>
/// One hotkey: a set of modifiers and a single key. This is exactly what the
/// operating system's own hotkey facility matches -- one key plus modifiers,
/// nothing more -- so a chord of several keys in a row is deliberately not
/// representable. The OS does the matching, which is why this program never
/// needs to watch the keyboard itself.
/// </summary>
public readonly record struct Hotkey(Modifiers Modifiers, VirtualKey Key)
{
    /// <summary>
    /// The combination written back out the way it is read in, e.g.
    /// <c>Ctrl+Alt+K</c>. Modifiers are always in the same order so that two
    /// hotkeys that are the same combination also print the same string.
    /// </summary>
    public override string ToString()
    {
        var parts = new StringBuilder();
        if (Modifiers.HasFlag(Modifiers.Control)) parts.Append("Ctrl+");
        if (Modifiers.HasFlag(Modifiers.Alt)) parts.Append("Alt+");
        if (Modifiers.HasFlag(Modifiers.Shift)) parts.Append("Shift+");
        if (Modifiers.HasFlag(Modifiers.Win)) parts.Append("Win+");
        parts.Append(Keys.NameOf(Key));
        return parts.ToString();
    }
}
