using Hotkeyd.Core;
using Xunit;

namespace Hotkeyd.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "hotkeyd-test-" + Guid.NewGuid().ToString("N"));

    private string File => Path.Combine(_dir, "config.json");

    [Fact]
    public void A_missing_file_loads_as_an_empty_config()
    {
        var store = new ConfigStore(File);
        var config = store.Load();
        Assert.Empty(config.Bindings);
    }

    [Fact]
    public void Round_trips_every_field()
    {
        var store = new ConfigStore(File);
        var original = new Config
        {
            Bindings =
            {
                new Binding
                {
                    Label = "Discord",
                    HotkeyText = "Ctrl+Alt+D",
                    Enabled = true,
                    Action = new ActionSpec
                    {
                        Kind = ActionKind.LaunchOrFocus,
                        Target = @"C:\Discord\Discord.exe",
                        Arguments = "--start-minimized",
                    },
                },
                new Binding
                {
                    Label = "Docs",
                    HotkeyText = "Win+F1",
                    Enabled = false,
                    Action = new ActionSpec { Kind = ActionKind.Open, Target = "https://example.test" },
                },
            },
        };
        store.Save(original);
        var loaded = store.Load();
        Assert.Equal(original.Bindings, loaded.Bindings);
        Assert.Equal(original.Version, loaded.Version);
    }

    [Fact]
    public void Enum_values_are_written_as_names_so_the_file_reads()
    {
        var store = new ConfigStore(File);
        store.Save(new Config { Bindings = { new Binding { Action = new ActionSpec { Kind = ActionKind.Command } } } });
        var text = System.IO.File.ReadAllText(File);
        Assert.Contains("Command", text);
        Assert.DoesNotContain("\"Kind\": 2", text);
    }

    [Fact]
    public void A_corrupt_file_throws_rather_than_discarding_bindings()
    {
        Directory.CreateDirectory(_dir);
        System.IO.File.WriteAllText(File, "{ this is not json");
        var store = new ConfigStore(File);
        Assert.ThrowsAny<Exception>(() => store.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}
