using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Collectors.Targets;

public class ContainerTarget : ICollectorTarget
{
    private readonly Container _container;

    public ContainerTarget(Container container)
    {
        _container = container;
    }

    public string Name => "Container";

    public void AddItem(IFormLink<IItemGetter> item)
    {
        _container.Items ??= [];
        _container.Items.Add(new ContainerEntry
        {
            Item = new ContainerItem  // Wrap in ContainerItem
            {
                Item = item,
                Count = 1
            }
        });
    }
}