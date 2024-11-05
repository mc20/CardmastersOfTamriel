using CardmastersOfTamriel.SynthesisPatcher.Models;
using Mutagen.Bethesda.Skyrim;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class LeveledItemDistributorHelper
{
    public static void DistributeItems<T>(ISkyrimMod customMod,
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
                    Log.Warning($"Failed to add item to {editorId}");
                }
            }
        }
    }
}