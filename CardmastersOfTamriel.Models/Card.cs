namespace CardmastersOfTamriel.Models;

public class Card
{
    public string? Id { get; init; }
    public string? SetId { get; init; }
    public string? SetDisplayName { get; init; }
    public string? SeriesId { get; init; }
    public string? ImageFileName { get; set; }
    public CardShape? Shape { get; set; }
    public string? DisplayName { get; set; }
    public uint DisplayedIndex { get; set; }
    public uint DisplayedTotalCount { get; set; }
    public uint TrueIndex { get; set; }
    public uint TrueTotalCount { get; set; }
    public string? Description { get; init; }
    public CardTier Tier { get; init; }
    public uint Value { get; init; }
    public float Weight { get; init; }
    public string[]? Keywords { get; init; }
    public string? SourceAbsoluteFilePath { get; init; }
    public string? DestinationAbsoluteFilePath { get; set; }
    public string? DestinationRelativeFilePath { get; set; }

    public void GetGenericDisplayName()
    {
        DisplayName = CreateGenericDisplayName(SetDisplayName ?? DisplayName, DisplayedIndex, DisplayedTotalCount);
    }

    public static string
        CreateGenericDisplayName(string? setDisplayName, uint displayedIndex, uint displayedTotalCount) =>
        string.IsNullOrWhiteSpace(setDisplayName)
            ? $"Card #{displayedIndex} of {displayedTotalCount}"
            : $"{setDisplayName} - Card #{displayedIndex} of {displayedTotalCount}";
}