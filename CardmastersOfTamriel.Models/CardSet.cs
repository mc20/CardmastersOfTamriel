namespace CardmastersOfTamriel.Models;

public class CardSet
{
    public string? Id { get; init; }
    public string? SetId { get; init; }
    public string? DisplayName { get; init; }
    public CardTier Tier { get; init; }
    public string? Theme { get; init; }
    public string? Description { get; init; }
    public DateTime ReleaseDate { get; init; }
    public string? Artist { get; init; }
    public bool IsLimitedEdition { get; init; }
    public ICollection<Card>? Cards { get; init; }
    public string? CollectorsNote { get; init; }
    public string? Region { get; init; }
    public Dictionary<string, object>? ExtraAttributes { get; init; }
}
