using CardmastersOfTamriel.ImageProcessor.Configuration;
using CardmastersOfTamriel.ImageProcessor.Models;
using CardmastersOfTamriel.ImageProcessor.Utilities;
using CardmastersOfTamriel.Models;
using CardmastersOfTamriel.Utilities;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.CardSets.Handlers.Helpers;

public class CardConversionProcessor
{
    private readonly Card _card;
    private readonly ImageConversionSettings _settings;
    private readonly uint _index;
    private readonly uint _totalTrueCount;

    public CardConversionProcessor(ImageConversionSettings settings, Card card, uint index, uint totalTrueCount)
    {
        _settings = settings;
        _card = card;
        _index = index;
        _totalTrueCount = totalTrueCount;
    }

    public async Task ProcessAndUpdateCardForConversionAsync(CardSet set, CancellationToken cancellationToken)
    {
        Log.Debug($"[{set.Id}]:\tProcessing card {_card.Id}");

        try
        {
            if (!File.Exists(_card.SourceAbsoluteFilePath))
            {
                Log.Error($"[{set.Id}]\tCard {_card.Id} does not exist at source path");
                return;
            }

            var result = await ConvertAndSaveImageAsync(_settings,
                set,
                _card.SourceAbsoluteFilePath,
                NamingHelper.CreateFileNameFromCardSetAndIndex(set, _index + 1, "dds"),
                cancellationToken);

            _card.ConversionDate = DateTime.Now;
            _card.Shape = result.Shape;
            _card.DisplayName = null;
            _card.DestinationAbsoluteFilePath = result.DestinationAbsoluteFilePath;
            _card.DestinationRelativeFilePath = FilePathHelper.GetRelativePath(result.DestinationAbsoluteFilePath, set.Tier);
            _card.DisplayedIndex = 0;
            _card.DisplayedTotalCount = 0;
            _card.TrueIndex = _index + 1;
            _card.TrueTotalCount = _totalTrueCount;
            _card.SetGenericDisplayName();
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{set.Id}]\tError processing card {{CardId}} in set {{SetId}}", _card.Id, set.Id);
            throw;
        }
    }

    public void HandleUnconvertedCard(CardSet set)
    {
        if (string.IsNullOrEmpty(_card.DestinationAbsoluteFilePath))
        {
            Log.Debug($"[{set.Id}]\tCard {_card.Id} was not converted and will be skipped");
            UpdateUnconvertedCard();
        }
        else
        {
            Log.Debug($"[{set.Id}]\tCard {_card.Id} was possibly already converted and will be used as-is");
            _card.DestinationRelativeFilePath = FilePathHelper.GetRelativePath(_card.DestinationAbsoluteFilePath, set.Tier);
        }
    }

    private void UpdateUnconvertedCard()
    {
        if (!File.Exists(_card.SourceAbsoluteFilePath))
        {
            Log.Error($"[{_card.SetId}]\tCard {_card.Id} does not exist at source path");
            return;
        }

        Log.Debug($"Card {_card.Id} was not converted and will be skipped");
        _card.Shape = CardShapeHelper.DetermineOptimalShape(_settings, _card.SourceAbsoluteFilePath); // Keep track of the shape for future reference
        _card.DisplayName = null;
        _card.DestinationAbsoluteFilePath = null;
        _card.DestinationRelativeFilePath = null;
        _card.DisplayedIndex = 0;
        _card.DisplayedTotalCount = 0;
        _card.TrueIndex = _index + 1;
        _card.TrueTotalCount = _totalTrueCount;
    }

    private static async Task<ConversionResult> ConvertAndSaveImageAsync(ImageConversionSettings settings,
        CardSet set,
        string sourceImageFilePath,
        string imageFileName,
        CancellationToken cancellationToken)
    {
        Log.Debug("[{SetId}]\tConverting image file {ImageFileName}...", set.Id, imageFileName);
        
        try
        {
            if (!File.Exists(sourceImageFilePath))
            {
                Log.Error($"[{set.Id}]\tSource image file does not exist at path '{sourceImageFilePath}'");
                throw new FileNotFoundException("Source image file does not exist", sourceImageFilePath);
            }

            var helper = new ImageConverter(settings);
            var imageDestinationFilePath = Path.Combine(set.DestinationAbsoluteFolderPath, imageFileName);
            var cardShape = CardShapeHelper.DetermineOptimalShape(settings, sourceImageFilePath);

            await helper.ConvertImageAndSaveToDestinationAsync(set.Tier, sourceImageFilePath, imageDestinationFilePath, cardShape, cancellationToken);

            return new ConversionResult
            {
                Shape = cardShape,
                DestinationAbsoluteFilePath = imageDestinationFilePath
            };
        }
        catch (Exception e)
        {
            Log.Error(e, $"[{set.Id}]\tError converting image file");
            throw;
        }
    }
}