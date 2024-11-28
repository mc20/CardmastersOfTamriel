using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class OverrideSetMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardOverrideData? overrideData = null)
    {
        var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (!File.Exists(destinationCardSetJsonFilePath))
        {
            Log.Warning($"{set.Id}\tNo set metadata file found at {destinationCardSetJsonFilePath}");
            await JsonFileWriter.WriteToJsonAsync(set, destinationCardSetJsonFilePath, cancellationToken);
        }
        else
        {
            var cardSetMetadataFromFile = await JsonFileReader.ReadFromJsonAsync<CardSet>(destinationCardSetJsonFilePath, cancellationToken);
            set.OverrideWith(cardSetMetadataFromFile);
        }

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
        if (File.Exists(savedJsonFilePath))
        {
            var cardsFromJsonlFile = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(savedJsonFilePath, cancellationToken);
            set.Cards = [];

            foreach (var card in cardsFromJsonlFile)
            {
                if (overrideData is not null)
                {
                    if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                    {
                        Log.Debug("Override data: {OverrideData}", JsonSerializer.Serialize(overrideData));
                        Log.Debug("Card before overwrite: {card}", JsonSerializer.Serialize(card));
                    }
                    
                    var isOverwritten = card.OverwriteWith(overrideData);
                    if (isOverwritten) Log.Information("Overwrote card {CardId} with override data", card.Id);

                    if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
                        Log.Debug("Card after overwrite: {card}", JsonSerializer.Serialize(card));
                }

                set.Cards.Add(card);

                EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
            }

            Log.Verbose("Updated {Count} cards in set {SetId}", set.Cards.Count, set.Id);

            await JsonFileWriter.WriteToJsonAsync(set, destinationCardSetJsonFilePath, cancellationToken);
            await JsonFileWriter.WriteToJsonLineFileAsync(set.Cards, savedJsonFilePath, cancellationToken);
        }
    }
}