using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CompileMasterMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var setMetadataFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "set_metadata.json");
        if (File.Exists(setMetadataFilePath))
        {
            var cardSet = await JsonFileReader.ReadFromJsonAsync<CardSet>(setMetadataFilePath, cancellationToken);
            if (cardSet.Cards is not null)
            {
                foreach (var card in cardSet.Cards)
                {
                    EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card.SetId));
                }
            }
        }
        else
        {
            Log.Error($"{set.Id}\tNo set metadata file found at {setMetadataFilePath}");
        }
    }
}