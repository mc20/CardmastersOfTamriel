namespace CardmastersOfTamriel.Models;

public class CardSet
{
    public string? Id { get; set; }
    public string? SeriesId { get; set; }
    public string? DisplayName { get; set; }
    public CardTier Tier { get; set; }
    public string? Theme { get; set; }
    public string? Description { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? Artist { get; set; }
    public bool IsLimitedEdition { get; set; }
    public ICollection<Card>? Cards { get; set; }
    public bool AutoRegenerateData { get; set; } = true;
    public string? CollectorsNote { get; set; }
    public string? Region { get; set; }
    public Dictionary<string, object>? ExtraAttributes { get; set; }
    public string SourceAbsoluteFolderPath { get; set; }
    public string DestinationAbsoluteFolderPath { get; set; }
    public string DestinationRelativeFolderPath { get; set; }
    public uint DefaultValue { get; set; } = 0;
    public float DefaultWeight { get; set; } = 0;
    public string[]? DefaultKeywords { get; set; }

    public CardSet()
    {
        SourceAbsoluteFolderPath = string.Empty;
        DestinationAbsoluteFolderPath = string.Empty;
        DestinationRelativeFolderPath = string.Empty;
    }
}