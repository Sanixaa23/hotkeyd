using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hotkeyd.Core;

/// <summary>The whole configuration: the bindings, and the file format version.</summary>
public sealed record Config
{
    public int Version { get; init; } = 1;
    public List<Binding> Bindings { get; init; } = new();
}

/// <summary>
/// Reads and writes the configuration as JSON. Two habits make it safe to
/// hand-edit and safe to crash during a save:
///
///   * A file that does not parse is reported, not silently replaced with an
///     empty one, so a typo never costs you every binding.
///   * The save is written to a temporary file and then moved into place, so
///     an interrupted write cannot leave a half-written config behind.
/// </summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Path { get; }

    public ConfigStore(string path) => Path = path;

    /// <summary>The default location: %APPDATA%\hotkeyd\config.json.</summary>
    public static ConfigStore Default()
    {
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "hotkeyd");
        return new ConfigStore(System.IO.Path.Combine(dir, "config.json"));
    }

    public Config Load()
    {
        if (!File.Exists(Path)) return new Config();
        var text = File.ReadAllText(Path);
        if (string.IsNullOrWhiteSpace(text)) return new Config();
        var config = JsonSerializer.Deserialize<Config>(text, Options);
        return config ?? throw new InvalidDataException(
            $"{Path} parsed to nothing; it is not a hotkeyd config file");
    }

    public void Save(Config config)
    {
        var directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path))!;
        Directory.CreateDirectory(directory);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(config, Options));
        File.Move(temporary, Path, overwrite: true);
    }
}
