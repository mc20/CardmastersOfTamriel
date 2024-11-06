using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Processors;

public class CardSeriesProcessor
{
    private readonly Config _config;
    private readonly MasterMetadataHandler _handler;

    public CardSeriesProcessor(Config config, MasterMetadataHandler handler)
    {
        _config = config;
        _handler = handler;
    }

    public void ProcessSeriesFolder(CardTier tier, string seriesSourceFolderPath,
        string tierDestinationFolderPath, ICardSetProcessor cardSetProcessor)
    {
        Log.Information($"Examining Source Series folder: '{seriesSourceFolderPath}'");

        var seriesId = Path.GetFileName(seriesSourceFolderPath);

        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);
        FileOperations.EnsureDirectoryExists(seriesDestinationFolderPath);

        _handler.WriteMetadataToFile(); // Save progress

        Log.Verbose($"Determining folder grouping at path: '{seriesDestinationFolderPath}'");

        var groupedFolders = DetermineFolderGrouping(seriesSourceFolderPath);

        if (groupedFolders.Count == 0)
        {
            Log.Warning($"No folder groups found in '{seriesSourceFolderPath}'");
            _handler.WriteMetadataToFile();
            return;
        }

        var cardSeriesFromSource =
            GetOrCreateCardSeriesFromSource(tier, seriesId, seriesSourceFolderPath, tierDestinationFolderPath);

        _handler.Metadata.Series?.RemoveAll(series => series.Id == seriesId);
        _handler.Metadata.Series?.Add(cardSeriesFromSource);

        // This also updates the handler, FYI
        var replicator = new CardSetReplicator(_handler, seriesId);
        replicator.HandleDestinationSetCreation(groupedFolders);

        ProcessCardSets(cardSetProcessor);

        _handler.WriteMetadataToFile();
    }

    private void ProcessCardSets(ICardSetProcessor cardSetProcessor)
    {
        if (_handler.Metadata.Series is null) return;

        foreach (var series in _handler.Metadata.Series.Where(series => series.Sets is not null))
        {
            if (series.Sets is null || series.Sets.Count == 0)
            {
                Log.Warning($"No CardSets found for {series.Id} from Metadata");
                continue;
            }

            Log.Verbose($"Processing card sets for Series Folder at '{series.DestinationFolderPath}'");

            foreach (var set in series.Sets)
            {
                cardSetProcessor.ProcessSetAndImages(set);
            }
        }
    }

    private CardSeries GetOrCreateCardSeriesFromSource(CardTier tier, string seriesId, string seriesSourceFolderPath,
        string tierDestinationFolderPath)
    {
        var seriesDestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

        CardSeries? existingSeriesMetadata = null;

        const string seriesMetadataFileName = "series_metadata.json";
        var seriesDestinationMetadataFilePath = Path.Combine(seriesDestinationFolderPath, seriesMetadataFileName);
        if (File.Exists(seriesDestinationMetadataFilePath))
        {
            try
            {
                // If we have an existing metadata, we'll use this instead
                existingSeriesMetadata = JsonFileReader.ReadFromJson<CardSeries>(seriesDestinationMetadataFilePath);
                Log.Verbose(
                    $"Found existing Series Metadata file at Destination Path: '{seriesDestinationMetadataFilePath}'");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    $"Could not convert the the Source Metadata file at '{seriesDestinationMetadataFilePath}' to a CardSeries");
            }
        }
        else
        {
            Log.Verbose(
                $"Did not find an existing Series Metadata file at Destination Path: '{seriesDestinationMetadataFilePath}'");

            var seriesSourceMetadataFilePath = Path.Combine(seriesSourceFolderPath, seriesMetadataFileName);
            if (File.Exists(seriesSourceMetadataFilePath))
            {
                try
                {
                    existingSeriesMetadata = JsonFileReader.ReadFromJson<CardSeries>(seriesSourceMetadataFilePath);
                    Log.Verbose(
                        $"Found existing Series Metadata file at Source Path: '{seriesSourceMetadataFilePath}'");
                }
                catch (Exception e)
                {
                    Log.Error(e,
                        $"Could not convert the the Source Metadata file at '{seriesSourceMetadataFilePath}' to a CardSeries");
                }
            }
        }

        if (existingSeriesMetadata == null)
        {
            Log.Verbose("Did not find series in existing metadata and now creating and returning new CardSeries");
            existingSeriesMetadata = new CardSeries()
            {
                DisplayName = NameHelper.FormatDisplayNameFromId(seriesId),
                Theme = "",
                ReleaseDate = DateTime.UtcNow,
                Artist = "",
                IsLimitedEdition = false,
                Description = string.Empty,
            };
        }

        existingSeriesMetadata.Id = seriesId;
        existingSeriesMetadata.Tier = tier;
        existingSeriesMetadata.Sets = [];
        existingSeriesMetadata.SourceFolderPath = seriesSourceFolderPath;
        existingSeriesMetadata.DestinationFolderPath = Path.Combine(tierDestinationFolderPath, seriesId);

        _handler.Metadata.Series ??= [];
        _handler.Metadata.Series.Add(existingSeriesMetadata);

        var serializedJson = JsonSerializer.Serialize(existingSeriesMetadata, JsonSettings.Options);
        File.WriteAllText(seriesDestinationMetadataFilePath, serializedJson);
        Log.Information($"Serialized Card Series metadata written to {seriesDestinationMetadataFilePath}");

        return existingSeriesMetadata;
    }

    private static Dictionary<string, List<string>> DetermineFolderGrouping(string seriesSourceFolderPath)
    {
        var groupedFolders = new Dictionary<string, List<string>>();

        foreach (var setSourceFolderPath in Directory.EnumerateDirectories(seriesSourceFolderPath))
        {
            CardSetHelper.GroupAndNormalizeFolderNames(setSourceFolderPath, groupedFolders);
        }

        return groupedFolders;
    }
}