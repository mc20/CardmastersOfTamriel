using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Models;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public class ContainerDistributor : IDistributor
{
    private readonly AppConfig _appConfig;
    private readonly IPatcherState<ISkyrimMod, ISkyrimModGetter> _state;
    private readonly ISkyrimMod _skyrimMod;

    public ContainerDistributor(AppConfig appConfig, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod skyrimMod)
    {
        _appConfig = appConfig;
        _state = state;
        _skyrimMod = skyrimMod;
    }

    public string UniqueIdentifier => "Container";

    public void Distribute(ICollector collector, LeveledItem leveledItemForCollector)
    {
        Log.Information("\n\nAssigning Collector LeveledItems to Designated Containers..\n");

        var designatedContainerJsonData =
            JsonFileReader.ReadFromJson<Dictionary<CollectorType, HashSet<string>>>(
                _appConfig.RetrieveContainerConfigFilePath(_state));
        Log.Information(
            $"Retrieved: {designatedContainerJsonData.Count} CollectorTypes from '{_appConfig.RetrieveContainerConfigFilePath(_state)}'");

        if (!designatedContainerJsonData.TryGetValue(collector.Type, out var editorIdsFromContainerJsonData)) return;

        Log.Information(
            $"Retrieved: {editorIdsFromContainerJsonData.Count} Containers for CollectorType: {collector.Type}");
        foreach (var designatedEditorId in editorIdsFromContainerJsonData.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            Log.Information($"Designated Container: {designatedEditorId}");
            var designatedContainer = _state.LoadOrder.PriorityOrder.Container().WinningOverrides()
                .FirstOrDefault(c => c.EditorID == designatedEditorId);

            if (designatedContainer is null) continue;

            var designatedContainerToModify = _skyrimMod.Containers.GetOrAddAsOverride(designatedContainer);
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
            Log.Information(
                $"Added LeveledItem: {leveledItemForCollector.EditorID} to Container: {designatedContainer.EditorID}");
        }
    }
}
