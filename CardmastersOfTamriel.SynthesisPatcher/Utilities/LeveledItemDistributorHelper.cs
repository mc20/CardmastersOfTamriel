using CardmastersOfTamriel.SynthesisPatcher.Models;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class LeveledItemDistributorHelper
{
    public static void DistributeItems<T>(
        ISkyrimMod customMod,
        string filePathToConfig,
        ICollector collector,
        T collectorItem,
        Func<ISkyrimMod, T, string, bool> addItemToTarget)
    {
        var jsonData = JsonFileReader.ReadFromJson<Dictionary<CollectorType, HashSet<string>>>(filePathToConfig);
        if (jsonData.TryGetValue(collector.Type, out var editorIds))
        {
            foreach (var editorId in editorIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                if (!addItemToTarget(customMod, collectorItem, editorId))
                {
                    Logger.LogAction($"Failed to add item to {editorId}", LogMessageType.Warning);
                }
            }
        }
    }
}
