namespace CardmastersOfTamriel.Models;

public class CardSet
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public CardTier Tier { get; set; }
    public string? Theme { get; set; }
    public string? Description { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? Artist { get; set; }
    public bool IsLimitedEdition { get; set; }
    public ICollection<Card>? Cards { get; set; }
    public string? CollectorsNote { get; set; }
    public string? Region { get; set; }
    public Dictionary<string, object>? ExtraAttributes { get; set; }
    public required string SourceFolderPath { get; set; }
    public required string DestinationFolderPath { get; set; }
}
