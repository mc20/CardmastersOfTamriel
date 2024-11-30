using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class FormKeyExtensionMethods
{
    public static string AddModNamePrefix(this string str)
    {
        return string.IsNullOrWhiteSpace(str) ? str : $"CMT_{str}";
    }

    public static bool CheckIfExists<T>(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state, FormKey formKey)
        where T : class, IMajorRecordGetter
    {
        return typeof(T) switch
        {
            { } t when t == typeof(IMiscItemGetter) =>
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides()
                    .Any(item => item.FormKey.Equals(formKey)),
            { } t when t == typeof(ITextureSetGetter) =>
                state.LoadOrder.PriorityOrder.TextureSet().WinningOverrides()
                    .Any(item => item.FormKey.Equals(formKey)),
            { } t when t == typeof(ILeveledItemGetter) =>
                state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()
                    .Any(item => item.FormKey.Equals(formKey)),
            _ => throw new InvalidOperationException($"Unsupported type: {typeof(T)}")
        };
    }

    public static bool CheckIfExists<T>(this ISkyrimMod skyrimMod, FormKey formKey)
    {
        return typeof(T) switch
        {
            { } t when t == typeof(MiscItem) =>
                skyrimMod.MiscItems.Any(item => item.FormKey.Equals(formKey)),
            { } t when t == typeof(TextureSet) =>
                skyrimMod.TextureSets.Any(item => item.FormKey.Equals(formKey)),
            { } t when t == typeof(LeveledItem) =>
                skyrimMod.LeveledItems.Any(item => item.FormKey.Equals(formKey)),
            _ => throw new InvalidOperationException($"Unsupported type: {typeof(T)}")
        };
    }

    public static TMajor AddNewWithId<TMajor>(this IGroup<TMajor> group, string editorId) where TMajor : IMajorRecord
    {
        var formKey = FormKeyGeneratorProvider.Instance.FormKeyGenerator.GetNextFormKey(editorId);
        var val = group.AddNew(formKey);
        val.EditorID = editorId;
        return val;
    }
}