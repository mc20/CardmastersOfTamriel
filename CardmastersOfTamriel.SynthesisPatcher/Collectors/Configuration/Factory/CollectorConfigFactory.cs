using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Configs;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Factory;

public class CollectorConfigFactory
{
    private readonly string _configFilePath;

    public CollectorConfigFactory(string configFilePath)
    {
        _configFilePath = configFilePath;
        Log.Information($"Loading collector config from: '{configFilePath}'");
    }

    public HashSet<ICollectorConfig<LeveledItemEntry>> LoadNpcCollectors()
    {
        var configRoot = JsonFileReader.ReadFromJson<CollectorConfigRoot>(_configFilePath);
        return configRoot.Collectors.Select(NpcCollectorConfig.CreateFromConfig).ToHashSet();
    }
    
    public HashSet<ICollectorConfig<ContainerEntry>> LoadContainers()
    {
        var configRoot = JsonFileReader.ReadFromJson<CollectorConfigRoot>(_configFilePath);
        return configRoot.Collectors.Select(ContainerCollectorConfig.CreateFromConfig).ToHashSet();
    }
}