namespace CardAssetManager.Config;

public static class Config
{
    public static readonly string SourceFolder = "";
    public static readonly string OutputFolder = "";

    // Imaging

    public static readonly Size TargetSizePortrait = new Size(1784, 2764);
    public static readonly Size TargetSizeLandscape = new Size(2764, 1784);
    public static readonly Size TargetSizeSquare = new Size(2764, 2764);
    public static readonly Point Offset = new Point(132, 118);
}