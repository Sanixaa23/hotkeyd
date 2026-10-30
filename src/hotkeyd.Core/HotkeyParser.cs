namespace Hotkeyd.Core;

/// <summary>
/// Turns text such as <c>Ctrl+Alt+K</c> into a <see cref="Hotkey"/>, or says
/// exactly why it cannot. It never guesses: an unknown word, a missing key, a
/// repeated modifier or two non-modifier keys are each a named error, not a
/// best effort. A hotkey the parser cannot read is one the OS could not have
/// registered, so refusing here is the same promise kept one step earlier.
/// </summary>
public static class HotkeyParser
{
    public static bool TryParse(string text, out Hotkey hotkey, out string error)
    {
        hotkey = default;
        error = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "the hotkey is empty";
            return false;
        }

        var tokens = text.Split('+', StringSplitOptions.TrimEntries)
                         .Where(t => t.Length > 0)
                         .ToArray();
        if (tokens.Length == 0)
        {
            error = "the hotkey is empty";
            return false;
        }

        var modifiers = Modifiers.None;
        VirtualKey? key = null;

        foreach (var token in tokens)
        {
            var asModifier = ModifierOf(token);
            if (asModifier is { } mod)
            {
                if (modifiers.HasFlag(mod))
                {
                    error = $"'{token}' is given twice";
                    return false;
                }
                modifiers |= mod;
                continue;
            }

            var asKey = Keys.Lookup(token);
            if (asKey is null)
            {
                error = $"'{token}' is not a key or modifier";
                return false;
            }
            if (key is not null)
            {
                error = "a hotkey has only one key besides its modifiers; " +
                        $"both '{Keys.NameOf(key.Value)}' and '{token}' are keys";
                return false;
            }
            key = asKey;
        }

        if (key is null)
        {
            error = "the hotkey has modifiers but no key";
            return false;
        }

        hotkey = new Hotkey(modifiers, key.Value);
        return true;
    }

    private static Modifiers? ModifierOf(string token) => token.ToLowerInvariant() switch
    {
        "ctrl" or "control" => Modifiers.Control,
        "alt" => Modifiers.Alt,
        "shift" => Modifiers.Shift,
        "win" or "meta" or "cmd" or "super" => Modifiers.Win,
        _ => null,
    };
}
