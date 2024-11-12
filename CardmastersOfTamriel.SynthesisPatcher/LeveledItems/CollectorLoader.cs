using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher.LeveledItems;

public static class CollectorLoader
{
    public static Dictionary<CollectorType, HashSet<string>> GetCollectorIds(string configFilePath)
    {
        return JsonFileReader.ReadFromJson<Dictionary<CollectorType, HashSet<string>>>(configFilePath);
    }
}