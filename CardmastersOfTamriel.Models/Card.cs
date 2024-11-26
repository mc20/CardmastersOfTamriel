namespace CardmastersOfTamriel.Models;

public class Card : IEquatable<Card>, IIdentifiable
{
    // Immutable identity properties (used for hash code)
    public Card(string id, string setId)
    {
        Id = id;
        SetId = setId;
    }

    public string Id { get; init; }
    public string SetId { get; init; }

    // Mutable properties
    public string? SetDisplayName { get; set; }
    public string? SeriesId { get; set; }
    public DateTime? ConversionDate { get; set; }
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
    public HashSet<string>? Keywords { get; set; }
    public string? SourceAbsoluteFilePath { get; set; }
    public string? DestinationAbsoluteFilePath { get; set; }
    public string? DestinationRelativeFilePath { get; set; }

    public void SetGenericDisplayName()
    {
        // var indexFormat = new string('0', DisplayedTotalCount.ToString().Length);
        // DisplayName = $"{SetDisplayName ?? DisplayName ?? Id} - Card #{DisplayedIndex.ToString(indexFormat)} of {DisplayedTotalCount}";

        DisplayName = $"{SetDisplayName ?? DisplayName ?? Id} - Card #{DisplayedIndex}";
    }

    public override int GetHashCode()
    {
        // Only use immutable identity properties
        return HashCode.Combine(Id, SetId);
    }

    public bool Equals(Card? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // For card equality, we might only care about the identity
        return Id == other.Id && SetId == other.SetId;
    }

    // Operator overloads for consistency
    public static bool operator ==(Card? left, Card? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Card? left, Card? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Card);
    }
}