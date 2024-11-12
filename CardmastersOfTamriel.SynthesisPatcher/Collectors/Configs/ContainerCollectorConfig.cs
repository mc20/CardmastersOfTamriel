using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Configs;

public class ContainerCollectorConfig
{
    public string Name => "Container";
    public CollectorType Type { get; set; }
    public Percent ChanceNone { get; set; }
    public required List<TierProbability> CardTierProbabilities { get; set; }

    public static ContainerCollectorConfig CreateFromConfig(CollectorConfig config)
    {
        return new ContainerCollectorConfig
        {
            Type = config.Type,
            ChanceNone = new Percent(config.ChanceNone),
            CardTierProbabilities =
                config.CardTierProbabilities.Select(probConfig => new TierProbability
                {
                    Tier = probConfig.Tier,
                    NumberOfTimes = probConfig.NumberOfTimes,
                    ChanceNone = new Percent(probConfig.ChanceNone)
                }).ToList()
        };
    }
}

public class NpcCollectorConfig
{
    public string Name => "Npc";
    public CollectorType Type { get; set; }
    public Percent ChanceNone { get; set; }
    public List<TierProbability> CardTierProbabilities { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public static NpcCollectorConfig CreateFromConfig(CollectorConfig config)
    {
        return new NpcCollectorConfig
        {
            Type = config.Type,
            ChanceNone = new Percent(config.ChanceNone),
            CardTierProbabilities =
                config.CardTierProbabilities.Select(probConfig => new TierProbability
                {
                    Tier = probConfig.Tier,
                    NumberOfTimes = probConfig.NumberOfTimes,
                    ChanceNone = new Percent(probConfig.ChanceNone)
                }).ToList()
        };
    }
}