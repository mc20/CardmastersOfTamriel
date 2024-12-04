using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public class FormKeyGeneratorProvider
{
    private static readonly Lazy<FormKeyGeneratorProvider> _instance = new(() => new FormKeyGeneratorProvider());

    private FormKeyGeneratorProvider()
    {
        SkyrimMod = new SkyrimMod(new ModKey("CardmastersOfTamriel", ModType.Plugin), SkyrimRelease.SkyrimSE);
        FormKeyGenerator = new FormKeyGenerator(SkyrimMod.ModKey);
        Log.Information("FormKeyGeneratorProvider initialized");
    }

    public FormKeyGenerator FormKeyGenerator { get; }

    public ISkyrimMod SkyrimMod { get; }

    public static FormKeyGeneratorProvider Instance => _instance.Value;
}

public class FormKeyGenerator
{
    private readonly Dictionary<string, uint> _identifierToFormId;
    private readonly ModKey _modKey;
    private uint _nextFormId;

    public FormKeyGenerator(ModKey modKey)
    {
        _modKey = modKey;
        _identifierToFormId = [];
        _nextFormId = 0x800; // Start at 0x800 to avoid reserved IDs
    }

    public FormKey GetNextFormKey(string identifier)
    {
        if (_identifierToFormId.TryGetValue(identifier, out var existingId))
        {
            Log.Debug($"Found existing FormID for {identifier}: {existingId}", identifier, existingId);
            return new FormKey(_modKey, existingId);
        }

        if (_nextFormId > 0xFFFFFF) // Changed from 0xFFF ESL
            throw new Exception("Exceeded ESP FormID limit");

        var formId = _nextFormId++;
        _identifierToFormId[identifier] = formId;
        Log.Debug($"Generated new FormID for {identifier}: {formId}", identifier, formId);
        return new FormKey(_modKey, formId);
    }
}