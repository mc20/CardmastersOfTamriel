using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CompileMasterMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken,
        CardOverrideData? overrideData = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var setmetadatajsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (File.Exists(setmetadatajsonFilePath))
        {
            var cardSetDataFromFile = await JsonFileReader.ReadFromJsonAsync<CardSet>(setmetadatajsonFilePath, cancellationToken);

            var cardsjsonlFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
            if (File.Exists(cardsjsonlFilePath))
            {
                set.Cards = [];
                cardSetDataFromFile.Cards = [];

                var cardsFromJsonLineFile = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(cardsjsonlFilePath, cancellationToken);
                foreach (var card in cardsFromJsonLineFile)
                {
                    if (overrideData is not null)
                    {
                        var isOverwritten = card.OverwriteWith(overrideData);
                        if (isOverwritten) Log.Information("Overwrote card {CardId} with override data", card.Id);
                    }
                    cardSetDataFromFile.Cards.Add(card);
                    set.Cards.Add(card);

                    EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
                }

                await JsonFileWriter.WriteToJsonAsync(cardSetDataFromFile, setmetadatajsonFilePath, cancellationToken);
            }
            else
            {
                Log.Error($"{set.Id}\tNo Cards jsonl file found at {cardsjsonlFilePath}");
            }
        }
        else
        {
            Log.Error($"{set.Id}\tNo CardSet metadata file found at {setmetadatajsonFilePath}");
        }
    }
}