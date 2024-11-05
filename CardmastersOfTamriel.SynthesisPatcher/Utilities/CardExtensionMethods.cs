using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class CardExtensionMethods
{
    public static string GetModelForCard(this Card card) => card.Shape switch
    {
        CardShape.Portrait => @"CardmastersOfTamriel\CardBasic_Portrait.nif",
        CardShape.Landscape => @"CardmastersOfTamriel\CardBasic_Landscape.nif",
        CardShape.Square => @"CardmastersOfTamriel\CardBasic_Square.nif",
        _ => @"CardmastersOfTamriel\CardBasic_Portrait.nif"
    };

    public static string GetNormalOrGloss(this Card card) => card.Shape switch
    {
        CardShape.Portrait => @"CardmastersOfTamriel\CardPortrait_n.dds",
        CardShape.Landscape => @"CardmastersOfTamriel\CardLandscape_n.dds",
        CardShape.Square => @"CardmastersOfTamriel\CardSquare_n.dds",
        _ => @"CardmastersOfTamriel\Card_n.dds"
    };
}
