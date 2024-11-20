using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace CardmastersOfTamriel.Utilities;

public static class JsonFileWriter
{
    public static async Task WriteToJsonAsync<T>(T data, string relativeFilePath, CancellationToken cancellationToken)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, relativeFilePath);

        var jsonString = JsonSerializer.Serialize(data, JsonSettings.Options);
        await File.WriteAllTextAsync(filePath, jsonString, cancellationToken);
    }

    public static async Task WriteToJsonLineFileAsync<T>(IEnumerable<T> data, string relativeFilePath, CancellationToken cancellationToken)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, relativeFilePath);

        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await using var writer = new StreamWriter(fileStream);

            foreach (var item in data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var jsonLine = JsonSerializer.Serialize(item, JsonSettings.OptionsJsonl);
                await writer.WriteLineAsync(jsonLine.AsMemory(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to write JSON lines to file: '{filePath}'");
            throw;
        }
    }

    public static async Task AppendDataToFileAsync<T>(T item, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var serializedJson = JsonSerializer.Serialize(item, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            });
            await using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true);
            await using var writer = new StreamWriter(fileStream);

            await writer.WriteLineAsync(serializedJson.AsMemory(), cancellationToken);
            Log.Debug($"Serialized JSON written to file: '{filePath}'");
        }
        catch (IOException ex)
        {
            Log.Error(ex, $"Failed to write to file: '{filePath}'");
            throw;
        }
    }

    public static async Task CreateEmptyJsonFileIfMissing(CancellationToken cancellationToken)
    {

    }
}