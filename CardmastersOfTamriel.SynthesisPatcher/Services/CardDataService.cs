using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.SynthesisPatcher.Services;

public class CardDataService : ICardDataService
{
    private readonly IMasterMetadataLoader _metadataLoader;

    public CardDataService(IMasterMetadataLoader metadataLoader)
    {
        _metadataLoader = metadataLoader;
    }

    public async Task<ICollection<Card>> GetCardsAsync(CardTier tier)
    {
        var metadata = await _metadataLoader.GetMasterMetadataAsync();

        if (metadata?.Series == null || metadata.Series.Count == 0)
        {
            return Array.Empty<Card>();
        }

        return metadata.Series
                       .SelectMany(series => series.Sets ?? Enumerable.Empty<CardSet>())
                       .Where(set => set.Tier == tier)
                       .SelectMany(set => set.Cards ?? Enumerable.Empty<Card>())
                       .ToList();
    }
}
