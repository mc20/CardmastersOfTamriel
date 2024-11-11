using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Configs;

public class ContainerCollectorConfig : ICollectorConfig<ContainerEntry>
{
    public string Name => "Container";
    public CollectorType Type { get; set; }
    public Percent ChanceNone { get; set; }
    public required List<TierProbability> CardTierProbabilities { get; set; }
    public ExtendedList<ContainerEntry> GetCollection<TRecord>(TRecord record) where TRecord : class
    {
        if (record is Container container)
        {
            container.Items ??= [];
            return container.Items;
        }
        
        throw new ArgumentException($"Expected Container but got {typeof(TRecord)}");
    }
    
    public static ICollectorConfig<ContainerEntry> CreateFromConfig(CollectorConfig config)
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