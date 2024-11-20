using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers;

public class RebuildMasterMetadataHandler : ICardSetHandler
{
    // TODO: Implement more reliable and robust tracking of index and display name length
    private int _displayedIndex = 1;
    private int _maxDisplayNameLength = 0;


    public async Task ProcessCardSetAsync(CardSet set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            set.Cards ??= [];
            set.Cards.Clear();

            await HandleSavedCardDataAsync(set, cancellationToken);
            await UpdateSetMetadataAsync(set, cancellationToken);

            var savedJsonLineFilePath = GetSavedJsonLineFilePath(set);
            Log.Verbose($"{set.Id}\tUpdating card metadata to be saved to '{savedJsonLineFilePath}'");

            var data = RebuildMasterMetadataData.Load(set, cancellationToken);
            data.LogInformation();

            _displayedIndex = 1;
            _maxDisplayNameLength = 0;
            foreach (var card in data.CardsFromSource.OrderBy(card => card.Id))
            {
                HandleCard(data, card, cancellationToken);
                set.Cards.Add(card);
            }

            foreach (var card in data.CardsFromSource.OrderBy(card => card.Id))
            {
                // Refresh the jsonl file with any new changes
                await JsonFileWriter.AppendDataToFileAsync(card, savedJsonLineFilePath, cancellationToken);
            }

            if (data.CardsFromSource.All(card => card.DestinationAbsoluteFilePath == null))
            {
                Log.Error($"{set.Id}\tThere were no cards saved to the metadata file having destination file paths.");
            }

            // Refresh the set metadata file with any new changes
            var destinationCardSetJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "set_metadata.json");
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
        Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");

    private static async Task HandleSavedCardDataAsync(CardSet set, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var savedJsonLineFilePath = GetSavedJsonLineFilePath(set);
            var savedJsonLineBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl.backup");

            if (File.Exists(savedJsonLineFilePath))
            {
                File.Move(savedJsonLineFilePath, savedJsonLineBackupFilePath, true);
                File.Delete(savedJsonLineFilePath);
            }
            else
            {
                Log.Warning($"{set.Id}\tNo cards.jsonl file found at '{savedJsonLineFilePath}'");
            }

            Log.Information(
                $"{set.Id}\t'{set.DisplayName}':\tProcessing from Source Path: '{set.SourceAbsoluteFolderPath}'");

            var rebuildlist =
                await JsonFileReader.ReadFromJsonAsync<Dictionary<string, string>>(
                    ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath, cancellationToken);
            if (rebuildlist.Count > 0)
            {
                if (!rebuildlist.TryGetValue(set.Id, out var seriesId) || seriesId != set.SeriesId)
                {
                    Log.Information(
                        $"{set.Id}\tSkipping rebuild as set is not in rebuild list or series ID does not match");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{set.Id}\tFailed to handle saved card data");
            throw;
        }
    }

    private static async Task UpdateSetMetadataAsync(CardSet set, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CardSetBasicMetadata? setMetadataOverride = null;
        var jsonlPath = ConfigurationProvider.Instance.Config.Paths.SetMetadataOverrideFilePath;
        if (File.Exists(jsonlPath))
        {
            setMetadataOverride =
                await JsonFileReader.FindMetadataLineBySetIdAsync<CardSetBasicMetadata>(jsonlPath, set.Id,
                    cancellationToken);
        }

        if (setMetadataOverride is not null)
        {
            set.DisplayName = setMetadataOverride?.DisplayName ?? set.DisplayName;
            set.DefaultValue = setMetadataOverride?.DefaultValue ?? set.DefaultValue;
            set.DefaultWeight = setMetadataOverride?.DefaultWeight ?? set.DefaultWeight;
            set.DefaultKeywords = setMetadataOverride?.DefaultKeywords ?? set.DefaultKeywords;
        }
        else
        {
            // Add to the jsonl if the set isn't there for convenience
            // var basicMetadata = set.GetBasicMetadata();
            // basicMetadata.DefaultKeywords = ConfigurationProvider.Instance.Config.General.DefaultMiscItemKeywords;
            // await JsonFileWriter.AppendDataToFileAsync(basicMetadata, jsonlPath, cancellationToken);
        }
    }

    private void HandleCard(RebuildMasterMetadataData data, Card card, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (card.SourceAbsoluteFilePath != null)
        {
            card.Shape ??= CardShapeHelper.DetermineOptimalShape(card.SourceAbsoluteFilePath);
        }

        if (data.ValidIdentifiersAtDestination.Contains(card.Id))
        {
            card.DestinationAbsoluteFilePath = data.ImageFilePathsAtDestination.FirstOrDefault(file =>
                Path.GetFileNameWithoutExtension(file) == card.Id);
            card.DestinationRelativeFilePath =
                FilePathHelper.GetRelativePath(card.DestinationAbsoluteFilePath, card.Tier);
            card.DisplayedIndex = (uint)_displayedIndex;
            card.DisplayedTotalCount = (uint)data.ValidIdentifiersAtDestination.Count;
            card.TrueTotalCount = (uint)data.ValidUniqueIdentifiersDeterminedFromSource.Count;
            card.SetGenericDisplayName();
            _displayedIndex++;

            if (_maxDisplayNameLength < card.DisplayName?.Length)
                _maxDisplayNameLength = card.DisplayName?.Length ?? 0;
        }
        else
        {
            card.DisplayName = null;
            card.DestinationAbsoluteFilePath = null;
            card.DestinationRelativeFilePath = null;
            card.DisplayedIndex = 0;
            card.DisplayedTotalCount = 0;
        }

        EventBroker.PublishSetHandlingProgress(this, new ProgressTrackingEventArgs(card.SetId));

        if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
        {
            Log.Debug($"{card.SetId}\tRefreshed metadata for Card '{card.Id}' -> " +
                      $"Shape: '{card.Shape}'{NameHelper.PadString(card.Shape?.ToString(), NameHelper.MaxCardShapeTextLength)}\t" +
                      $"SourceAbsoluteFilePath: '{card.SourceAbsoluteFilePath}'\t" +
                      $"DisplayName: '{card.DisplayName}'{NameHelper.PadString(card.DisplayName, _maxDisplayNameLength)}\t" +
                      $"DestinationRelativeFilePath: '{card.DestinationRelativeFilePath}'\t");
        }
    }
}