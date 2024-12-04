using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;

public static class TextureSetFactory
{
    public static TextureSet CreateTextureSet(ISkyrimMod skyrimMod, Card card)
    {
        var editorId = $"TextureSet_{card.Id}".AddModNamePrefix();
        var textureSet = skyrimMod.TextureSets.AddNewWithId(editorId);
        textureSet.Diffuse = @$"CardmastersOfTamriel\{card.DestinationRelativeFilePath}";
        textureSet.NormalOrGloss = card.GetNormalOrGloss();

        Log.Debug($"Added TextureSet {textureSet.EditorID} with Diffuse Path: '{textureSet.Diffuse}'");

        return textureSet;
    }
}