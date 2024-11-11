using Mutagen.Bethesda.Plugins;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public class FormIdGenerator
{
    private readonly ModKey _modKey;
    private readonly Dictionary<string, uint> _identifierToFormId;
    private uint _nextFormId;

    public FormIdGenerator(ModKey modKey)
    {
        _modKey = modKey;
        _identifierToFormId = [];
        _nextFormId = 0x800; // Start at 0x800 to avoid reserved IDs
    }

    public FormKey GetNextFormKey(string identifier)
    {
        if (_identifierToFormId.TryGetValue(identifier, out var existingId))
        {
            return new FormKey(_modKey, existingId);
        }

        if (_nextFormId > 0xFFF)
        {
            throw new Exception("Exceeded ESL FormID limit");
        }

        var formId = _nextFormId++;
        _identifierToFormId[identifier] = formId;
        return new FormKey(_modKey, formId);
    }
}