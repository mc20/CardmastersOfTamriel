using System.Text.Json;
using CardmastersOfTamriel.SynthesisPatcher.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Starfield;
using Mutagen.Bethesda.Synthesis;
using Serilog;

namespace CardmastersOfTheStars.SynthesisPatcher;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await SynthesisPipeline.Instance
            .AddPatch<IStarfieldMod, IStarfieldModGetter>(RunPatch)
            .SetTypicalOpen(GameRelease.Starfield, "CardmastersOfTheStars.esp")
            .Run(args);
    }

    public static void RunPatch(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
    {
        var configuration = SetupConfiguration(state);

        var patcherConfig = configuration.Get<PatcherConfiguration>();
        if (patcherConfig is null)
        {
            Console.WriteLine("App config is missing");
            return;
        }

        var patcherConfigJson = JsonSerializer.Serialize(patcherConfig, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("Patcher Configuration:");
        Console.WriteLine(patcherConfigJson);

        patcherConfig.ApplyInternalFilePaths(state);

        if (string.IsNullOrEmpty(patcherConfig.LogOutputFilePath)) Console.WriteLine("Log output file path is missing");
    }

    private static IConfigurationRoot SetupConfiguration(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
    {
        return new ConfigurationBuilder()
            .AddJsonFile(state.RetrieveInternalFile("appsettings.json"), false, true)
            .AddJsonFile(state.RetrieveInternalFile("localsettings.json"), true, true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void SetupLogging(PatcherConfiguration appConfig)
    {
        Log.Information("Setting up logging.. saving to {0}", appConfig.LogOutputFilePath);

        // var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.Delete(appConfig.LogOutputFilePath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(appConfig.LogOutputFilePath)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();
    }
}