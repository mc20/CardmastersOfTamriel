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

    public static async Task AddUniqueSeriesNamesAsKeywordsAsync(PatcherConfiguration patcherConfig,
        IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ISkyrimMod customMod, CancellationToken cancellationToken)
    {
        Log.Information("Getting cards from metadata.");
        var metadataFilePath = patcherConfig.MasterMetadataFilePath =
            state.RetrieveInternalFile(patcherConfig.MasterMetadataFilePath);
        var data = await JsonFileReader.ReadFromJsonAsync<Dictionary<CardTier, HashSet<CardSeries>>>(metadataFilePath,
            cancellationToken);
        var seriesNames = data.Values.SelectMany(series => series).ToHashSet();

        foreach (var kw in seriesNames
                     .Select(NamingHelper.CreateKeyword)
                     .Where(kw => kw is not null)
                     .OfType<string>())
        {
            Log.Verbose("Adding keyword: {Keyword}", kw);
            _ = customMod.Keywords.AddNewWithId(kw);
        }
    }
}