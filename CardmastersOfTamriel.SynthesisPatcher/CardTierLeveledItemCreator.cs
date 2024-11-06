using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher;

public class CardTierLeveledItemCreator
{
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;

    public CardTierLeveledItemCreator(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod skyrimMod)
    {
        _state = state;
        _skyrimMod = skyrimMod;
    }

    public Dictionary<CardTier, LeveledItem> CreateLeveledItemsForCardTiers(Dictionary<Card, MiscItem> miscItems)
    {
        var cardTierLeveledItems = new Dictionary<CardTier, LeveledItem>();
        foreach (var tier in Enum.GetValues(typeof(CardTier)).Cast<CardTier>())
        {
            var newLeveledItemId = $"LeveledItem_CardTier{tier}".AddModNamePrefix();

            if (_state.CheckIfExists<ILeveledItemGetter>(newLeveledItemId) ||
                _skyrimMod.CheckIfExists<LeveledItem>(newLeveledItemId))
            {
                Log.Warning($"LeveledItem {newLeveledItemId} already exists in the load order.");
                continue;
            }

            var newLeveledItemForCardTier = _skyrimMod.LeveledItems.AddNew();
            newLeveledItemForCardTier.EditorID = newLeveledItemId;
            newLeveledItemForCardTier.ChanceNone = Percent.Zero;
            newLeveledItemForCardTier.Entries ??= [];
            Counters.IncrementLeveledItemCount(
                $"Card{tier}\t{newLeveledItemForCardTier.EditorID}\tChanceNone: {100 - newLeveledItemForCardTier.ChanceNone}");

            foreach (var miscItem in miscItems.Where(item => item.Key.Tier == tier).Select(item => item.Value))
            {
                var entry = new LeveledItemEntry
                {
                    Data = new LeveledItemEntryData
                    {
                        Reference = miscItem.ToLink(),
                        Count = 1,
                        Level = 1
                    }
                };
                newLeveledItemForCardTier.Entries.Add(entry);
            }

            cardTierLeveledItems.Add(tier, newLeveledItemForCardTier);
        }

        return cardTierLeveledItems;
    }
}