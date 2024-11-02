namespace CardmastersOfTamriel.Models;

public class Card
{
    public string? Id { get; init; }
    public string? SetId { get; init; }
    public string? SetDisplayName { get; init; }
    public string? SeriesId { get; init; }
    public string? ImageFileName { get; init; }
    public string? ImageFilePath { get; init; }
    public CardShape Shape { get; init; }
    public string? DisplayName { get; init; }
    public int Index { get; init; }
    public int TotalCount { get; init; }
    public string? Description { get; init; }
    public CardTier Tier { get; init; }
    public uint Value { get; init; }
    public float Weight { get; init; }
    public string[]? Keywords { get; init; }
}
