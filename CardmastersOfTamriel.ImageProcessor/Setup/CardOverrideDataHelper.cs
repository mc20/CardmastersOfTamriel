using System.Collections.Concurrent;
using System.Text.Json;
using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Setup;

public class CardOverrideDataHelper
{
    private Dictionary<string,string> _seriesKeywordDictionary = new();
    private readonly Config _config;
    private readonly CancellationToken _cancellationToken;

    public CardOverrideDataHelper(Config config, CancellationToken cancellationToken)
    {
        _config = config;
        _cancellationToken = cancellationToken;
    }
    
    private void LoadSeriesKeywordDictionary(Dictionary<CardTier, HashSet<CardSeries>> metadata)
    {
        _seriesKeywordDictionary = metadata.Values
            .SelectMany(series => series)
            .DistinctBy(series => series.Id)
            .ToDictionary(series => series.Id, NamingHelper.CreateKeyword);
        Log.Information("Generated {Count} series keywords", _seriesKeywordDictionary.Count);
    }

    /// <summary>
    /// Creates and writes a new override file to disk asynchronously.
    /// </summary>
    /// <param name="masterMetadataFilePath">
    /// The file path to the master metadata file. If null or whitespace, the default path from the configuration will be used.
    /// </param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <exception cref="Exception">Thrown if there is an error during the creation of the override file.</exception>
    public async Task CreateAndWriteNewOverrideFileToDiskAsync(string? masterMetadataFilePath = null)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(masterMetadataFilePath)) masterMetadataFilePath = _config.Paths.MasterMetadataFilePath;

        if (File.Exists(masterMetadataFilePath))
        {
            Log.Information("Creating overrides from master metadata file '{MasterMetadataFilePath}'",
                masterMetadataFilePath);

            try
            {
                var metadata =
                    await JsonFileReader.ReadFromJsonAsync<Dictionary<CardTier, HashSet<CardSeries>>>(
                        masterMetadataFilePath, _cancellationToken);

                LoadSeriesKeywordDictionary(metadata);
                
                var allOverrides = new ConcurrentDictionary<CardTier, ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>>();
                
                foreach (CardTier tier in Enum.GetValues(typeof(CardTier)))
                {
                    var seriesMetadataByTier = metadata.GetValueOrDefault(tier);
                    if (seriesMetadataByTier is null) continue;

                    var overridesSeriesLevel = new ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>();
                    foreach (var series in seriesMetadataByTier.OrderBy(s => s.Id))
                    {
                        overridesSeriesLevel[series.Id] = SetupCardSetOverrideData(series);
                    }

                    allOverrides[tier] = overridesSeriesLevel;
                }

                await JsonFileWriter.WriteToJsonAsync(allOverrides, _config.Paths.SetMetadataOverrideFilePath, _cancellationToken);
                Log.Information($"New CardSeries metadata override file created at '{_config.Paths.SetMetadataOverrideFilePath}'");
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to create overrides from master metadata file");
                throw;
            }
        }
        else
        {
            Log.Warning("No Master metadata file exists to create the overrides file");
        }
    }

    private ConcurrentDictionary<string, CardSetHandlerOverrideData> SetupCardSetOverrideData(CardSeries series)
    {
        if (series.Sets is null) return [];
        
        var overridesSetsLevel = new ConcurrentDictionary<string, CardSetHandlerOverrideData>();
        Log.Information("Processing {Count} card sets", series.Sets.Count);

        foreach (var cs in series.Sets.OrderBy(s => s.Id))
        {
            var data = new CardSetHandlerOverrideData
            {
                CardSetId = cs.Id,
                CardSeriesId = cs.SeriesId,
                NewSetDisplayName = null,
                UseOriginalFileNamesAsDisplayNames = false,
                IgnoreMaximumNumberOfCardsToIncludeLimit = cs.Tier == CardTier.Tier4,
                ValueToOverwriteEachCardValue = _config.DefaultCardValues.DefaultValues.GetValueOrDefault(cs.Tier),
                ValueToOverwriteEachCardWeight = _config.DefaultCardValues.DefaultWeights.GetValueOrDefault(cs.Tier),
                KeywordsToOverwriteEachCardKeywords = _config.DefaultCardValues.DefaultMiscItemKeywords.ToHashSet()
            };

            if (_config.DefaultCardValues.AlwaysIncludeSeriesKeyword)
            {
                Log.Verbose("Adding series keyword '{SeriesKeyword}' to default keywords", cs.SeriesKeyword);
                var seriesKeyword = _seriesKeywordDictionary.GetValueOrDefault(cs.SeriesId);
                if (!string.IsNullOrWhiteSpace(seriesKeyword)) data.KeywordsToOverwriteEachCardKeywords.Add(seriesKeyword);
            }

            Log.Debug(JsonSerializer.Serialize(data, JsonSettings.Options));

            overridesSetsLevel.TryAdd(cs.Id, data);
        }

        return overridesSetsLevel;
    }

    public async Task<ConcurrentDictionary<CardTier, ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>>>
        LoadOverridesAsync()
    {
        var filePath = _config.Paths.SetMetadataOverrideFilePath;

        Log.Information("Loading overrides from '{FilePath}'", filePath);

        try
        {
            if (File.Exists(filePath))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var overrides = await JsonFileReader
                    .ReadFromJsonAsync<Dictionary<CardTier, ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>>>(filePath,
                        _cancellationToken);
                return new ConcurrentDictionary<CardTier, ConcurrentDictionary<string, ConcurrentDictionary<string, CardSetHandlerOverrideData>>>(overrides);
            }

            Log.Warning("No overrides file found at '{FilePath}'", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load overrides from '{filePath}'");
        }

        return [];
    }
}