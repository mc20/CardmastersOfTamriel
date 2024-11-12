using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher.Models;

public class AppConfig
{
    public required string MasterMetadataFilePath { get; init; }
    public required string LogOutputFilePath { get; init; }
    public required CollectorsFilePaths CollectorsFilePaths { get; init; }
    public required DistributionsFilePaths DistributionsFilePaths { get; init; }
    
    public void ApplyInternalFilePaths(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        ApplyInternalFilePathsToProperties(this, state);
    }

    private static void ApplyInternalFilePathsToProperties(object configSection, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        foreach (var property in configSection.GetType().GetProperties())
        {
            if (property.PropertyType == typeof(string) && property.CanWrite)
            {
                var originalPath = (string)property.GetValue(configSection)!;
                var resolvedPath = state.RetrieveInternalFile(originalPath);
                property.SetValue(configSection, resolvedPath);
            }
            else if (!property.PropertyType.IsPrimitive && !property.PropertyType.IsEnum)
            {
                var nestedConfig = property.GetValue(configSection);
                if (nestedConfig != null)
                {
                    ApplyInternalFilePathsToProperties(nestedConfig, state);
                }
            }
        }
    }
}

public class CollectorsFilePaths
{
    public required string Container { get; init; }
    public required string LeveledItem { get; init; }
    public required string NonPlayerCharacter { get; init; }
}

public class DistributionsFilePaths
{
    public required string Container { get; init; }
    public required string LeveledItem { get; init; }
    public required string NonPlayerCharacter { get; init; }
}