using System.Collections.Concurrent;
using System.Text.Json;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor;

public class CardOverrideDataHelper(Config config, CancellationToken cancellationToken)
{
    /// <summary>
    /// Creates a new override file from the master metadata file and writes it to disk.
    /// </summary>
    /// <param name="masterMetadataFilePath">The path to the master metadata file. If null or empty, the default path from the config will be used.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateAndWriteNewOverrideFileToDiskAsync(string? masterMetadataFilePath = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(masterMetadataFilePath))
        {
            masterMetadataFilePath = config.Paths.MasterMetadataFilePath;
        }

        if (File.Exists(masterMetadataFilePath))
        {
            Log.Information("Creating overrides from master metadata file '{MasterMetadataFilePath}'",
                masterMetadataFilePath);

            try
            {
                var metadata =
                    await JsonFileReader.ReadFromJsonAsync<Dictionary<CardTier, HashSet<CardSeries>>>(
                        masterMetadataFilePath, cancellationToken);

                var overrides = new Dictionary<string, CardOverrideData>();

                var seriesKeywordDictionary = metadata.Values
                    .SelectMany(series => series)
                    .DistinctBy(series => series.Id)
                    .ToDictionary(series => series.Id, NamingHelper.CreateKeyword);

                Log.Information("Generated {Count} series keywords", seriesKeywordDictionary.Count);

                var allCardSets = metadata.Values
                    .SelectMany(series => series)
                    .SelectMany(series => series.Sets ?? [])
                    .DistinctBy(cs => cs.Id)
                    .ToHashSet();

                Log.Information("Processing {Count} card sets", allCardSets.Count);

                foreach (var cs in allCardSets)
                {
                    var data = new CardOverrideData
                    {
                        CardSetId = cs.Id,
                        CardSeriesId = cs.SeriesId,
                        ValueToOverwriteEachCardValue = config.Defaults.DefaultCardValues.GetValueOrDefault(cs.Tier),
                        ValueToOverwriteEachCardWeight = config.Defaults.DefaultCardWeights.GetValueOrDefault(cs.Tier),
                        KeywordsToOverwriteEachCardKeywords = config.Defaults.DefaultMiscItemKeywords.ToHashSet(),
                    };
                    
                    //portrait: IDO_DisplayAsBowKeyword
                    //IDO_DisplayAsDaggerKeyword
                    //IDO_DisplayAsGreatswordKeyword
                    //IDO_DisplayAsMaceKeyword
                    //IDO_DisplayAsStaffKeyword
                    //IDO_DisplayAsSwordKeyword

                    if (config.Defaults.AlwaysIncludeSeriesKeyword)
                    {
                        Log.Verbose("Adding series keyword '{SeriesKeyword}' to default keywords", cs.SeriesKeyword);
                        var seriesKeyword = seriesKeywordDictionary.GetValueOrDefault(cs.SeriesId);
                        if (!string.IsNullOrWhiteSpace(seriesKeyword))
                        {
                            data.KeywordsToOverwriteEachCardKeywords.Add(seriesKeyword);
                        }
                    }

                    Log.Debug(JsonSerializer.Serialize(data, JsonSettings.Options));

                    overrides.TryAdd(cs.Id, data);
                }

                await JsonFileWriter.WriteToJsonAsync(
                    overrides.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    config.Paths.SetMetadataOverrideFilePath, cancellationToken);
                Log.Information(
                    $"New CardSeries metadata override file created at '{config.Paths.SetMetadataOverrideFilePath}'");
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "Failed to create overrides file from metadata but because this is an optional operation, the program will continue.");
            }
        }
        else
        {
            Log.Warning("No Master metadata file exists to create the overrides file");
        }
    }

    public async Task<ConcurrentDictionary<string, CardOverrideData>> LoadOverridesAsync()
    {
        var filePath = config.Paths.SetMetadataOverrideFilePath;

        Log.Information("Loading overrides from '{FilePath}'", filePath);

        try
        {
            if (File.Exists(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var overrides =
                    await JsonFileReader.ReadFromJsonAsync<Dictionary<string, CardOverrideData>>(filePath,
                        cancellationToken);
                return new ConcurrentDictionary<string, CardOverrideData>(overrides);
            }
            else
            {
                Log.Warning("No overrides file found at '{FilePath}'", filePath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load overrides from '{filePath}'");
        }

        return [];
    }
}