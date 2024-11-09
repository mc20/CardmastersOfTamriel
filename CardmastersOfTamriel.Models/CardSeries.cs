namespace CardmastersOfTamriel.Models;

public class CardSeries : IEquatable<CardSeries>, IIdentifiable
{
    public CardSeries(string id)
    {
        Id = id;
    }

    // Immutable identity property
    public string Id { get; init; }

    // Mutable properties
    public string? DisplayName { get; set; }
    public CardTier Tier { get; set; }
    public string? Description { get; set; }
    public HashSet<CardSet>? Sets { get; set; }
    public required string SourceFolderPath { get; set; }
    public required string DestinationFolderPath { get; set; }

    public override int GetHashCode()
    {
        // Only use immutable identity property
        return Id.GetHashCode();
    }

    public bool Equals(CardSeries? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // For series equality, we only care about the identity
        return Id == other.Id;
    }

    public static bool operator ==(CardSeries? left, CardSeries? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(CardSeries? left, CardSeries? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CardSeries);
    }
}