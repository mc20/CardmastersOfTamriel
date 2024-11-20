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

        var setmetadatajson = Path.Combine(set.DestinationAbsoluteFolderPath, "set_metadata.json");
        if (File.Exists(setmetadatajson))
        {
            var cardSet = await JsonFileReader.ReadFromJsonAsync<CardSet>(setmetadatajson, cancellationToken);

            var cardsjsonl = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
            if (File.Exists(cardsjsonl))
            {
                var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(cardsjsonl, cancellationToken);
                foreach (var card in cards)
                {
                    cardSet.Cards ??= [];
                    cardSet.Cards.Add(card);

                    EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card.SetId));
                }
                
                await JsonFileWriter.WriteToJsonAsync(cardSet, setmetadatajson, cancellationToken);
            }
        }
        else
        {
            Log.Error($"{set.Id}\tNo set metadata file found at {setmetadatajson}");
        }
    }
}