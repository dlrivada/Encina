using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

string[] arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
string? directory = null;

for (var index = 0; index < arguments.Length; index++)
{
    var current = arguments[index];
    if (string.Equals(current, "--directory", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(current, "-d", StringComparison.OrdinalIgnoreCase))
    {
        if (index + 1 >= arguments.Length)
        {
            Console.Error.WriteLine("Missing value for --directory argument.");
            Environment.Exit(1);
        }

        directory = arguments[++index];
    }
}

directory ??= LocateLatestPerformanceDirectory();

if (directory is null || !Directory.Exists(directory))
{
    Console.Error.WriteLine("Could not locate a benchmark artifacts directory. Use --directory to specify one.");
    Environment.Exit(1);
}

directory = Path.GetFullPath(directory);

// Find all CSV files in the benchmark directory
var csvFiles = Directory.EnumerateFiles(directory, "*.csv", SearchOption.TopDirectoryOnly).ToList();

// Also check subdirectories where BenchmarkDotNet might place results
var searchRoots = new[]
{
    Path.Combine(directory, "results"),
    Path.Combine(directory, "BenchmarkDotNet.Artifacts", "results"),
    Path.Combine("artifacts", "performance", "results")
};

foreach (var root in searchRoots)
{
    if (Directory.Exists(root))
    {
        csvFiles.AddRange(Directory.EnumerateFiles(root, "*.csv", SearchOption.TopDirectoryOnly));
    }
}

// Fallback to recursive search if no files found
if (csvFiles.Count == 0)
{
    csvFiles.AddRange(Directory.EnumerateFiles(directory, "*.csv", SearchOption.AllDirectories));
}

if (csvFiles.Count == 0)
{
    Console.Error.WriteLine($"No CSV report found in {directory}.");
    Environment.Exit(1);
}

Console.WriteLine($"Found {csvFiles.Count} CSV file(s) to analyze");

// Collect all benchmark data from all CSV files
var allBenchmarkData = new List<(string Method, string[] Columns)>();

foreach (var csvFile in csvFiles.Distinct())
{
    Console.WriteLine($"  Reading: {Path.GetFileName(csvFile)}");
    var csvLines = File.ReadAllLines(csvFile);
    if (csvLines.Length < 2)
    {
        continue;
    }

    var csvDelimiter = DetectDelimiter(csvLines[0]);
    var csvHeaders = SplitColumns(csvLines[0], csvDelimiter);
    var csvMethodIndex = Array.IndexOf(csvHeaders, "Method");

    if (csvMethodIndex < 0)
    {
        continue;
    }

    foreach (var csvLine in csvLines.Skip(1))
    {
        if (string.IsNullOrWhiteSpace(csvLine))
        {
            continue;
        }

        var columns = SplitColumns(csvLine, csvDelimiter);
        if (columns.Length > csvMethodIndex)
        {
            allBenchmarkData.Add((columns[csvMethodIndex], columns));
        }
    }
}

if (allBenchmarkData.Count == 0)
{
    Console.Error.WriteLine("No benchmark data found in any CSV file.");
    Environment.Exit(1);
}

// For threshold checking, we need to find headers from the first valid CSV
var firstCsvWithData = csvFiles.First(f => File.ReadAllLines(f).Length >= 2);
var firstLines = File.ReadAllLines(firstCsvWithData);
var firstDelimiter = DetectDelimiter(firstLines[0]);
var headers = SplitColumns(firstLines[0], firstDelimiter);
int methodIndex = Array.IndexOf(headers, "Method");
int meanIndex = Array.IndexOf(headers, "Mean");
int allocatedIndex = Array.IndexOf(headers, "Allocated");

if (methodIndex < 0 || meanIndex < 0 || allocatedIndex < 0)
{
    Console.Error.WriteLine("CSV report is missing required columns (Method, Mean, Allocated).");
    Environment.Exit(1);
}

var thresholds = LoadThresholds()
    ?? new Dictionary<string, (double maxMeanMicroseconds, double maxAllocatedKb)>(StringComparer.Ordinal)
    {
        ["Send_Command_WithInstrumentation"] = (maxMeanMicroseconds: 1.56, maxAllocatedKb: 5.63),
        ["Publish_Notification_WithMultipleHandlers"] = (maxMeanMicroseconds: 1.14, maxAllocatedKb: 2.98)
    };

var results = new List<BenchmarkResult>();
var violations = new List<string>();

foreach (var (method, columns) in allBenchmarkData)
{
    if (columns.Length <= Math.Max(methodIndex, Math.Max(meanIndex, allocatedIndex)))
    {
        continue;
    }

    if (!thresholds.TryGetValue(method, out var limit))
    {
        continue;
    }

    var meanMicroseconds = ParseDuration(columns[meanIndex]);
    var allocatedKb = ParseSize(columns[allocatedIndex]);

    var result = new BenchmarkResult(method, meanMicroseconds, allocatedKb, limit.maxMeanMicroseconds, limit.maxAllocatedKb);
    results.Add(result);

    if (meanMicroseconds > limit.maxMeanMicroseconds)
    {
        violations.Add($"{method}: mean {meanMicroseconds:F3} µs exceeds {limit.maxMeanMicroseconds:F2} µs");
    }

    if (allocatedKb > limit.maxAllocatedKb)
    {
        violations.Add($"{method}: allocations {allocatedKb:F3} KB exceeds {limit.maxAllocatedKb:F2} KB");
    }
}

if (results.Count == 0)
{
    Console.Error.WriteLine("No benchmark results matched the enforced thresholds.");
    Environment.Exit(1);
}

foreach (var result in results)
{
    Console.WriteLine(result.ToString());
}

var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summaryFile))
{
    using var writer = File.AppendText(summaryFile);
    writer.WriteLine("### Benchmark Thresholds");
    writer.WriteLine();
    foreach (var result in results)
    {
        writer.WriteLine(result.ToMarkdown());
    }
    writer.WriteLine();
}

