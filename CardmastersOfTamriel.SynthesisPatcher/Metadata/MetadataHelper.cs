using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.SynthesisPatcher.Metadata;


public class MetadataHelper
{
    private readonly MasterMetadataHandler _handler;

    public MetadataHelper(MasterMetadataHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Retrieves a collection of cards from the metadata.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{Card}"/> containing the cards from the metadata.
    /// </returns>
    /// <remarks>
    /// This method logs the retrieval process and filters out sets and cards that are null or empty.
    /// </remarks>
    public IEnumerable<Card> GetCards()
    {
        Log.Information("Getting cards from metadata.");

        var sets = _handler.Metadata.Series?
              .SelectMany(series => series.Sets ?? [])
              .Where(set => set.Cards != null && set.Cards.Count != 0)
              .ToList() ?? [];

        return sets.SelectMany(set => set.Cards ?? [])
          .Where(card => !string.IsNullOrEmpty(card.Id) && !string.IsNullOrWhiteSpace(card.DestinationRelativeFilePath))
          .ToList() ?? [];
    }
}
