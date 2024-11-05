namespace CardmastersOfTamriel.Models;

public class Card
{
    public string? Id { get; set; }
    public string? SetId { get; set; }
    public string? SetDisplayName { get; set; }
    public string? SeriesId { get; set; }
    public string? ImageFileName { get; set; }
    public CardShape? Shape { get; set; }
    public string? DisplayName { get; set; }
    public uint DisplayedIndex { get; set; }
    public uint DisplayedTotalCount { get; set; }
    public uint TrueIndex { get; set; }
    public uint TrueTotalCount { get; set; }
    public string? Description { get; set; }
    public CardTier Tier { get; set; }
    public uint Value { get; set; }
    public float Weight { get; set; }
    public string[]? Keywords { get; set; }
    public string? SourceAbsoluteFilePath { get; set; }
    public string? DestinationAbsoluteFilePath { get; set; }
    public string? DestinationRelativeFilePath { get; set; }

    public void GetGenericDisplayName()
    {
        DisplayName = CreateGenericDisplayName(SetDisplayName ?? DisplayName, DisplayedIndex, DisplayedTotalCount);
    }

    public static string CreateGenericDisplayName(string? setDisplayName, uint displayedIndex, uint displayedTotalCount)
    {
        if (string.IsNullOrWhiteSpace(setDisplayName))
        {
            return $"Card #{displayedIndex} of {displayedTotalCount}";
        }
        else
        {
            return $"{setDisplayName} - Card #{displayedIndex} of {displayedTotalCount}";
        }
    }
}
