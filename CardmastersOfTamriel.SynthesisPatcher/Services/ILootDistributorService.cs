using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher;

public interface ILootDistributorService
{
    void DistributeLeveledItem(LeveledItem leveledItem);
}
