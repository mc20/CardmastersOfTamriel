using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class AppConfig
{
    public required string MetadataFilePath { get; set; }
    public required string LogOutputFilePath { get; set; }
    public required string CollectorNpcConfigFilePath { get; set; }
    public required string CollectorContainerConfigFilePath { get; set; }
    public required string ContainerConfigFilePath { get; set; }
    public required string LeveledItemConfigFilePath { get; set; }

    public string RetrieveMetadataFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(MetadataFilePath);
    public string RetrieveLogFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(LogOutputFilePath);
    public string RetrieveCollectorNpcConfigFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(CollectorNpcConfigFilePath);
    public string RetrieveCollectorContainerConfigPath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(CollectorContainerConfigFilePath);
    public string RetrieveContainerConfigFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(ContainerConfigFilePath);
    public string RetrieveLeveledItemConfigFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(LeveledItemConfigFilePath);
}