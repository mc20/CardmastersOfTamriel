namespace CardmastersOfTamriel.Models;

public class CardSeriesBasicMetadata
{
    public required string Id { get; init; }

    public CardSeriesBasicMetadata(string id)
    {
        Id = id;
    }

    public string? DisplayName { get; init; }
    public CardTier Tier { get; init; }
    public string? Description { get; init; }
    public HashSet<CardSetBasicMetadata>? Sets { get; init; }
    public HashSet<string>? DefaultKeywords { get; init; }
    public int? DefaultMaxSampleSize { get; init; }
}

public class CardSetBasicMetadata : IIdentifiable
{
    public required string Id { get; init; }
    public required string SeriesId { get; init; }
    public string? DisplayName { get; set; }
    public uint DefaultValue { get; set; }
    public float DefaultWeight { get; set; }
    public HashSet<string>? DefaultKeywords { get; set; }
    public int? MaxSampleSize { get; set; }
}
