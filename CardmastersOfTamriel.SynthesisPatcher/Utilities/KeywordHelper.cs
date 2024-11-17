using CardmastersOfTamriel.Models;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class KeywordHelper
{
    public static void AddKeywords(ISkyrimMod customMod)
    {
        Log.Information("Adding custom keywords to mod..");

        customMod.Keywords.AddNewWithId("CMT_CollectorCard");

        foreach (CardTier cardTier in Enum.GetValues(typeof(CardTier)))
        {
            var kw = cardTier.ToString().ToUpper().AddModNamePrefix();
            if (kw is not null)
            {
                Log.Verbose("Adding keyword: {Keyword}", kw);
                _ = customMod.Keywords.AddNewWithId(kw);
            }
        }
    }
}