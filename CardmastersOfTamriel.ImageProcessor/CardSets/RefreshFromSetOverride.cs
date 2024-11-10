using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets;

public class OverrideSetMetadata : ICardSetHandler
{
    public void ProcessCardSet(CardSet set)
    {
        var handler = MasterMetadataProvider.Instance.MetadataHandler;

        var cardSetMetadata = handler.Metadata.Series?.SelectMany(series => series.Sets ?? [])
            .FirstOrDefault(s => s.Id == set.Id);

        if (cardSetMetadata == null)
        {
            Log.Error($"{set.Id}\tNo metadata found for set '{set.DisplayName}'");
            return;
        }

        var setOverrideJsonPath = ConfigurationProvider.Instance.Config.Paths.SetMetadataOverrideFilePath;
        var setMetadataOverride =
            FileOperations.FindMetadataLineBySetId<CardSetBasicMetadata>(setOverrideJsonPath, set.Id);
        if (setMetadataOverride is not null)
        {
            Log.Information($"{set.Id}\t'{set.DisplayName}':\tRefreshing data from set override file");
            cardSetMetadata.DisplayName = setMetadataOverride?.DisplayName ?? cardSetMetadata.DisplayName;
            cardSetMetadata.DefaultValue = setMetadataOverride?.DefaultValue ?? cardSetMetadata.DefaultValue;
            cardSetMetadata.DefaultWeight = setMetadataOverride?.DefaultWeight ?? cardSetMetadata.DefaultWeight;
            cardSetMetadata.DefaultKeywords = setMetadataOverride?.DefaultKeywords ?? cardSetMetadata.DefaultKeywords;
        }
        else
        {
            // Add to the jsonl if the set isn't there for convenience
            var basicMetadata = cardSetMetadata.GetBasicMetadata();
            basicMetadata.DefaultKeywords = ConfigurationProvider.Instance.Config.General.DefaultMiscItemKeywords;
            FileOperations.AppendDataToFile<CardSetBasicMetadata>(basicMetadata, setOverrideJsonPath);
            Log.Verbose(
                $"{set.Id}\t'{set.DisplayName}':\tAdded missing Card Set metadatda to override file at {setOverrideJsonPath}");
        }

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
        if (File.Exists(savedJsonFilePath))
        {
            var cardsFromMasterMetadata = JsonFileReader.LoadAllFromJsonLineFile<Card>(savedJsonFilePath)
                .Where(card => card != null).Select(card => card!).ToHashSet();
            cardSetMetadata.Cards ??= cardsFromMasterMetadata;
        }

        handler.WriteMetadataToFile();
    }
}