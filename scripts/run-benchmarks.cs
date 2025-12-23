using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

var artifactsRoot = Path.Combine("artifacts", "performance");
Directory.CreateDirectory(artifactsRoot);

var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd.HHmmss", CultureInfo.InvariantCulture);
var outputDirectory = Path.GetFullPath(Path.Combine(artifactsRoot, timestamp));
Directory.CreateDirectory(outputDirectory);

var arguments = string.Join(' ', new[]
{
    "run",
    "--configuration",
    "Release",
    "--project",
    "benchmarks/Encina.Benchmarks/Encina.Benchmarks.csproj",
    "--",
    "--artifacts",
    Quote(outputDirectory),
    "--exporters",
    "csv,html,github"
});

var psi = new ProcessStartInfo("dotnet", arguments)
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

Console.WriteLine($"Running benchmarks with artifacts in: {outputDirectory}");

using var process = Process.Start(psi);
if (process is null)
{
    Console.Error.WriteLine("Failed to start dotnet benchmark process.");
    Environment.Exit(1);
}

process.OutputDataReceived += (_, data) =>
{
    if (data.Data is not null)
    {
        Console.WriteLine(data.Data);
    }
};
process.ErrorDataReceived += (_, data) =>
{
    if (data.Data is not null)
    {
        Console.Error.WriteLine(data.Data);
    }
};

process.BeginOutputReadLine();
process.BeginErrorReadLine();
process.WaitForExit();

if (process.ExitCode != 0)
{
    Console.Error.WriteLine($"Benchmark execution failed with exit code {process.ExitCode}.");
    Environment.Exit(process.ExitCode);
}

Console.WriteLine($"Benchmark artifacts written to: {outputDirectory}");

CopyExportedReports(outputDirectory);

var outputFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
if (!string.IsNullOrEmpty(outputFile))
{
    File.AppendAllText(outputFile, $"benchmark-dir={outputDirectory}{Environment.NewLine}");
}

var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summaryFile))
{
    File.AppendAllText(summaryFile, $"### Benchmark Run{Environment.NewLine}{Environment.NewLine}- Directory: `{outputDirectory}`{Environment.NewLine}");
}

static string Quote(string value)
{
    return value.Contains(' ') ? $"\"{value}\"" : value;
}

static void CopyExportedReports(string outputDirectory)
{
    // BenchmarkDotNet with --artifacts places results in {artifacts}/results/
    // Also check the legacy BenchmarkDotNet.Artifacts path for backwards compatibility
    var searchRoots = new[]
    {
        Path.Combine(outputDirectory, "results"),
        Path.Combine(outputDirectory, "BenchmarkDotNet.Artifacts", "results"),
        Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results"),
        Path.Combine("artifacts", "performance", "results")
    };

    foreach (var root in searchRoots)
    {
        if (!Directory.Exists(root))
        {
            continue;
        }

        var files = Directory.EnumerateFiles(root).ToArray();
        if (files.Length == 0)
        {
            continue;
        }

        foreach (var file in files)
        {
            var destination = Path.Combine(outputDirectory, Path.GetFileName(file));
            File.Copy(file, destination, overwrite: true);
        }

        Console.WriteLine($"Copied {files.Length} benchmark result files from {root}");
        return;
    }

    Console.WriteLine("No BenchmarkDotNet result files were found to copy into the artifacts directory.");
}
