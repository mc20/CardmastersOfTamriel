using CardmastersOfTamriel.SynthesisPatcher.Config;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Services
{
    public class CollectorService : ICollectorService
    {
        private readonly Dictionary<CollectorType, CollectorConfig> _collectorConfigs;

        public CollectorService(string configFilePath)
        {
            var configRoot = JsonFileReader.ReadFromJson<CollectorConfigRoot>(configFilePath);
            _collectorConfigs = new Dictionary<CollectorType, CollectorConfig>();

            foreach (var collector in configRoot.Collectors)
            {
                _collectorConfigs[collector.Type] = collector;
            }
        }

        public ICollector CreateCollector(CollectorType type)
        {
            if (!_collectorConfigs.TryGetValue(type, out var config))
            {
                throw new ArgumentException("Invalid collector type", nameof(type));
            }

            var cardTierProbabilities = new List<TierProbability>();
            foreach (var probConfig in config.CardTierProbabilities)
            {
                cardTierProbabilities.Add(new TierProbability
                {
                    Tier = probConfig.Tier,
                    NumberOfTimes = probConfig.NumberOfTimes,
                    ChanceNone = new Percent(probConfig.ChanceNone)
                });
            }

            return new Collector(config.Type, new Percent(config.ChanceNone), cardTierProbabilities);
        }
    }
}