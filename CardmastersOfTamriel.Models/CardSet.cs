namespace CardmastersOfTamriel.Models;

public class CardSetBasicMetadata : IIdentifiable
{
    public required string Id { get; init; }
    public required string SeriesId { get; init; }
    public string? DisplayName { get; set; }
    public uint DefaultValue { get; set; }
    public float DefaultWeight { get; set; }
    public HashSet<string>? DefaultKeywords { get; set; }
}

public class CardSet : IEquatable<CardSet>, IIdentifiable
{
    public CardSet(string id, string seriesId)
    {
        Id = id;
        SeriesId = seriesId;
    }

    // Immutable identity properties
    public string Id { get; init; }
    public string SeriesId { get; init; }

    // Mutable properties
    public string? DisplayName { get; set; }
    public CardTier Tier { get; set; }
    public string? Description { get; set; }
    public HashSet<Card>? Cards { get; set; }
    public required string SourceAbsoluteFolderPath { get; set; }
    public required string DestinationAbsoluteFolderPath { get; set; }
    public required string DestinationRelativeFolderPath { get; set; }
    public uint DefaultValue { get; set; }
    public float DefaultWeight { get; set; }
    public HashSet<string>? DefaultKeywords { get; set; }

    public CardSetBasicMetadata GetBasicMetadata()
    {
        return new CardSetBasicMetadata()
        {
            Id = Id,
            SeriesId = SeriesId,
            DisplayName = DisplayName,
            DefaultValue = DefaultValue,
            DefaultWeight = DefaultWeight,
            DefaultKeywords = DefaultKeywords
        };
    }

    public override int GetHashCode()
    {
        // Only use immutable identity properties
        return HashCode.Combine(Id, SeriesId);
    }

    public bool Equals(CardSet? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // For set equality, we might only care about the identity
        return Id == other.Id && SeriesId == other.SeriesId;
    }

    public static bool operator ==(CardSet? left, CardSet? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(CardSet? left, CardSet? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CardSet);
    }
}

public class CardEqualityComparer : IEqualityComparer<Card>
{
    public static CardEqualityComparer Instance { get; } = new CardEqualityComparer();

    public bool Equals(Card? x, Card? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id && x.SetId == y.SetId;
    }

    public int GetHashCode(Card obj)
    {
        if (obj is null) return 0;
        return HashCode.Combine(obj.Id, obj.SetId);
    }
}

public class CardSetEqualityComparer : IEqualityComparer<CardSet>
{
    public static CardSetEqualityComparer Instance { get; } = new CardSetEqualityComparer();

    public bool Equals(CardSet? x, CardSet? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id && x.SeriesId == y.SeriesId;
    }

    public int GetHashCode(CardSet obj)
    {
        if (obj is null) return 0;
        return HashCode.Combine(obj.Id, obj.SeriesId);
    }
}