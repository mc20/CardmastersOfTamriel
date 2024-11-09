using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Configuration.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Configuration;

public class CollectorConfigFactory : ICollectorConfigFactory
{
    private readonly Dictionary<CollectorType, CollectorConfig> _collectorConfigs = [];

    public CollectorConfigFactory(string configFilePath)
    {
        Log.Information($"Loading collector config from: '{configFilePath}'");

        var configRoot = JsonFileReader.ReadFromJson<CollectorConfigRoot>(configFilePath);

        foreach (var collector in configRoot.Collectors)
        {
            _collectorConfigs[collector.Type] = collector;
        }
    }

    public ICollector? CreateCollector(CollectorType type)
    {
        if (!_collectorConfigs.TryGetValue(type, out var config))
        {
            Log.Error($"No config found for collector type: {type}");
            return null;
            // throw new ArgumentException("Invalid collector type", nameof(type));
        }

        var cardTierProbabilities = config.CardTierProbabilities.Select(probConfig => new TierProbability
        {
            Tier = probConfig.Tier,
            NumberOfTimes = probConfig.NumberOfTimes,
            ChanceNone = new Percent(probConfig.ChanceNone)
        }).ToList();

        return new Collector(config.Type, new Percent(config.ChanceNone), cardTierProbabilities);
    }
}
