using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class ApplyOverrideDataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardSetHandlerOverrideData? overrideData = null)
    {
        if (set.Cards != null)
        {
            foreach (var card in set.Cards)
            {
                if (overrideData is not null)
                {
                    var isOverwritten = card.OverwriteWith(overrideData);
                    if (isOverwritten) Log.Information($"[{overrideData.CardSetId}]:\tOverwrote card {card.Id} with override data");
                }

                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
            }

            await JsonFileWriter.WriteToJsonAsync(set, Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson), 
                cancellationToken);
        }
    }
}