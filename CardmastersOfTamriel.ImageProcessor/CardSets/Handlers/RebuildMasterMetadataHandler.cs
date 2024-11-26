using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;
using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class RebuildMasterMetadataHandler : ICardSetHandler
{
    private readonly Config _config;

    public RebuildMasterMetadataHandler(Config config)
    {
        _config = config;
    }

    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken,
        CardSetBasicMetadata? setOverride = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            set.Cards ??= [];
            set.Cards.Clear();

            await HandleSavedCardDataAsync(set, cancellationToken);

            var savedJsonLineFilePath = GetSavedJsonLineFilePath(set);
            Log.Verbose($"{set.Id}\tUpdating card metadata to be saved to '{savedJsonLineFilePath}'");

            if (setOverride is not null)
            {
                Log.Information($"{set.Id}\t'{set.DisplayName}':\tRefreshing data from set override file");
                set.OverrideWith(setOverride);
            }

            var data = RebuildMasterMetadataData.Load(set, cancellationToken);
            data.LogInformation();

            var totalCardCountToDisplayOnCard = data.ValidIdentifiersAtDestination.Count;
            var formatter = new CardMetadataUpdater(this, set, data, _config, (uint)totalCardCountToDisplayOnCard);

            var displayedIndex = 1;
            var maxDisplayNameLength = 0;
            foreach (var card in data.CardsFromSource.OrderBy(card => card.Id))
            {
                formatter.UpdateCardMetadataAndPublishHandlingProgress(card, ref displayedIndex,
                    ref maxDisplayNameLength, cancellationToken);
                set.Cards.Add(card);
            }

            foreach (var card in data.CardsFromSource.OrderBy(card => card.Id))
            {
                // Refresh the jsonl file with any new changes
                await JsonFileWriter.AppendDataToJsonLineFileAsync(card, savedJsonLineFilePath, cancellationToken);
            }

            if (data.CardsFromSource.All(card => card.DestinationAbsoluteFilePath == null))
            {
                Log.Warning($"{set.Id}\tThere were no cards saved to the metadata file having destination file paths.");
            }

            // Refresh the set metadata file with any new changes
            var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath,
                PathSettings.DefaultFilenameForSetMetadataJson);
            await JsonFileWriter.WriteToJsonAsync(set, destinationCardSetJsonFilePath, cancellationToken);
            Log.Verbose($"{set.Id}\tUpdated metadata written to {destinationCardSetJsonFilePath}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{set.Id}\tFailed to process card set");
            throw;
        }
    }

    private static string GetSavedJsonLineFilePath(CardSet set) =>
        Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForCardsJsonl);

    private async Task HandleSavedCardDataAsync(CardSet set, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var savedJsonLineFilePath = GetSavedJsonLineFilePath(set);
            var savedJsonLineBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath,
                PathSettings.DefaultFilenameForCardsJsonlBackup);

            if (File.Exists(savedJsonLineFilePath))
            {
                File.Move(savedJsonLineFilePath, savedJsonLineBackupFilePath, true);
                File.Delete(savedJsonLineFilePath);
            }
            else
            {
                Log.Warning(
                    $"{set.Id}\tNo {PathSettings.DefaultFilenameForCardsJsonl} file found at '{savedJsonLineFilePath}'");
            }

            Log.Information(
                $"{set.Id}\t'{set.DisplayName}':\tProcessing from Source Path: '{set.SourceAbsoluteFolderPath}'");

            if (File.Exists(_config.Paths.RebuildListFilePath))
            {
                var rebuildlist =
                    await JsonFileReader.ReadFromJsonAsync<Dictionary<string, string>>(
                        _config.Paths.RebuildListFilePath, cancellationToken);
                if (rebuildlist.Count > 0)
                {
                    if (!rebuildlist.TryGetValue(set.Id, out var seriesId) || seriesId != set.SeriesId)
                    {
                        Log.Information(
                            $"{set.Id}\tSkipping rebuild as set is not in rebuild list or series ID does not match");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{set.Id}\tFailed to handle saved card data");
            throw;
        }
    }
}