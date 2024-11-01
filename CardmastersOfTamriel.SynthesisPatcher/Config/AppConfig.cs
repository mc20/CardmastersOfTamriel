using System.Text.Json;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class AppConfig
{
    public required string SourceFolder { get; set; }
    public required string OutputFolder { get; set; }
    public required string MasterMetadataPath { get; set; }
    public required Dictionary<string, string> TargetEditorIds { get; set; }
    public required string CollectorConfigPath { get; set; }

    public HashSet<string> GetEditorIds(string targetType)
    {
        return TargetEditorIds.TryGetValue(targetType, out var ids)
            ? ids.Split(",", StringSplitOptions.RemoveEmptyEntries)
                 .Select(id => id.Trim())
                 .ToHashSet()
            : [];
    }

    public static AppConfig Load(string configFilePath)
    {
        var configJson = File.ReadAllText(configFilePath);
        var config = JsonSerializer.Deserialize<AppConfig>(configJson) ?? throw new InvalidOperationException("Failed to deserialize the configuration file.");
        return config;
    }
}