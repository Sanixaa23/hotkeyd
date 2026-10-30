using Hotkeyd.Core;
using Xunit;

namespace Hotkeyd.Tests;

public class ParserTests
{
    [Theory]
    [InlineData("Ctrl+Alt+K", Modifiers.Control | Modifiers.Alt, 'K')]
    [InlineData("ctrl+k", Modifiers.Control, 'K')]
    [InlineData("CONTROL+SHIFT+A", Modifiers.Control | Modifiers.Shift, 'A')]
    [InlineData("Win+D", Modifiers.Win, 'D')]
    public void Parses_modifiers_and_key(string text, Modifiers mods, char key)
    {
        Assert.True(HotkeyParser.TryParse(text, out var hk, out _));
        Assert.Equal(mods, hk.Modifiers);
        Assert.Equal((uint)key, hk.Key.Code);
    }

    [Fact]
    public void Order_and_case_of_modifiers_do_not_matter()
    {
        HotkeyParser.TryParse("Alt+Ctrl+J", out var a, out _);
        HotkeyParser.TryParse("ctrl+alt+j", out var b, out _);
        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ctrl+Alt")]            // modifiers but no key
    [InlineData("Ctrl+K+J")]            // two keys
    [InlineData("Ctrl+Ctrl+K")]         // repeated modifier
    [InlineData("Ctrl+Splat")]          // unknown token
    public void Refuses_with_a_reason(string text)
    {
        Assert.False(HotkeyParser.TryParse(text, out _, out var error));
        Assert.NotEqual("", error);
    }

    [Fact]
    public void Every_listed_key_name_round_trips_through_parse()
    {
        // The name table is the whole truth about what a key is, so every
        // spelling in it must parse on its own as a modifier-less hotkey.
        foreach (var name in Keys.AllNames)
        {
            Assert.True(HotkeyParser.TryParse(name, out var hk, out var error),
                $"'{name}' did not parse: {error}");
            Assert.Equal(Modifiers.None, hk.Modifiers);
            Assert.Equal(Keys.Lookup(name)!.Value, hk.Key);
        }
    }

    [Fact]
    public void ToString_is_stable_and_canonical()
    {
        HotkeyParser.TryParse("alt+shift+ctrl+f5", out var hk, out _);
        Assert.Equal("Ctrl+Alt+Shift+F5", hk.ToString());
    }
}
