using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Configs;

public class NpcCollectorConfig : ICollectorConfig<LeveledItemEntry>
{
    public string Name => "Npc";
    public CollectorType Type { get; set; }
    public Percent ChanceNone { get; set; }
    public required List<TierProbability> CardTierProbabilities { get; set; }

    public ExtendedList<LeveledItemEntry> GetCollection<TRecord>(TRecord record) where TRecord : class
    {
        if (record is LeveledItem leveledItem)
        {
            leveledItem.Entries ??= [];
            return leveledItem.Entries;
        }

        throw new ArgumentException($"Expected LeveledItem but got {typeof(TRecord)}");
    }

    public static ICollectorConfig<LeveledItemEntry> CreateFromConfig(CollectorConfig config)
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