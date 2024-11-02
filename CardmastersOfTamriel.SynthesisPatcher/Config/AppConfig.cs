using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class AppConfig
{
    public required string SourceFolder { get; set; }
    public required string OutputFolder { get; set; }
    public required string MasterMetadataPath { get; set; }
    public Dictionary<CollectorType, string> TargetEditorIds { get; set; } = new();
    public required string CollectorConfigPath { get; set; }
    public required string ContainerConfigPath { get; set; }
    public required string LeveledItemConfigPath { get; set; }

    public static AppConfig Load(string configFilePath)
    {
        return JsonFileReader.ReadFromJson<AppConfig>(configFilePath);
    }
}