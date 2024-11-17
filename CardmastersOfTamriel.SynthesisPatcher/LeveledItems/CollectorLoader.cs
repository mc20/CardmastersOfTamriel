using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public static class CollectorLoader
{
    public static Dictionary<CollectorType, SortedSet<string>> GetCollectorIds(string configFilePath)
    {
        if (File.Exists(configFilePath))
            return JsonFileReader.ReadFromJson<Dictionary<CollectorType, SortedSet<string>>>(configFilePath);
        else
        {
            Log.Error($"Collector config file not found at: {configFilePath}");
            return [];
        }
    }

    public static Dictionary<CollectorType, SortedSet<string>> GetCollectorIds(HashSet<string> configFilePaths)
    {
        var combined = new Dictionary<CollectorType, SortedSet<string>>();
        foreach (var filePath in configFilePaths)
        {
            var config = GetCollectorIds(filePath);
            if (File.Exists(filePath))
            {
                foreach (var kvp in config)
                {
                    if (combined.TryGetValue(kvp.Key, out var existing))
                    {
                        existing.UnionWith(kvp.Value);
                    }
                    else
                    {
                        combined[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                Log.Error($"Collector config file not found at: {filePath}");
            }
        }

        return combined;
    }
}