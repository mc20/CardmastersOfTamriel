using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;
using ZXing;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class RebuildMasterMetadataHandler : ICardSetHandler
{
    private readonly Config _config;

    public RebuildMasterMetadataHandler(Config config)
    {
        _config = config;
    }

    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken,
        CardOverrideData? overrideData = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            set.Cards ??= [];
            set.Cards.Clear();

            BackupExistingCardJsonLineFile(set);

            var savedJsonLineFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
            Log.Verbose($"{set.Id}\tUpdating Card metadata to be saved to '{savedJsonLineFilePath}'");

            var data = RebuildMasterMetadataData.Load(_config, set, cancellationToken);
            data.LogDataAsInformation();
            
            // Rebuilds cards.jsonl and set_metadata.json
            
            // the json data is based on existing images in the destination folder

            var totalCardCountToDisplayOnCard = data.ValidIdentifiersAtDestination.Count;
            var formatter = new CardMetadataUpdater(this, data, _config, (uint)totalCardCountToDisplayOnCard);

            var displayedIndex = 1;
            var maxDisplayNameLength = 0;
            foreach (var card in data.CardsFromSource.OrderBy(card => card.Id))
            {
                formatter.UpdateCardMetadataAndPublishHandlingProgress(card, ref displayedIndex, ref maxDisplayNameLength, cancellationToken);
                if (overrideData is not null)
                {
                    var isOverwritten = card.OverwriteWith(overrideData);
                    if (isOverwritten) Log.Information("Overwrote card {CardId} with override data", card.Id);
                }
                set.Cards.Add(card);
            }

            foreach (var card in data.CardsFromSource.OrderBy(card => card.Id))
            {
                // Refresh the jsonl file with any new changes
                await JsonFileWriter.AppendDataToJsonLineFileAsync(card, savedJsonLineFilePath, cancellationToken);
            }

            if (data.CardsFromSource.All(card => card.DestinationAbsoluteFilePath == null))
            {
                Log.Warning($"{set.Id}\tThere were no Cards saved to the metadata file having destination file paths.");
            }

            // Refresh the set metadata file with any new changes
            var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson);
            await JsonFileWriter.WriteToJsonAsync(set, destinationCardSetJsonFilePath, cancellationToken);
            Log.Verbose($"{set.Id}\tUpdated metadata written to {destinationCardSetJsonFilePath}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{set.Id}\tFailed to process Card set");
            throw;
        }
    }

    private static void BackupExistingCardJsonLineFile(CardSet set)
    {
        var savedJsonLineFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);
        var savedJsonLineBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonlBackup);
        
        if (!File.Exists(savedJsonLineFilePath)) return;
        
        File.Move(savedJsonLineFilePath, savedJsonLineBackupFilePath, true);
        File.Delete(savedJsonLineFilePath);
        Log.Information("Backed up existing Card metadata file to '{SavedJsonLineBackupFilePath}'", savedJsonLineBackupFilePath);
    }
}