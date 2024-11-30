using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CompileMasterMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken,
        CardSetHandlerOverrideData? overrideData = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var jsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (File.Exists(jsonFilePath))
        {
            var cardSetFromJson = await JsonFileReader.ReadFromJsonAsync<CardSet>(jsonFilePath, cancellationToken);
            cardSetFromJson.Cards ??= [];

            set.OverwriteWith(cardSetFromJson);

            foreach (var card in cardSetFromJson.Cards)
            {
                if (overrideData is not null)
                {
                    var isOverwritten = card.OverwriteWith(overrideData);
                    if (isOverwritten) Log.Information("Overwrote card {CardId} with override data", card.Id);
                }

                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
            }

            set.Cards = cardSetFromJson.Cards.ToHashSet();

            await JsonFileWriter.WriteToJsonAsync(cardSetFromJson, jsonFilePath, cancellationToken);
        }
        else
        {
            Log.Error($"{set.Id}\tNo CardSet metadata file found at {jsonFilePath}");
        }
    }
}