using CardmastersOfTamriel.Models;
using Noggog;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Configuration.Models;

public interface ICollectorConfig<TCollection>
{
    string Name { get; }
    CollectorType Type { get; set; }
    Percent ChanceNone { get; set; }
    List<TierProbability> CardTierProbabilities { get; set; }
    ExtendedList<TCollection> GetCollection<TRecord>(TRecord record) where TRecord : class;
}