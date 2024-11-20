using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class OverrideSetMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken)
    {
        var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "set_metadata.json");
        if (!File.Exists(destinationCardSetJsonFilePath))
        {
            Log.Error($"{set.Id}\tNo set metadata file found at {destinationCardSetJsonFilePath}");
            return;
        }

        var cardSetMetadata = await JsonFileReader.ReadFromJsonAsync<CardSet>(destinationCardSetJsonFilePath, cancellationToken);

        CardSetBasicMetadata? setMetadataOverride = null;
        var setOverrideJsonPath = ConfigurationProvider.Instance.Config.Paths.SetMetadataOverrideFilePath;
        if (!File.Exists(setOverrideJsonPath))
        {
            setMetadataOverride = await JsonFileReader.FindMetadataLineBySetIdAsync<CardSetBasicMetadata>(setOverrideJsonPath, set.Id, cancellationToken);
        }

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
            await JsonFileWriter.AppendDataToFileAsync(basicMetadata, setOverrideJsonPath, cancellationToken);
            Log.Verbose($"{set.Id}\t'{set.DisplayName}':\tAdded missing Card Set metadatda to override file at {setOverrideJsonPath}");
        }

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
        if (File.Exists(savedJsonFilePath))
        {
            var cardsFromMasterMetadata = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(savedJsonFilePath, cancellationToken);
            cardSetMetadata.Cards ??= cardsFromMasterMetadata.Select(card => card!).ToHashSet();

            foreach (var card in cardsFromMasterMetadata)
            {
                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card.SetId));
            }
        }
    }
}