using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

var options = SummaryOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
var rootDirectory = options.RootDirectory;

if (!Directory.Exists(rootDirectory))
{
    Console.Error.WriteLine(FormattableString.Invariant($"NBomber artifacts directory '{rootDirectory}' not found."));
    return 1;
}

var targetDirectory = string.IsNullOrEmpty(options.TargetDirectory)
    ? FindLatestDirectory(rootDirectory)
    : Path.GetFullPath(options.TargetDirectory!);

if (string.IsNullOrEmpty(targetDirectory) || !Directory.Exists(targetDirectory))
{
    Console.Error.WriteLine("NBomber artifacts directory not found. Provide --directory <path> when running the script.");
    return 1;
}

var summaryPath = Path.Combine(targetDirectory, "nbomber-summary.json");
if (!File.Exists(summaryPath))
{
    Console.Error.WriteLine(FormattableString.Invariant($"NBomber summary '{summaryPath}' not found."));
    return 1;
}

ScenarioSummary? summary;
try
{
    var json = File.ReadAllText(summaryPath);
    summary = JsonSerializer.Deserialize(json, NbomberJsonContext.Default.ScenarioSummary);
}
catch (Exception ex)
{
    Console.Error.WriteLine(FormattableString.Invariant($"Failed to read NBomber summary: {ex.Message}"));
    return 1;
}

if (summary is null)
{
    Console.Error.WriteLine("NBomber summary file could not be parsed.");
    return 1;
}

Console.WriteLine(FormattableString.Invariant($"NBomber artifacts: {targetDirectory}"));
if (summary.Send is not null)
{
    Console.WriteLine(DescribeScenario("Send", summary.Send));
}

if (summary.Publish is not null)
{
    Console.WriteLine(DescribeScenario("Publish", summary.Publish));
}

if (summary.Errors is { Length: > 0 })
{
    Console.WriteLine("Sample errors:");
    foreach (var error in summary.Errors.Take(5))
    {
        Console.WriteLine(" - " + error);
    }
}

AppendStepSummary(targetDirectory, summary, options);

var failures = EvaluateThresholds(summary, options);
if (failures.Count > 0)
{
    Console.Error.WriteLine("NBomber thresholds violated:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(" - " + failure);
    }

    return 1;
}

Console.WriteLine("NBomber metrics within thresholds.");
return 0;

static string DescribeScenario(string label, ScenarioMetrics metrics)
{
    var throughput = double.IsNaN(metrics.MeanOpsPerSecond)
        ? "N/A"
        : metrics.MeanOpsPerSecond.ToString("F2", CultureInfo.InvariantCulture) + " ops/sec";

    var latency = string.Join(", ", new[]
    {
        FormatLatency("mean", metrics.MeanLatencyMs),
        FormatLatency("P50", metrics.P50LatencyMs),
        FormatLatency("P95", metrics.P95LatencyMs)
    }.Where(value => !string.IsNullOrEmpty(value)));

    return latency.Length > 0
        ? FormattableString.Invariant($"{label} throughput: {throughput} | Latency {latency}")
        : FormattableString.Invariant($"{label} throughput: {throughput}");
}

static string FormatLatency(string label, double value)
{
    if (double.IsNaN(value))
    {
        return string.Empty;
    }

    return FormattableString.Invariant($"{label} {value:F3} ms");
}

static void AppendStepSummary(string directory, ScenarioSummary summary, SummaryOptions options)
{
    var path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (string.IsNullOrEmpty(path))
    {
        return;
    }

    using var writer = File.AppendText(path);
    writer.WriteLine("### NBomber Summary");
    writer.WriteLine();
    writer.WriteLine(FormattableString.Invariant($"- Artifacts: `{directory}`"));
    if (!string.IsNullOrEmpty(options.ThresholdConfigPath))
    {
        writer.WriteLine(FormattableString.Invariant($"- Threshold config: `{options.ThresholdConfigPath}`"));
    }

    if (summary.Send is not null)
    {
        writer.WriteLine("- " + DescribeScenario("Send", summary.Send));
    }

    if (summary.Publish is not null)
    {
        writer.WriteLine("- " + DescribeScenario("Publish", summary.Publish));
    }

    if (summary.Errors is { Length: > 0 })
    {
        writer.WriteLine("- Sample errors:");
        foreach (var error in summary.Errors.Take(5))
        {
            writer.WriteLine("  - " + error);
        }
    }

    writer.WriteLine();
}

static string? FindLatestDirectory(string root)
{
    var directory = Directory.EnumerateDirectories(root, "nbomber-*", SearchOption.TopDirectoryOnly)
        .Select(path => new DirectoryInfo(path))
        .OrderBy(info => info.Name, StringComparer.Ordinal)
        .LastOrDefault();

    return directory?.FullName;
}

