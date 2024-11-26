using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class CompileMasterMetadataHandler : ICardSetHandler
{
    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken, CardSetBasicMetadata? setOverride = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var setmetadatajson = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
        if (File.Exists(setmetadatajson))
        {
            var cardSet = await JsonFileReader.ReadFromJsonAsync<CardSet>(setmetadatajson, cancellationToken);

            var cardsjsonl = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
            if (File.Exists(cardsjsonl))
            {
                if (setOverride is not null)
                {
                    Log.Information($"{set.Id}\t'{set.DisplayName}':\tRefreshing data from set override file");
                    cardSet.OverrideWith(setOverride);
                }

                var cards = await JsonFileReader.LoadAllFromJsonLineFileAsync<Card>(cardsjsonl, cancellationToken);
                foreach (var card in cards)
                {
                    cardSet.Cards ??= [];
                    cardSet.Cards.Add(card);

                    EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card));
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
