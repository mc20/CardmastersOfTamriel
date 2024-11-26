using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class OverrideSetMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardSetBasicMetadata? setOverride = null)
    {
        var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (!File.Exists(destinationCardSetJsonFilePath))
        {
            Log.Error($"{set.Id}\tNo set metadata file found at {destinationCardSetJsonFilePath}");
            return;
        }

        var cardSetMetadata = await JsonFileReader.ReadFromJsonAsync<CardSet>(destinationCardSetJsonFilePath, cancellationToken);

        if (setOverride is not null)
        {
            Log.Information($"{set.Id}\t'{set.DisplayName}':\tRefreshing data from set override file");
            cardSetMetadata.OverrideWith(setOverride);
        }

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
        if (File.Exists(savedJsonFilePath))
        {
            var cardsFromMasterMetadata = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(savedJsonFilePath, cancellationToken);
            cardSetMetadata.Cards ??= cardsFromMasterMetadata.Select(card => card!).ToHashSet();

            foreach (var card in cardsFromMasterMetadata)
            {
                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
            }
        }
    }
}