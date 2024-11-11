using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;

public static class ModelFactory
{
    public static Model CreateModel(Card card, TextureSet textureSet)
    {
        return new Model
        {
            File = card.GetModelForCard(),
            AlternateTextures =
            [
                new AlternateTexture
                {
                    Name = "Card",
                    Index = 0,
                    NewTexture = textureSet.ToLink()
                }
            ]
        };
    }
}