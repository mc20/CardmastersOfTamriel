using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Common.Configuration;
using CardmastersOfTamriel.Utilities;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class KeywordHelper
{
    public static void AddStandardKeywords(ISkyrimMod customMod)
    {
        Log.Information("Adding custom keywords to mod..");

        customMod.Keywords.AddNewWithId("CMT_CollectorCard");

        foreach (CardTier cardTier in Enum.GetValues(typeof(CardTier)))
        {
            var kw = cardTier.ToString().ToUpper().AddModNamePrefix();
            Log.Verbose("Adding keyword: {Keyword}", kw);
            _ = customMod.Keywords.AddNewWithId(kw);
        }
    }

    public static async Task<Dictionary<string, string>> AddUniqueSeriesNamesAsKeywordsAsync(
        PatcherConfiguration patcherConfig,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, CancellationToken cancellationToken)
    {
        Log.Information("Adding card series keywords to mod..");

        var keywordsBySeries = new Dictionary<string, string>();

        var metadataFilePath = patcherConfig.MasterMetadataFilePath =
            state.RetrieveInternalFile(patcherConfig.MasterMetadataFilePath);
        var data = await JsonFileReader.ReadFromJsonAsync<Dictionary<CardTier, HashSet<CardSeries>>>(metadataFilePath,
            cancellationToken);
        var cardSeries = data.Values.SelectMany(series => series).ToHashSet();

        Log.Debug($"Found {cardSeries.Count} card series and adding eligible keywords");

        var rules = new List<Rule>();

        foreach (var cs in cardSeries)
        {
            var keyword = NamingHelper.CreateKeyword(cs);
            if (string.IsNullOrEmpty(keyword))
            {
                Log.Debug("Skipping series with no name: {SeriesId}", cs.Id);
                continue;
            }

            Log.Debug("Adding keyword: {Keyword}", keyword);
            _ = customMod.Keywords.AddNewWithId(keyword);

            foreach (var set in cs.Sets ?? [])
            {
                keywordsBySeries[set.Id] = keyword;
            }

            // Add a rule for the Inventory Injector configuration
            rules.Add(new Rule
            {
                Match = new Match
                {
                    FormType = "MiscItem",
                    Keywords = [keyword]
                },
                Assign = new Assign
                {
                    SubType = cs.Tier.ToString().ToUpper(),
                    SubTypeDisplay = cs.DisplayName ?? "CMT Card"
                }
            });
        }

        foreach (var (key, value) in keywordsBySeries)
        {
            Log.Debug("Mapped CardSet {SetId} to keyword {Keyword}", key, value);
        }

        // Generate the Inventory Injector configuration file
        var config = new InventoryInjectorConfig
        {
            Rules = rules
        };

        var jsonFilePath =
            Path.Combine(state.DataFolderPath, "SKSE", "Plugins", "InventoryInjector", "CardmastersOfTamriel.json");
        Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath)!);
        await JsonFileWriter.WriteToJsonAsync(config, jsonFilePath, cancellationToken);

        Log.Information("Inventory Injector configuration file saved to {FilePath}", jsonFilePath);

        return keywordsBySeries;
    }
}