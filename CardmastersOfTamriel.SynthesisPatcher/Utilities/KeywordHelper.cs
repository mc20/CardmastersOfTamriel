using CardmastersOfTamriel.Models;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class KeywordHelper
{
    public static void AddKeywords(ISkyrimMod customMod)
    {
        customMod.Keywords.AddNewWithId("CMT_CollectorCard");

        foreach (CardTier cardTier in Enum.GetValues(typeof(CardTier)))
        {
            var kw = cardTier.ToString().ToUpper().AddModNamePrefix();
            if (kw is not null)
                _ = customMod.Keywords.AddNewWithId(kw);
        }
    }
}