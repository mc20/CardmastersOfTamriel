using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;

namespace CardmastersOfTamriel.SynthesisPatcher.Utilities;


public class MetadataHelper
{
    private readonly IMasterMetadataHandler _handler;

    public MetadataHelper(IMasterMetadataHandler handler)
    {
        _handler = handler;
    }

    public IEnumerable<Card> GetCards()
    {
        var sets = _handler.Metadata.Series?
              .SelectMany(series => series.Sets ?? [])
              .Where(set => set.Cards != null && set.Cards.Count != 0)
              .ToList() ?? [];

        return sets.SelectMany(set => set.Cards ?? [])
          .Where(card => !string.IsNullOrEmpty(card.Id) && !string.IsNullOrWhiteSpace(card.DestinationRelativeFilePath))
          .ToList() ?? [];
    }
}
