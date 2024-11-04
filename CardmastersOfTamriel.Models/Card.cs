namespace CardmastersOfTamriel.Models;

public class Card
{
    public string? Id { get; set; }
    public string? SetId { get; set; }
    public string? SetDisplayName { get; set; }
    public string? SeriesId { get; set; }
    public string? ImageFileName { get; set; }
    public CardShape Shape { get; set; }
    public string? DisplayName { get; set; }
    public int Index { get; set; }
    public int TotalCount { get; set; }
    public string? Description { get; set; }
    public CardTier Tier { get; set; }
    public uint Value { get; set; }
    public float Weight { get; set; }
    public string[]? Keywords { get; set; }
    public string? SourceFilePath { get; set; }
    public string? DestinationFilePath { get; set; }
}
