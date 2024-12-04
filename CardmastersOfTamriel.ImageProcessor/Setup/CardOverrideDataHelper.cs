using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Setup;

public class CardOverrideDataHelper
{
    private Dictionary<string, string> _seriesKeywordDictionary = new();
    private readonly Config _config;
    private readonly CancellationToken _cancellationToken;

    public CardOverrideDataHelper(Config config, CancellationToken cancellationToken)
    {
        _config = config;
        _cancellationToken = cancellationToken;
    }

    public async Task<HashSet<CardSetHandlerOverrideData>> CreateNewOverrideDataFromCardSets(MasterMetadata masterMetadata)
    {
        var seriesKeywordDictionary = LoadSeriesKeywordDictionary(masterMetadata.Metadata);

        var cardSets = masterMetadata.Metadata.Values.SelectMany(series => series.SelectMany(s => s.Sets ?? [])).ToHashSet();

        var data = new HashSet<CardSetHandlerOverrideData>();
        foreach (var set in cardSets)
        {
            var dataOverride = new CardSetHandlerOverrideData
            {
                CardSetId = set.Id,
                CardSeriesId = set.SeriesId,
                Tier = set.Tier,
                NewSetDisplayName = null,
                UseOriginalFileNamesAsDisplayNames = false,
                IgnoreMaximumNumberOfCardsToIncludeLimit = set.Tier == CardTier.Tier4,
                ValueToOverwriteEachCardValue = _config.DefaultCardValues.DefaultValues.GetValueOrDefault(set.Tier),
                ValueToOverwriteEachCardWeight = _config.DefaultCardValues.DefaultWeights.GetValueOrDefault(set.Tier),
                KeywordsToOverwriteEachCardKeywords = _config.DefaultCardValues.DefaultMiscItemKeywords.ToHashSet()
            };

            if (_config.DefaultCardValues.AlwaysIncludeSeriesKeyword)
            {
                var seriesKeyword = seriesKeywordDictionary.GetValueOrDefault(set.SeriesId);
                if (!string.IsNullOrWhiteSpace(seriesKeyword))
                {
                    dataOverride.KeywordsToOverwriteEachCardKeywords.Add(seriesKeyword);
                }
            }

            data.Add(dataOverride);
        }

        return data;
    }

    private static Dictionary<string, string> LoadSeriesKeywordDictionary(Dictionary<CardTier, HashSet<CardSeries>> metadata)
    {
        var seriesKeywordDictionary = metadata.Values
            .SelectMany(series => series)
            .DistinctBy(series => series.Id)
            .ToDictionary(series => series.Id, NamingHelper.CreateKeyword);
        Log.Information("Generated {Count} series keywords", seriesKeywordDictionary.Count);
        return seriesKeywordDictionary;
    }
    
    public async Task<HashSet<CardSetHandlerOverrideData>?> LoadOverridesFromDiskAsync()
    {
        Log.Information("Loading overrides from '{FilePath}'", _config.Paths.SetMetadataOverrideFilePath);

        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (File.Exists(_config.Paths.SetMetadataOverrideFilePath))
            {
                return await JsonFileReader.ReadFromJsonAsync<HashSet<CardSetHandlerOverrideData>>(_config.Paths.SetMetadataOverrideFilePath,
                    _cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load overrides from '{_config.Paths.SetMetadataOverrideFilePath}'");
            throw;
        }

        Log.Information("No overrides exist at '{FilePath}'", _config.Paths.SetMetadataOverrideFilePath);
        return null;
    }
}