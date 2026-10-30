namespace Hotkeyd.Core;

/// <summary>
/// One row of the configuration: a hotkey, written as text, and the action it
/// triggers. The hotkey is kept as text, not a parsed <see cref="Hotkey"/>, so
/// that a file hand-edited into an invalid combination round-trips and is
/// reported in the UI rather than throwing on load.
/// </summary>
public sealed record Binding
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Label { get; init; } = "";
    public string HotkeyText { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public ActionSpec Action { get; init; } = new();

    /// <summary>The parsed hotkey, or null with the reason it would not parse.</summary>
    public Hotkey? Parse(out string error) =>
        HotkeyParser.TryParse(HotkeyText, out var hk, out error) ? hk : null;
}
