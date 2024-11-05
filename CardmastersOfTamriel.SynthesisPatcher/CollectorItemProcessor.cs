using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

public class CollectorItemProcessor
{
    private readonly AppConfig _appConfig;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _customMod;

    public CollectorItemProcessor(AppConfig appConfig, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod)
    {
        _appConfig = appConfig;
        _state = state;
        _customMod = customMod;
    }

    public void SetupCollectorLeveledEntries(IEnumerable<ICollector> collectors, Dictionary<CardTier, LeveledItem> cardTierItemMappings)
    {

        foreach (var collector in collectors)
        {
            var leveledItemCollectorId = $"LeveledItem_Collector{collector.Type}".AddModNamePrefix();

            if (_state.CheckIfExists<ILeveledItemGetter>(leveledItemCollectorId) || _customMod.CheckIfExists<LeveledItem>(leveledItemCollectorId))
            {
                Log.Warning($"LeveledItem {leveledItemCollectorId} already exists in the load order.");
                continue;
            }

            var leveledItemForCollector = _customMod.LeveledItems.AddNew();
            leveledItemForCollector.EditorID = leveledItemCollectorId;
            leveledItemForCollector.ChanceNone = collector.ChanceNone;
            leveledItemForCollector.Entries ??= [];
            Counters.IncrementLeveledItemCount($"Collector{collector.Type}\t{leveledItemForCollector.EditorID}\tChanceNone: {leveledItemForCollector.ChanceNone}");

            foreach (var probability in collector.CardTierProbabilities)
            {
                AddLeveledItemForCollector(collector, probability, cardTierItemMappings, leveledItemForCollector);
            }

            DistributeToNpcs(collector, leveledItemForCollector);

            DistributeToContainers(collector, leveledItemForCollector);

        }
    }

    private void AddLeveledItemForCollector(ICollector collector, ITierProbability probability, Dictionary<CardTier, LeveledItem> tierBasedLeveledItems, LeveledItem cardCollectorLeveledItem)
    {
        var newProbabilityId = $"LeveledItem_Collector{collector.Type}_Card{probability.Tier}".AddModNamePrefix();

        if (_state.CheckIfExists<ILeveledItemGetter>(newProbabilityId) || _customMod.CheckIfExists<LeveledItem>(newProbabilityId))
        {
            Log.Warning($"LeveledItem {newProbabilityId} already exists in the load order.");
            return;
        }

        var newLeveledItemForCollectorProbability = _customMod.LeveledItems.AddNew();
        newLeveledItemForCollectorProbability.EditorID = newProbabilityId;
        newLeveledItemForCollectorProbability.ChanceNone = probability.ChanceNone;
        newLeveledItemForCollectorProbability.Entries ??= [];

        var tierEntry = new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Reference = tierBasedLeveledItems[probability.Tier].ToLink(),
                Count = 1,
                Level = 1
            }
        };

        newLeveledItemForCollectorProbability.Entries.Add(tierEntry);

        Counters.IncrementLeveledItemCount($"Collector{collector.Type}\tProbability Card{probability.Tier}\t{newLeveledItemForCollectorProbability.EditorID}\tChanceNone: {newLeveledItemForCollectorProbability.ChanceNone}");

        for (var i = 0; i < probability.NumberOfTimes; i++)
        {
            var entry = new LeveledItemEntry
            {
                Data = new LeveledItemEntryData
                {
                    Reference = newLeveledItemForCollectorProbability.ToLink(),
                    Count = 1,
                    Level = 1
                }
            };

            Counters.IncrementLeveledItemEntryCount(newLeveledItemForCollectorProbability.EditorID ?? "UNKNOWN");
            cardCollectorLeveledItem.Entries ??= [];
            cardCollectorLeveledItem.Entries.Add(entry);
        }
    }

    private void DistributeToNpcs(ICollector collector, LeveledItem leveledItemForCollector)
    {
        Log.Information("\n\nAssigning Collector LeveledItems to Designated LeveledItems..\n");

        var designatedLeveledItemJsonData = JsonFileReader.ReadFromJson<Dictionary<CollectorType, HashSet<string>>>(_appConfig.RetrieveLeveledItemConfigFilePath(_state));
        Log.Information($"Retrieved: {designatedLeveledItemJsonData.Count} CollectorTypes from '{_appConfig.RetrieveLeveledItemConfigFilePath(_state)}'");
        if (designatedLeveledItemJsonData.TryGetValue(collector.Type, out var editorIdsFromLeveledItemJsonData))
        {
            foreach (var designatedEditorId in editorIdsFromLeveledItemJsonData.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                var designatedLeveledItem = _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().FirstOrDefault(ll => ll.EditorID == designatedEditorId);
                if (designatedLeveledItem is not null)
                {
                    var designatedLeveledItemToModify = _customMod.LeveledItems.GetOrAddAsOverride(designatedLeveledItem);
                    designatedLeveledItemToModify.Entries ??= [];

                    var entry = new LeveledItemEntry
                    {
                        Data = new LeveledItemEntryData
                        {
                            Reference = leveledItemForCollector.ToLink(),
                            Count = 1,
                            Level = 1,
                        }
                    };

                    designatedLeveledItemToModify.Entries.Add(entry);
                    Log.Information($"Added LeveledItem: {leveledItemForCollector.EditorID} to LeveledItem: {designatedLeveledItem.EditorID}");
                }
            }
        }
    }

    private void DistributeToContainers(ICollector collector, LeveledItem leveledItemForCollector)
    {
        Log.Information("\n\nAssigning Collector LeveledItems to Designated Containers..\n");

        var designatedContainerJsonData = JsonFileReader.ReadFromJson<Dictionary<CollectorType, HashSet<string>>>(_appConfig.RetrieveContainerConfigFilePath(_state));
        Log.Information($"Retrieved: {designatedContainerJsonData.Count} CollectorTypes from '{_appConfig.RetrieveContainerConfigFilePath(_state)}'");

        // Log.Information(JsonSerializer.Serialize(designatedContainerJsonData));

        if (designatedContainerJsonData.TryGetValue(collector.Type, out var editorIdsFromContainerJsonData))
        {
            Log.Information($"Retrieved: {editorIdsFromContainerJsonData.Count} Containers for CollectorType: {collector.Type}");
            foreach (var designatedEditorId in editorIdsFromContainerJsonData.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                Log.Information($"Designated Container: {designatedEditorId}");
                var designatedContainer = _state.LoadOrder.PriorityOrder.Container().WinningOverrides().FirstOrDefault(c => c.EditorID == designatedEditorId);
                if (designatedContainer is not null)
                {
                    var designatedContainerToModify = _customMod.Containers.GetOrAddAsOverride(designatedContainer);
                    designatedContainerToModify.Items ??= [];

                    var entry = new ContainerEntry
                    {
                        Item = new ContainerItem
                        {
                            Item = leveledItemForCollector.ToLink(),
                            Count = 1
                        }
                    };

                    designatedContainerToModify.Items.Add(entry);
                    Log.Information($"Added LeveledItem: {leveledItemForCollector.EditorID} to Container: {designatedContainer.EditorID}");
                }
            }
        }
    }

}