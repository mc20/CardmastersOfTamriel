using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public static class CollectorLoader
{
    public static async Task<Dictionary<CollectorType, SortedSet<string>>> GetCollectorIdsAsync(HashSet<string> configFilePaths,
        CancellationToken cancellationToken)
    {
        var combined = new Dictionary<CollectorType, SortedSet<string>>();
        foreach (var filePath in configFilePaths)
        {
            var config = await GetCollectorIdsAsync(filePath, cancellationToken);
            if (File.Exists(filePath))
                foreach (var kvp in config)
                    if (combined.TryGetValue(kvp.Key, out var existing))
                        existing.UnionWith(kvp.Value);
                    else
                        combined[kvp.Key] = kvp.Value;
            else
                Log.Error($"Collector config file not found at: {filePath}");
        }

        return combined;
    }
    
    private static async Task<Dictionary<CollectorType, SortedSet<string>>> GetCollectorIdsAsync(string configFilePath, CancellationToken cancellationToken)
    {
        if (File.Exists(configFilePath))
            return await JsonFileReader.ReadFromJsonAsync<Dictionary<CollectorType, SortedSet<string>>>(configFilePath, cancellationToken);
        Log.Error($"Collector config file not found at: {configFilePath}");
        return [];
    }
}