using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class ImmersiveDisplayOverhaulHelper
{
    public static void AddDisplayKeyword(this Card card)
    {
        card.Keywords ??= [];
        card.Keywords.Add("IDO_DisplayAsDaggerKeyword");
        
        switch (card.Shape)
        {
            case CardShape.Landscape:
                card.Keywords.Add("IDO_DisplayAsGreatswordKeyword");
                break;
            case CardShape.Portrait:
                card.Keywords.Add("IDO_DisplayAsBowKeyword");
                break;
            case CardShape.Square:
                card.Keywords.Add("IDO_DisplayAsSwordKeyword");
                break;
        }
    }
}