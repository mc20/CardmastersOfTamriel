using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class NpcDistributor : IDistributor
{
    private readonly AppConfig _appConfig;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;

    public NpcDistributor(AppConfig appConfig, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod skyrimMod)
    {
        _appConfig = appConfig;
        _state = state;
        _skyrimMod = skyrimMod;
    }

    public string UniqueIdentifier => "Npc";

    public void Distribute(ICollector collector, LeveledItem leveledItemForCollector)
    {
        Log.Verbose("\n\nAssigning Collector LeveledItems to Designated LeveledItems..\n");

        var designatedLeveledItemJsonData =
            JsonFileReader.ReadFromJson<Dictionary<CollectorType, HashSet<string>>>(
                _appConfig.RetrieveLeveledItemConfigFilePath(_state));
        Log.Information(
            $"Retrieved: {designatedLeveledItemJsonData.Count} CollectorTypes from '{_appConfig.RetrieveLeveledItemConfigFilePath(_state)}'");

        if (!designatedLeveledItemJsonData.TryGetValue(collector.Type, out var editorIdsFromLeveledItemJsonData))
            return;

        foreach (var designatedEditorId in editorIdsFromLeveledItemJsonData.Where(id =>
                     !string.IsNullOrWhiteSpace(id)))
        {
            var designatedLeveledItem = _state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()
                .FirstOrDefault(ll => ll.EditorID == designatedEditorId);

            if (designatedLeveledItem is null) continue;
            var designatedLeveledItemToModify = _skyrimMod.LeveledItems.GetOrAddAsOverride(designatedLeveledItem);
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