namespace CardmastersOfTamriel.Models;

public class CardOverrideData
{
    public required string CardSetId { get; init; }
    public required string CardSeriesId { get; init; }
    public uint? ValueToOverwriteEachCardValue { get; set; }
    public float? ValueToOverwriteEachCardWeight { get; set; }
    public HashSet<string>? KeywordsToOverwriteEachCardKeywords { get; set; }
}