static List<string> EvaluateThresholds(ScenarioSummary summary, SummaryOptions options)
{
    var failures = new List<string>();
    if (string.IsNullOrEmpty(options.ThresholdConfigPath))
    {
        return failures;
    }

    var configPath = Path.GetFullPath(options.ThresholdConfigPath!);
    if (!File.Exists(configPath))
    {
        Console.WriteLine(FormattableString.Invariant($"Warning: NBomber threshold file '{configPath}' not found."));
        return failures;
    }

    NbomberThresholds? thresholds;
    try
    {
        var json = File.ReadAllText(configPath);
        thresholds = JsonSerializer.Deserialize(json, NbomberJsonContext.Default.NbomberThresholds);
    }
    catch (Exception ex)
    {
        Console.WriteLine(FormattableString.Invariant($"Warning: failed to parse NBomber threshold file '{configPath}': {ex.Message}"));
        return failures;
    }

    if (thresholds is null)
    {
        Console.WriteLine(FormattableString.Invariant($"Warning: NBomber threshold file '{configPath}' contained no usable data."));
        return failures;
    }

    if (summary.Send is not null)
    {
        EvaluateScenario("Send", summary.Send, thresholds, failures);
    }

    if (summary.Publish is not null)
    {
        EvaluateScenario("Publish", summary.Publish, thresholds, failures);
    }

    return failures;

    static void EvaluateScenario(string label, ScenarioMetrics metrics, NbomberThresholds thresholds, List<string> sink)
    {
        if (thresholds.MinThroughputOpsPerSecond.HasValue && !double.IsNaN(metrics.MeanOpsPerSecond) && metrics.MeanOpsPerSecond + double.Epsilon < thresholds.MinThroughputOpsPerSecond.Value)
        {
            sink.Add(FormattableString.Invariant($"{label} throughput {metrics.MeanOpsPerSecond:F2} ops/sec below minimum {thresholds.MinThroughputOpsPerSecond.Value:F2} ops/sec"));
        }

        if (thresholds.MaxLatencyMs.HasValue)
        {
            if (!double.IsNaN(metrics.MeanLatencyMs) && metrics.MeanLatencyMs > thresholds.MaxLatencyMs.Value)
            {
                sink.Add(FormattableString.Invariant($"{label} mean latency {metrics.MeanLatencyMs:F3} ms exceeds maximum {thresholds.MaxLatencyMs.Value:F3} ms"));
            }

            if (!double.IsNaN(metrics.P95LatencyMs) && metrics.P95LatencyMs > thresholds.MaxLatencyMs.Value)
            {
                sink.Add(FormattableString.Invariant($"{label} P95 latency {metrics.P95LatencyMs:F3} ms exceeds maximum {thresholds.MaxLatencyMs.Value:F3} ms"));
            }
        }
    }
}

internal sealed record SummaryOptions(string RootDirectory, string? TargetDirectory, string? ThresholdConfigPath)
{
    public static SummaryOptions Parse(string[] args)
    {
        var root = Path.Combine("artifacts", "load-metrics");
        string? directory = null;
        string? thresholds = null;

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--root" when TryRead(args, ref index, out var rootValue):
                    root = rootValue;
                    break;
                case "--directory" when TryRead(args, ref index, out var directoryValue):
                    directory = directoryValue;
                    break;
                case "--thresholds" when TryRead(args, ref index, out var thresholdsValue):
                    thresholds = thresholdsValue;
                    break;
            }
        }

        return new SummaryOptions(root, directory, thresholds);
    }

    private static bool TryRead(string[] args, ref int index, out string value)
    {
        value = string.Empty;
        if (index + 1 >= args.Length)
        {
            return false;
        }

        value = args[++index];
        return true;
    }
}

internal sealed class ScenarioSummary
{
    public ScenarioMetrics? Send { get; set; }

    public ScenarioMetrics? Publish { get; set; }

    public string[] Errors { get; set; } = Array.Empty<string>();
}

internal sealed class ScenarioMetrics
{
    public double MeanOpsPerSecond { get; set; }

    public double MeanLatencyMs { get; set; }

    public double P50LatencyMs { get; set; }

    public double P95LatencyMs { get; set; }
}

internal sealed class NbomberThresholds
{
    public string? Scenario { get; set; }

    public double? MinThroughputOpsPerSecond { get; set; }

    public double? MaxLatencyMs { get; set; }
}

[JsonSerializable(typeof(ScenarioSummary))]
[JsonSerializable(typeof(NbomberThresholds))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class NbomberJsonContext : JsonSerializerContext;
