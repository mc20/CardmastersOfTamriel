using CardmastersOfTamriel.SynthesisPatcher.Models;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public interface IDistributor
{
    string UniqueIdentifier { get; }
    void Distribute(ICollectorConfig collectorConfig, LeveledItem leveledItemForCollector);
}
