using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Targets;

public interface ICollectorTarget
{
    string Name { get; }
    void AddItem(IFormLink<IItemGetter> item);
}