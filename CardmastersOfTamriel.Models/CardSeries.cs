namespace CardmastersOfTamriel.Models;

public class CardSeries
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public CardTier Tier { get; set; }
    public string? Theme { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? Artist { get; set; }
    public bool IsLimitedEdition { get; set; }
    public string? Description { get; set; }
    public List<CardSet>? Sets { get; set; }
    public string SourceFolderPath { get; set; }
    public string DestinationFolderPath { get; set; }

    public CardSeries()
    {
        SourceFolderPath = string.Empty;
        DestinationFolderPath = string.Empty;
    }
}