namespace CardmastersOfTamriel.Models;

public class CardSetHandlerOverrideData
{
    public required string CardSetId { get; init; }
    public required string CardSeriesId { get; init; }
    public required CardTier Tier { get; init; }
    public string? NewSetDisplayName { get; init; }
    public bool UseOriginalFileNamesAsDisplayNames { get; init; } = false;
    public bool IgnoreMaximumNumberOfCardsToIncludeLimit { get; init; } = false;
    public uint? ValueToOverwriteEachCardValue { get; init; }
    public float? ValueToOverwriteEachCardWeight { get; init; }
    public HashSet<string>? KeywordsToOverwriteEachCardKeywords { get; init; }
}