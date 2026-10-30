namespace Hotkeyd.Core;

/// <summary>What a hotkey does when it fires.</summary>
public enum ActionKind
{
    /// <summary>Start a program, or bring it to the front if it is already up.</summary>
    LaunchOrFocus,
    /// <summary>Open a URL or a file with its default handler.</summary>
    Open,
    /// <summary>Run a shell command line.</summary>
    Command,
}

/// <summary>
/// The description of an action, as data only. It carries no behaviour -- the
/// running of it lives in the app layer, where starting a process is a real
/// side effect -- so the core can be tested without launching anything.
/// </summary>
public sealed record ActionSpec
{
    public ActionKind Kind { get; init; } = ActionKind.LaunchOrFocus;

    /// <summary>Program path, URL/file, or command line, depending on Kind.</summary>
    public string Target { get; init; } = "";

    /// <summary>Arguments for <see cref="ActionKind.LaunchOrFocus"/>.</summary>
    public string Arguments { get; init; } = "";

    /// <summary>
    /// A human description, e.g. "Open Discord". Empty means the UI should
    /// show the target instead.
    /// </summary>
    public string Describe() => Kind switch
    {
        ActionKind.LaunchOrFocus => $"Başlat / öne getir: {Target}",
        ActionKind.Open => $"Aç: {Target}",
        ActionKind.Command => $"Komut: {Target}",
        _ => Target,
    };
}
