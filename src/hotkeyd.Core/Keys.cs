namespace Hotkeyd.Core;

/// <summary>
/// A Windows virtual-key code. It is its own type rather than a bare number
/// so that a key cannot be passed where a modifier mask is expected, which is
/// an easy and silent mistake when both are integers.
/// </summary>
public readonly record struct VirtualKey(uint Code);

/// <summary>
/// The names a key may be written as, and the virtual-key code each maps to.
///
/// The table is the whole truth: a name that is not in it is not a key, and
/// parsing refuses it rather than guessing. That is what lets the parser be
/// tested exhaustively -- every accepted spelling is listed here, so a test
/// can walk the table and expect every one to round-trip.
/// </summary>
public static class Keys
{
    // Declared before ByName on purpose: field initializers run top to
    // bottom, and Build() fills this one, so it must already exist when
    // Build() runs.
    private static readonly Dictionary<uint, string> CanonicalName = new();
    private static readonly Dictionary<string, VirtualKey> ByName = Build();

    private static Dictionary<string, VirtualKey> Build()
    {
        var map = new Dictionary<string, VirtualKey>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, uint code, bool canonical = true)
        {
            map[name] = new VirtualKey(code);
            if (canonical) CanonicalName[code] = name;
        }

        for (char c = 'A'; c <= 'Z'; c++) Add(c.ToString(), (uint)c);
        for (char d = '0'; d <= '9'; d++) Add(d.ToString(), (uint)d);
        for (uint f = 1; f <= 24; f++) Add("F" + f, 0x6F + f);

        // Named keys. The spelling on the left of a group is the one printed
        // back; the others are accepted spellings for the same code.
        Add("Space", 0x20);
        Add("Enter", 0x0D); Add("Return", 0x0D, canonical: false);
        Add("Tab", 0x09);
        Add("Esc", 0x1B); Add("Escape", 0x1B, canonical: false);
        Add("Backspace", 0x08);
        Add("Delete", 0x2E); Add("Del", 0x2E, canonical: false);
        Add("Insert", 0x2D); Add("Ins", 0x2D, canonical: false);
        Add("Home", 0x24);
        Add("End", 0x23);
        Add("PageUp", 0x21); Add("PgUp", 0x21, canonical: false);
        Add("PageDown", 0x22); Add("PgDn", 0x22, canonical: false);
        Add("Left", 0x25);
        Add("Up", 0x26);
        Add("Right", 0x27);
        Add("Down", 0x28);
        Add("PrintScreen", 0x2C); Add("PrtSc", 0x2C, canonical: false);
        Add("Pause", 0x13);

        Add("`", 0xC0); Add("-", 0xBD); Add("=", 0xBB);
        Add("[", 0xDB); Add("]", 0xDD); Add("\\", 0xDC);
        Add(";", 0xBA); Add("'", 0xDE);
        Add(",", 0xBC); Add(".", 0xBE); Add("/", 0xBF);

        // Numpad digits, then its operator keys. The operators are spelled
        // with words rather than the symbols +-*/ because '+' is the token
        // separator in a hotkey string, so a key literally called "Num+"
        // could never be written down unambiguously.
        for (uint n = 0; n <= 9; n++) Add("Num" + n, 0x60 + n);
        Add("NumAdd", 0x6B); Add("NumSub", 0x6D);
        Add("NumMul", 0x6A); Add("NumDiv", 0x6F); Add("NumDot", 0x6E);

        return map;
    }

    /// <summary>The key for a name, or null if the name is not a key.</summary>
    public static VirtualKey? Lookup(string name) =>
        ByName.TryGetValue(name, out var key) ? key : null;

    /// <summary>The printed name of a key, e.g. for <c>Hotkey.ToString</c>.</summary>
    public static string NameOf(VirtualKey key) =>
        CanonicalName.TryGetValue(key.Code, out var name)
            ? name
            : $"0x{key.Code:X2}";

    /// <summary>Every accepted spelling, for tests and for the UI's picker.</summary>
    public static IReadOnlyCollection<string> AllNames => ByName.Keys;
}
