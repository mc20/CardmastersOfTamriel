using CardmastersOfTamriel.ImageProcessor.Events;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CompileMasterMetadataHandler : ICardSetHandler
{
    public event EventHandler<SetProgressEventArgs>? ProgressUpdated;

    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "set_metadata.json");
        if (File.Exists(destinationCardSetJsonFilePath))
        {
            var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(destinationCardSetJsonFilePath,
                cancellationToken);
            foreach (var card in cards)
            {
                EventBroker.PublishSetProgress(this, new SetProgressEventArgs(card.SetId));
            }
        }
        else
        {
            Log.Error($"{set.Id}\tNo set metadata file found at {destinationCardSetJsonFilePath}");
        }
    }
}