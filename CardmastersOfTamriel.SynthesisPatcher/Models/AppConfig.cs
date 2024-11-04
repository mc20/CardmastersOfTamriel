namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class AppConfig
{
    public required string SourceFolder { get; set; }
    public required string OutputFolder { get; set; }
    public required string MasterMetadataPath { get; set; }
    public required string CollectorConfigPath { get; set; }
    public required string ContainerConfigPath { get; set; }
    public required string LeveledItemConfigPath { get; set; }
}