if (violations.Count > 0)
{
    Console.Error.WriteLine("Benchmark regressions detected:");
    foreach (var violation in violations)
    {
        Console.Error.WriteLine("- " + violation);
    }

    Environment.Exit(1);
}

static Dictionary<string, (double maxMeanMicroseconds, double maxAllocatedKb)>? LoadThresholds()
{
    const string configPath = "ci/benchmark-thresholds.json";
    if (!File.Exists(configPath))
    {
        return null;
    }

    try
    {
        using var stream = File.OpenRead(configPath);
        var payload = JsonSerializer.Deserialize(stream, BenchmarkJsonContext.Default.BenchmarkThresholds);
        if (payload?.Benchmarks is null)
        {
            return null;
        }

        return payload.Benchmarks
            .ToDictionary(
                pair => pair.Key,
                pair => (pair.Value.MaxMeanMicroseconds, pair.Value.MaxAllocatedKb),
                StringComparer.Ordinal);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to read benchmark thresholds from '{configPath}': {ex.Message}");
        Environment.Exit(1);
        return null;
    }
}

static string? LocateLatestPerformanceDirectory()
{
    var root = Path.Combine("artifacts", "performance");
    if (!Directory.Exists(root))
    {
        return null;
    }

    return Directory
        .EnumerateDirectories(root)
        .OrderBy(path => path, StringComparer.Ordinal)
        .LastOrDefault();
}

static char DetectDelimiter(string headerLine)
{
    if (headerLine.Contains(';'))
    {
        return ';';
    }

    if (headerLine.Contains(','))
    {
        return ',';
    }

    if (headerLine.Contains('\t'))
    {
        return '\t';
    }

    return ';';
}

static string[] SplitColumns(string line, char delimiter)
{
    return line
        .Split(delimiter)
        .Select(part => part.Trim().Trim('\"'))
        .ToArray();
}

static double ParseDuration(string value)
{
    var (numeric, unit) = SplitValueAndUnit(value);
    var amount = double.Parse(numeric, CultureInfo.InvariantCulture);

    return unit switch
    {
        "ns" => amount / 1_000.0,
        "μs" => amount,
        "us" => amount,
        "ms" => amount * 1_000.0,
        "s" => amount * 1_000_000.0,
        _ => throw new InvalidOperationException($"Unsupported duration unit '{unit}' in '{value}'.")
    };
}

static double ParseSize(string value)
{
    var (numeric, unit) = SplitValueAndUnit(value);
    var amount = double.Parse(numeric, CultureInfo.InvariantCulture);

    return unit switch
    {
        "B" => amount / 1024.0,
        "KB" => amount,
        "MB" => amount * 1024.0,
        "GB" => amount * 1024.0 * 1024.0,
        _ => throw new InvalidOperationException($"Unsupported size unit '{unit}' in '{value}'.")
    };
}

static (string number, string unit) SplitValueAndUnit(string raw)
{
    var trimmed = raw.Trim();
    if (string.IsNullOrEmpty(trimmed))
    {
        throw new InvalidOperationException("Encountered empty value while parsing benchmark output.");
    }

    var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 1)
    {
        // Some exporters omit the unit when it is implicit (e.g., value already stripped).
        return (parts[0], string.Empty);
    }

    return (parts[0], parts[1]);
}

sealed record BenchmarkResult(string Method, double MeanMicroseconds, double AllocatedKb, double LimitMeanMicroseconds, double LimitAllocatedKb)
{
    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}: mean {1:F3} µs (limit {2:F2} µs), allocations {3:F3} KB (limit {4:F2} KB)",
            Method,
            MeanMicroseconds,
            LimitMeanMicroseconds,
            AllocatedKb,
            LimitAllocatedKb);
    }

    public string ToMarkdown()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "- `{0}` → mean **{1:F3} µs** (limit {2:F2} µs), allocations **{3:F3} KB** (limit {4:F2} KB)",
            Method,
            MeanMicroseconds,
            LimitMeanMicroseconds,
            AllocatedKb,
            LimitAllocatedKb);
    }
}

sealed record BenchmarkThresholds(Dictionary<string, Threshold> Benchmarks);

sealed record Threshold(double MaxMeanMicroseconds, double MaxAllocatedKb);

[JsonSerializable(typeof(BenchmarkThresholds))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class BenchmarkJsonContext : JsonSerializerContext;
