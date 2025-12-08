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
    "benchmarks/SimpleMediator.Benchmarks/SimpleMediator.Benchmarks.csproj",
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
