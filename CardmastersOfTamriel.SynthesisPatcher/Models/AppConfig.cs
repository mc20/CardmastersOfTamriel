using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class AppConfig
{
    public required string MetadataFilePath { get; init; }
    public required string LogOutputFilePath { get; init; }
    public required string CollectorNpcConfigFilePath { get; init; }
    public required string CollectorContainerConfigFilePath { get; init; }
    public required string ContainerConfigFilePath { get; init; }
    public required string LeveledItemConfigFilePath { get; init; }

    public string RetrieveMetadataFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(MetadataFilePath);
    public string RetrieveLogFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(LogOutputFilePath);
    public string RetrieveCollectorNpcConfigFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(CollectorNpcConfigFilePath);
    public string RetrieveCollectorContainerConfigPath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(CollectorContainerConfigFilePath);
    public string RetrieveContainerConfigFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(ContainerConfigFilePath);
    public string RetrieveLeveledItemConfigFilePath(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) => state.RetrieveInternalFile(LeveledItemConfigFilePath);
}