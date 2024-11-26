using CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Models;
using CardmastersOfTamriel.ImageProcessor.ProgressTracking;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;

public class CardMetadataUpdater
{
    private readonly Config _config;
    private readonly CardSet _set;
    private readonly RebuildMasterMetadataData _data;
    private readonly object? _publisher;
    private readonly uint _totalCardCountToDisplayOnCard;

    public CardMetadataUpdater(object? publisher, CardSet set, RebuildMasterMetadataData data, Config config, uint totalCardCountToDisplayOnCard)
    {
        _publisher = publisher;
        _set = set;
        _data = data;
        _config = config;
        _totalCardCountToDisplayOnCard = totalCardCountToDisplayOnCard;
    }

    public void UpdateCardMetadataAndPublishHandlingProgress(Card card, ref int displayedIndex, ref int maxDisplayNameLength, CancellationToken cancellationToken, bool assignRelativePath = true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (card.SourceAbsoluteFilePath != null)
        {
            card.Shape ??= CardShapeHelper.DetermineOptimalShape(_config, card.SourceAbsoluteFilePath);
        }

        card.Keywords ??= _config.General.DefaultMiscItemKeywords;
        if (_set.DefaultKeywords != null) card.Keywords.UnionWith(_set.DefaultKeywords);
        
        card.TrueTotalCount = (uint)_data.ValidUniqueIdentifiersDeterminedFromSource.Count;

        if (_data.ValidIdentifiersAtDestination.Contains(card.Id))
        {
            card.DestinationAbsoluteFilePath = _data.ImageFilePathsAtDestination.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == card.Id);

            if (assignRelativePath)
            {
                card.DestinationRelativeFilePath = FilePathHelper.GetRelativePath(card.DestinationAbsoluteFilePath, card.Tier);
            }

            var isDistributingCardInGame = card.DestinationRelativeFilePath != null;
            if (isDistributingCardInGame)
            {
                card.DisplayedIndex = (uint)displayedIndex;
                card.DisplayedTotalCount = _totalCardCountToDisplayOnCard;
                card.SetGenericDisplayName();
                displayedIndex++;
            }
            else
            {
                card.DisplayedIndex = 0;
                card.DisplayedTotalCount = 0;
                card.DisplayName = null;
            }

            if (maxDisplayNameLength < (card.DisplayName?.Length ?? 0))
                maxDisplayNameLength = card.DisplayName?.Length ?? 0;
        }
        else
        {
            card.DestinationAbsoluteFilePath = null;
            card.DestinationRelativeFilePath = null;
            card.DisplayedIndex = 0;
            card.DisplayedTotalCount = 0;
            card.DisplayName = null;
        }

        EventBroker.PublishSetHandlingProgress(_publisher, new ProgressTrackingEventArgs(card));

        if (Log.IsEnabled(Serilog.Events.LogEventLevel.Debug))
        {
            Log.Debug($"{card.SetId}\tRefreshed metadata for Card '{card.Id}' -> " +
                      $"Shape: '{card.Shape}'{TextFormattingHelper.PadString(card.Shape?.ToString(), TextFormattingHelper.MaxCardShapeTextLength)}\t" +
                      $"SourceAbsoluteFilePath: '{card.SourceAbsoluteFilePath}'\t" +
                      $"DisplayName: '{card.DisplayName}'{TextFormattingHelper.PadString(card.DisplayName, maxDisplayNameLength)}\t" +
                      $"DestinationRelativeFilePath: '{card.DestinationRelativeFilePath}'\t");
        }
    }
}