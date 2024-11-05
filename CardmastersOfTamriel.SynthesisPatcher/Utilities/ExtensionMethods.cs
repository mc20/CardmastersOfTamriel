using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;

public static class ExtensionMethods
{
    public static string AddModNamePrefix(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;

        return $"CardmastersOfTamriel_{str}";
    }

    public static string RetrieveInternalFilePath(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string configPath)
    {
        return configPath is null ? throw new ArgumentNullException(nameof(configPath)) : state.RetrieveInternalFile(configPath);
    }

    public static bool CheckIfExists<T>(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
    {
        var existing = typeof(T) switch
        {
            Type t when t == typeof(IMiscItemGetter) => state.LoadOrder.PriorityOrder.MiscItem() as IEnumerable<T>,
            Type t when t == typeof(ITextureSetGetter) => state.LoadOrder.PriorityOrder.TextureSet() as IEnumerable<T>,
            Type t when t == typeof(ILeveledItemGetter) => state.LoadOrder.PriorityOrder.LeveledItem() as IEnumerable<T>,
            _ => throw new InvalidOperationException($"Unsupported type: {typeof(T)}")
        };

        return existing is not null;
    }

    public static bool CheckIfExists<T>(this IPatcherState<ISkyrimMod, ISkyrimModGetter> state, string editorId) where T : class, IMajorRecordGetter
    {
        return typeof(T) switch
        {
            Type t when t == typeof(IMiscItemGetter) =>
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides().Any(item => item.EditorID != null && item.EditorID.Equals(editorId, StringComparison.OrdinalIgnoreCase)),
            Type t when t == typeof(ITextureSetGetter) =>
                state.LoadOrder.PriorityOrder.TextureSet().WinningOverrides().Any(item => item.EditorID != null && item.EditorID.Equals(editorId, StringComparison.OrdinalIgnoreCase)),
            Type t when t == typeof(ILeveledItemGetter) =>
                state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Any(item => item.EditorID != null && item.EditorID.Equals(editorId, StringComparison.OrdinalIgnoreCase)),
            _ => throw new InvalidOperationException($"Unsupported type: {typeof(T)}")
        };
    }

    public static bool CheckIfExists<T>(this ISkyrimMod skyrimMod, string editorId)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(MiscItem) =>
                skyrimMod.MiscItems.Any(item => item.EditorID != null && item.EditorID.Equals(editorId, StringComparison.OrdinalIgnoreCase)),
            Type t when t == typeof(TextureSet) =>
                skyrimMod.TextureSets.Any(item => item.EditorID != null && item.EditorID.Equals(editorId, StringComparison.OrdinalIgnoreCase)),
            Type t when t == typeof(LeveledItem) =>
                skyrimMod.LeveledItems.Any(item => item.EditorID != null && item.EditorID.Equals(editorId, StringComparison.OrdinalIgnoreCase)),
            _ => throw new InvalidOperationException($"Unsupported type: {typeof(T)}")
        };
    }
}
