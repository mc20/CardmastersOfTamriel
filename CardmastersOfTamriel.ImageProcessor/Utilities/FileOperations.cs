using System.Diagnostics;
using Serilog;

namespace CardmastersOfTamriel.ImageProcessor.Utilities;

public static class FileOperations
{
    public static async Task ConvertToDdsAsync(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "texconv.exe"),
                    Arguments = $"-o \"{Path.GetDirectoryName(outputPath)}\" -ft DDS -f DXT5 -srgb \"{inputPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            Log.Verbose($"Converting '{Path.GetFileName(inputPath)}' to dds file: '{Path.GetFileName(outputPath)}'");

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    Log.Information(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill();
                    Log.Warning($"Conversion process for '{Path.GetFileName(inputPath)}' was cancelled");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to kill conversion process");
                }

                throw new OperationCanceledException("Process was cancelled", cancellationToken);
            }
        }, cancellationToken);
    }

    public static void EnsureDirectoryExists(string path)
    {
        try
        {
            if (Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            Log.Verbose($"Created directory: '{path}'");
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to create directory");
            throw;
        }
    }
}
