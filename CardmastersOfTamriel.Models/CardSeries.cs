namespace CardmastersOfTamriel.Models;

public class CardSeries
{
    public string? DisplayName { get; init; }
    public string? Id { get; init; }
    public string? Theme { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public string? Artist { get; init; }
    public bool IsLimitedEdition { get; init; }
    public string? Description { get; set; }
    public CardTier? Tier { get; init; }
    public ICollection<CardSet>? Sets { get; init; }
}
