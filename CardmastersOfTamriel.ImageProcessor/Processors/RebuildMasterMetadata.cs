using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Factories;
using CardmastersOfTamriel.ImageProcessor.Providers;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public class RebuildMasterMetadata : ICardSetHandler
{
    public void ProcessCardSet(CardSet set)
    {
        var handler = MasterMetadataProvider.Instance.MetadataHandler;

        set.Cards ??= [];
        set.Cards.Clear();

        var savedJsonFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl");
        var savedJsonBackupFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "cards.jsonl.backup");

        if (File.Exists(savedJsonFilePath))
        {
            File.Move(savedJsonFilePath, savedJsonBackupFilePath, true);
            File.Delete(savedJsonFilePath);
        }
        else
        {
            Log.Warning($"{set.Id}\tNo cards.jsonl file found at '{savedJsonFilePath}'");
        }

        Log.Information($"{set.Id}\t'{set.DisplayName}':\tProcessing from Source Path: '{set.SourceAbsoluteFolderPath}'");

        var rebuildlist = JsonFileReader.ReadFromJson<Dictionary<string, string>>(ConfigurationProvider.Instance.Config.Paths.RebuildListFilePath);
        if (rebuildlist != null && rebuildlist.Count > 0)
        {
            if (!rebuildlist.TryGetValue(set.Id, out var seriesId) || seriesId != set.SeriesId)
            {
                Log.Information($"{set.Id}\tSkipping rebuild as set is not in rebuild list or series ID does not match");
                return;
            }
        }

        var jsonlPath = ConfigurationProvider.Instance.Config.Paths.SetMetadataOverrideFilePath;
        var setMetadataOverride = FileOperations.FindMetadataLineBySetId<CardSetBasicMetadata>(jsonlPath, set.Id);
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
            var basicMetadata = set.GetBasicMetadata();
            basicMetadata.DefaultKeywords = ConfigurationProvider.Instance.Config.General.DefaultMiscItemKeywords;
            FileOperations.AppendDataToFile<CardSetBasicMetadata>(basicMetadata, jsonlPath);
        }

        var imageFilePathsAtDestination =
            CardSetImageHelper.GetImageFilePathsFromFolder(set.DestinationAbsoluteFolderPath, ["*.dds"]);
        Log.Information($"{set.Id}\tFound {imageFilePathsAtDestination.Count} DDS images at destination path");

        var imageFilePathsAtSource = CardSetImageHelper.GetImageFilePathsFromFolder(set.SourceAbsoluteFolderPath).OrderBy(file => Path.GetFileNameWithoutExtension(file)).ToHashSet();
        var cardsFromSource = CardFactory.CreateCardsFromImagesAtFolderPath(set, imageFilePathsAtSource, true);
        Log.Information($"{set.Id}\tCreated {cardsFromSource.Count} cards from source images");

        var validUniqueIdentifiersDeterminedFromSource = cardsFromSource.Select(card => card.Id).ToHashSet();
        Log.Information($"{set.Id}\tFound {validUniqueIdentifiersDeterminedFromSource.Count} unique identifiers from source images");

        var uniqueIdentifiersAtDestination = imageFilePathsAtDestination.Select(Path.GetFileNameWithoutExtension).ToHashSet();

        Log.Information($"{set.Id}\tFound {uniqueIdentifiersAtDestination.Count} unique identifiers from destination images");

        var validIdentifiersAtDestination = uniqueIdentifiersAtDestination.Intersect(validUniqueIdentifiersDeterminedFromSource).ToHashSet();

        Log.Information($"{set.Id}\tFound {validIdentifiersAtDestination.Count} valid unique identifiers at destination");

        Log.Information($"{set.Id}\tUpdating card metadata to be saved to '{savedJsonFilePath}'");

        var displayedIndex = 1;
        var displayedTotalCount = validIdentifiersAtDestination.Count;
        var maxDisplayNameLength = 0;
        foreach (var cardWithIndex in cardsFromSource.OrderBy(card => card.Id).Select((card, index) => (card, index)))
        {
            if (cardWithIndex.card.SourceAbsoluteFilePath != null)
            {
                cardWithIndex.card.Shape ??= ImageHelper.DetermineOptimalShape(cardWithIndex.card.SourceAbsoluteFilePath);
            }

            if (validIdentifiersAtDestination.Contains(cardWithIndex.card.Id))
            {
                cardWithIndex.card.DestinationAbsoluteFilePath = imageFilePathsAtDestination.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == cardWithIndex.card.Id);
                cardWithIndex.card.DestinationRelativeFilePath = FilePathHelper.GetRelativePath(cardWithIndex.card.DestinationAbsoluteFilePath, set.Tier);
                cardWithIndex.card.DisplayedIndex = (uint)displayedIndex;
                cardWithIndex.card.DisplayedTotalCount = (uint)validIdentifiersAtDestination.Count;
                cardWithIndex.card.TrueTotalCount = (uint)validUniqueIdentifiersDeterminedFromSource.Count;
                cardWithIndex.card.SetGenericDisplayName();
                displayedIndex++;

                if (maxDisplayNameLength < cardWithIndex.card.DisplayName?.Length) maxDisplayNameLength = cardWithIndex.card.DisplayName?.Length ?? 0;
            }
            else
            {
                cardWithIndex.card.DisplayName = null;
                cardWithIndex.card.DestinationAbsoluteFilePath = null;
                cardWithIndex.card.DestinationRelativeFilePath = null;
                cardWithIndex.card.DisplayedIndex = 0;
                cardWithIndex.card.DisplayedTotalCount = 0;
            }

            Log.Verbose($"{set.Id}\tRefreshed metadata for Card '{cardWithIndex.card.Id}' -> " +
            $"Shape: '{cardWithIndex.card.Shape}'{new string(' ', NameHelper.MaxCardShapeTextLength - (cardWithIndex.card.Shape?.ToString().Length ?? 0))}\t" +
            $"SourceAbsoluteFilePath: '{cardWithIndex.card.SourceAbsoluteFilePath}'\t" +
            $"DisplayName: '{cardWithIndex.card.DisplayName}'{new string(' ', maxDisplayNameLength - (cardWithIndex.card.DisplayName?.Length ?? 0))}\t" +
            $"DestinationRelativeFilePath: '{cardWithIndex.card.DestinationRelativeFilePath}'\t");

        }

        foreach (var card in cardsFromSource.OrderBy(card => card.Id))
        {
            FileOperations.AppendDataToFile(card, savedJsonFilePath);
        }

        if (cardsFromSource.All(card => card.DestinationAbsoluteFilePath == null))
        {
            Log.Error($"{set.Id}\tThere were no cards saved to the metadata file having destination file paths.");
        }

        var destinationSetMetadataFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, "set_metadata.json");
        var serializedJson = JsonSerializer.Serialize(set, JsonSettings.Options);
        File.WriteAllText(destinationSetMetadataFilePath, serializedJson);
        Log.Information($"{set.Id}\tUpdated metadata written to {destinationSetMetadataFilePath}");

        var cardSetMetadata = handler.Metadata.Series?.SelectMany(series => series.Sets ?? [])
            .FirstOrDefault(s => s.Id == set.Id);

        if (cardSetMetadata == null) return;

        cardSetMetadata.Cards = cardsFromSource;

        handler.WriteMetadataToFile();

    }
}