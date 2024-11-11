using CardmastersOfTamriel.SynthesisPatcher.Diagnostics;
using CardmastersOfTamriel.SynthesisPatcher.Utilities;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.MiscItems.Factory;

public static class TextureSetFactory
{
    public static TextureSet CreateTextureSet(ISkyrimMod skyrimMod, Card card, FormKey formKey)
    {
        var textureSet = skyrimMod.TextureSets.AddNew(formKey);
        textureSet.Diffuse = @$"CardmastersOfTamriel\{card.DestinationRelativeFilePath}";
        textureSet.NormalOrGloss = card.GetNormalOrGloss();

        Log.Verbose($"Added TextureSet {textureSet.EditorID} with Diffuse Path: '{textureSet.Diffuse}'");

        ModificationTracker.IncrementTextureSetCount(textureSet.FormKey.ToString());

        return textureSet;
    }
}