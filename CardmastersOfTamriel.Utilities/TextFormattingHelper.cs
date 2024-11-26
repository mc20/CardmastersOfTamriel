using CardmastersOfTamriel.Models;

namespace CardmastersOfTamriel.Utilities;

public static class TextFormattingHelper
{

    public static string AddModNamePrefix(this string str) =>
        string.IsNullOrWhiteSpace(str) ? str : $"CMT_{str}";

    public static readonly int MaxCardShapeTextLength = Enum.GetValues<CardShape>().Max(shape => shape.ToString().Length);

    public static string PadString(string? value, int maxLength)
    {
        var length = value?.Length ?? 0;
        return new string(' ', maxLength - length);
    }

    public static string FormatDuration(long milliseconds)
    {
        var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
        var parts = new List<string>();

        if (timeSpan.Hours > 0)
            parts.Add($"{timeSpan.Hours} hr");
        if (timeSpan.Minutes > 0)
            parts.Add($"{timeSpan.Minutes} min");
        if (timeSpan.Seconds > 0 || timeSpan.TotalSeconds < 60) // Show seconds if total time < 1 minute
            parts.Add($"{timeSpan.Seconds} sec");

        return string.Join(", ", parts);
    }
}
