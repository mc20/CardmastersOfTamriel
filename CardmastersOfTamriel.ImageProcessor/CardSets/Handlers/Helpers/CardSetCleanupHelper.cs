using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;

public static class CardSetCleanupHelper
{
    public static async Task RemoveCardsWithNoSourceAbsoluteFilePathAsync(CardSet set, CancellationToken cancellationToken)
    {
        Log.Information($"[{set.Id}]\tRemoving Cards with no SourceAbsoluteFilePath [total cards: {set.Cards?.Count}]");

        if (set.Cards is null) return;

        // these cards lost their sources or the sources do not exist and can't be relied on
        // they should be not be considered as existing destination cards 
        var cardsToDelete = set.Cards.Where(c => string.IsNullOrWhiteSpace(c.SourceAbsoluteFilePath) || !File.Exists(c.SourceAbsoluteFilePath)).ToHashSet();

        // delete DDS images having no absolute source file path in the json file
        foreach (var card in cardsToDelete)
        {
            if (File.Exists(card.DestinationAbsoluteFilePath))
            {
                Log.Information($"[{set.Id}]\tDeleting Card {card.Id} as it has no SourceAbsoluteFilePath");
                File.Delete(card.DestinationAbsoluteFilePath);
            }
            else
            {
                Log.Information($"[{set.Id}]\tCard {card.Id} does not exist at destination path");
            }

            set.Cards.Remove(card);
        }

        await JsonFileWriter.WriteToJsonAsync(set, Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson),
            cancellationToken);
    }

    public static async Task RemoveCardsWithNoExistingFileAtDestination(CardSet set, CancellationToken cancellationToken)
    {
        Log.Information($"[{set.Id}]\tRemoving Cards with no destination file path [total cards: {set.Cards?.Count}]");

        if (set.Cards is null) return;

        var missingConvertedImages = set.Cards
            .Where(c => !string.IsNullOrEmpty(c.DestinationAbsoluteFilePath) && !File.Exists(c.DestinationAbsoluteFilePath)).ToHashSet();
        foreach (var card in missingConvertedImages)
        {
            Log.Warning($"[{set.Id}]\tCard {card.Id} is missing a converted image");
            set.Cards.Remove(card);
        }

        await JsonFileWriter.WriteToJsonAsync(set, Path.Combine(set.DestinationAbsoluteFolderPath, PathSettings.DefaultFilenameForSetMetadataJson),
            cancellationToken);
    }
}