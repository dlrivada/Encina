using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

var options = AggregationOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
Directory.CreateDirectory(options.OutputDirectory);

var benchmarkRows = HistoryAggregator.AggregateBenchmarkRuns(options.BenchmarkRootDirectories);
var loadRows = HistoryAggregator.AggregateLoadRuns(options.LoadRootDirectories);

var benchmarkTablePath = Path.Combine(options.OutputDirectory, "benchmark-history.md");
var loadTablePath = Path.Combine(options.OutputDirectory, "load-history.md");

File.WriteAllLines(benchmarkTablePath, MarkdownFormatter.ToBenchmarkTable(benchmarkRows));
File.WriteAllLines(loadTablePath, MarkdownFormatter.ToLoadTable(loadRows));

Console.WriteLine($"Benchmark history written to {benchmarkTablePath}");
Console.WriteLine($"Load history written to {loadTablePath}");

return;

internal sealed record AggregationOptions(string[] BenchmarkRootDirectories, string[] LoadRootDirectories, string OutputDirectory)
{
    public static AggregationOptions Parse(string[] args)
    {
        var benchmarkRoots = new[] { Path.Combine("artifacts", "performance") };
        var loadRoots = new[] { Path.Combine("artifacts", "load-metrics") };
        var output = Path.Combine("docs", "data");

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--benchmark-root":
                    benchmarkRoots = ReadDirectories(args, ref index);
                    break;
                case "--load-root":
                    loadRoots = ReadDirectories(args, ref index);
                    break;
                case "--output":
                    output = ReadRequired(args, ref index);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{current}'.");
            }
        }

        return new AggregationOptions(benchmarkRoots, loadRoots, output);
    }

    private static string[] ReadDirectories(string[] args, ref int index)
    {
        var value = ReadRequired(args, ref index);
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ReadRequired(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for argument '{args[index]}'.");
        }

        index++;
        return args[index];
    }
}

internal static class HistoryAggregator
{
    public static BenchmarkRow[] AggregateBenchmarkRuns(string[] roots)
    {
        var rows = roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateDirectories(root))
            .SelectMany(ReadBenchmarkDirectory)
            .OrderBy(row => row.Timestamp)
            .ToArray();

        return rows;
    }

    public static LoadRow[] AggregateLoadRuns(string[] roots)
    {
        var rows = roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "metrics-*.csv", SearchOption.AllDirectories))
            .Select(ReadLoadCsv)
            .OrderBy(row => row.Timestamp)
            .ToArray();

        return rows;
    }

    private static BenchmarkRow[] ReadBenchmarkDirectory(string directory)
    {
        var timestamp = TryParseTimestamp(Path.GetFileName(directory));
        var csvFiles = Directory.EnumerateFiles(directory, "*.csv", SearchOption.TopDirectoryOnly);
        var rows = new List<BenchmarkRow>();

        foreach (var file in csvFiles)
        {
            var lines = File.ReadAllLines(file);
            if (lines.Length < 2)
            {
                continue;
            }

            var headers = lines[0].Split(';');
            var methodIndex = Array.IndexOf(headers, "Method");
            var meanIndex = Array.IndexOf(headers, "Mean");
            var allocatedIndex = Array.IndexOf(headers, "Allocated");

            if (methodIndex < 0 || meanIndex < 0 || allocatedIndex < 0)
            {
                continue;
            }

            var maxIndex = Math.Max(methodIndex, Math.Max(meanIndex, allocatedIndex));

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(';');
                if (parts.Length <= maxIndex)
                {
                    continue;
                }

                var scenario = parts[methodIndex];
                var mean = BenchmarkRow.ParseDuration(parts[meanIndex]);
                var allocated = BenchmarkRow.ParseSize(parts[allocatedIndex]);

                if (double.IsNaN(mean) || double.IsNaN(allocated))
                {
                    continue;
                }

                rows.Add(new BenchmarkRow(timestamp, scenario, mean, allocated));
            }
        }

        return rows.ToArray();
    }

    private static LoadRow ReadLoadCsv(string path)
    {
        var token = Path.GetFileNameWithoutExtension(path).Replace("metrics-", string.Empty);
        var timestamp = TryParseTimestamp(token);
        var lines = File.ReadAllLines(path).Skip(1).ToArray();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var throughput = LoadLogParser.Read(directory, token);

        if (lines.Length == 0)
        {
            return new LoadRow(
                timestamp,
                double.NaN,
                double.NaN,
                throughput.SendOpsPerSecond,
                throughput.PublishOpsPerSecond,
                throughput.SendP50OpsPerSecond,
                throughput.SendP95OpsPerSecond,
                throughput.PublishP50OpsPerSecond,
                throughput.PublishP95OpsPerSecond);
        }

        var workingSetValues = lines
            .Select(ParseColumns)
            .Select(columns => columns.WorkingSet)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        var cpuValues = lines
            .Select(ParseColumns)
            .Select(columns => columns.ProcessCpu)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        var meanWorkingSet = workingSetValues.Length > 0 ? workingSetValues.Average() : double.NaN;
        var peakWorkingSet = workingSetValues.Length > 0 ? workingSetValues.Max() : double.NaN;
        var meanCpuPercent = cpuValues.Length > 0 ? cpuValues.Average() : double.NaN;

        return new LoadRow(
            timestamp,
            meanCpuPercent,
            peakWorkingSet,
            throughput.SendOpsPerSecond,
            throughput.PublishOpsPerSecond,
            throughput.SendP50OpsPerSecond,
            throughput.SendP95OpsPerSecond,
            throughput.PublishP50OpsPerSecond,
            throughput.PublishP95OpsPerSecond);

        static (double? ProcessCpu, long? WorkingSet) ParseColumns(string line)
        {
            var parts = line.Split(',', StringSplitOptions.None);
            if (parts.Length < 4)
            {
                return (null, null);
            }

            double? cpu = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var cpuValue) ? cpuValue : null;
            long? workingSet = long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var wsValue) && wsValue > 0
                ? wsValue
                : null;

            return (cpu, workingSet);
        }
    }

    private static DateTimeOffset TryParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTimeOffset.MinValue;
        }

        if (DateTimeOffset.TryParseExact(value, "yyyy-MM-dd.HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
        {
            return timestamp;
        }

        return DateTimeOffset.MinValue;
    }
}

internal readonly record struct BenchmarkRow(DateTimeOffset Timestamp, string Scenario, double MeanMicroseconds, double AllocatedKb)
{
    public static BenchmarkRow? TryParse(DateTimeOffset timestamp, string line)
    {
        var parts = line.Split(';');
        if (parts.Length < 3)
        {
            return null;
        }

        if (!double.TryParse(parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return null;
        }

        var scenario = parts[0];
        var mean = ParseDuration(parts[1]);
        var allocated = ParseSize(parts[2]);

        return new BenchmarkRow(timestamp, scenario, mean, allocated);
    }

    public static double ParseDuration(string value)
    {
        var segments = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return double.NaN;
        }

        var numeric = ParseFlexibleDouble(segments[0]);
        var unit = segments.Length > 1 ? segments[1].Trim().Trim('"', '\'') : "";

        return unit switch
        {
            "ns" => numeric / 1_000.0,
            "μs" or "us" => numeric,
            "ms" => numeric * 1_000.0,
            "s" => numeric * 1_000_000.0,
            _ => numeric
        };
    }

    public static double ParseSize(string value)
    {
        var segments = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return double.NaN;
        }

        var numeric = ParseFlexibleDouble(segments[0]);
        var unit = segments.Length > 1 ? segments[1].Trim().Trim('"', '\'') : "";

        return unit switch
        {
            "B" => numeric / 1024.0,
            "KB" => numeric,
            "MB" => numeric * 1024.0,
            "GB" => numeric * 1024.0 * 1024.0,
            _ => numeric
        };
    }

    public static BenchmarkRow Empty => new(DateTimeOffset.MinValue, string.Empty, double.NaN, double.NaN);

    private static double ParseFlexibleDouble(string raw)
    {
        var cleaned = raw.Trim().Trim('"', '\'');
        cleaned = cleaned.Replace("\u00A0", string.Empty).Replace(" ", string.Empty);

        if (double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var current))
        {
            return current;
        }

        var normalized = cleaned.Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var normalizedValue))
        {
            return normalizedValue;
        }

        normalized = cleaned.Replace(".", string.Empty).Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var fallback))
        {
            return fallback;
        }

        return double.NaN;
    }
}

internal readonly record struct LoadRow(
    DateTimeOffset Timestamp,
    double MeanProcessCpuPercent,
    double PeakWorkingSetBytes,
    double SendThroughputOpsPerSecond,
    double PublishThroughputOpsPerSecond,
    double SendThroughputP50OpsPerSecond,
    double SendThroughputP95OpsPerSecond,
    double PublishThroughputP50OpsPerSecond,
    double PublishThroughputP95OpsPerSecond)
{
    public static LoadRow Empty => new(DateTimeOffset.MinValue, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
}

internal static class MarkdownFormatter
{
    public static string[] ToBenchmarkTable(BenchmarkRow[] rows)
    {
        var lines = new string[rows.Length + 2];
        lines[0] = "| Timestamp (UTC) | Scenario | Mean (µs) | Allocated (KB) |";
        lines[1] = "|----------------|----------|-----------|----------------|";

        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            lines[index + 2] = string.Format(
                CultureInfo.InvariantCulture,
                "| {0:yyyy-MM-dd HH:mm:ss} | `{1}` | {2:F3} | {3:F3} |",
                row.Timestamp == DateTimeOffset.MinValue ? DateTimeOffset.UnixEpoch : row.Timestamp,
                row.Scenario,
                row.MeanMicroseconds,
                row.AllocatedKb);
        }

        return lines;
    }

    public static string[] ToLoadTable(LoadRow[] rows)
    {
        var lines = new string[rows.Length + 2];
        lines[0] = "| Timestamp (UTC) | Mean Process CPU (%) | Peak Working Set (MB) | Send Throughput (ops/sec) | Send P50 (ops/sec) | Send P95 (ops/sec) | Publish Throughput (ops/sec) | Publish P50 (ops/sec) | Publish P95 (ops/sec) |";
        lines[1] = "|----------------|----------------------|-----------------------|----------------------------|--------------------|--------------------|------------------------------|----------------------|----------------------|";

        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var cpu = double.IsNaN(row.MeanProcessCpuPercent) ? "N/A" : row.MeanProcessCpuPercent.ToString("F2", CultureInfo.InvariantCulture);
            var workingSetMb = double.IsNaN(row.PeakWorkingSetBytes) ? "N/A" : (row.PeakWorkingSetBytes / 1_048_576.0).ToString("F2", CultureInfo.InvariantCulture);
            var sendThroughput = double.IsNaN(row.SendThroughputOpsPerSecond) ? "N/A" : row.SendThroughputOpsPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            var sendP50 = double.IsNaN(row.SendThroughputP50OpsPerSecond) ? "N/A" : row.SendThroughputP50OpsPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            var sendP95 = double.IsNaN(row.SendThroughputP95OpsPerSecond) ? "N/A" : row.SendThroughputP95OpsPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            var publishThroughput = double.IsNaN(row.PublishThroughputOpsPerSecond) ? "N/A" : row.PublishThroughputOpsPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            var publishP50 = double.IsNaN(row.PublishThroughputP50OpsPerSecond) ? "N/A" : row.PublishThroughputP50OpsPerSecond.ToString("F2", CultureInfo.InvariantCulture);
            var publishP95 = double.IsNaN(row.PublishThroughputP95OpsPerSecond) ? "N/A" : row.PublishThroughputP95OpsPerSecond.ToString("F2", CultureInfo.InvariantCulture);

            lines[index + 2] = string.Format(
                CultureInfo.InvariantCulture,
                "| {0:yyyy-MM-dd HH:mm:ss} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} |",
                row.Timestamp == DateTimeOffset.MinValue ? DateTimeOffset.UnixEpoch : row.Timestamp,
                cpu,
                workingSetMb,
                sendThroughput,
                sendP50,
                sendP95,
                publishThroughput,
                publishP50,
                publishP95);
        }

        return lines;
    }

    private static double ParseFlexibleDouble(string raw)
    {
        var cleaned = raw.Trim().Trim('"', '\'');
        cleaned = cleaned.Replace("\u00A0", string.Empty).Replace(" ", string.Empty);

        if (double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var current))
        {
            return current;
        }

        var normalized = cleaned.Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var normalizedValue))
        {
            return normalizedValue;
        }

        normalized = cleaned.Replace(".", string.Empty).Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var fallback))
        {
            return fallback;
        }

        return double.NaN;
    }
}

internal static class LoadLogParser
{
    public static LoadThroughput Read(string directory, string timestampToken)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(timestampToken))
        {
            return LoadThroughput.Empty;
        }

        var logPath = Path.Combine(directory, $"harness-{timestampToken}.log");
        if (!File.Exists(logPath))
        {
            return LoadThroughput.Empty;
        }

        double sendThroughput = double.NaN;
        double sendP50 = double.NaN;
        double sendP95 = double.NaN;
        double publishThroughput = double.NaN;
        double publishP50 = double.NaN;
        double publishP95 = double.NaN;

        foreach (var line in File.ReadLines(logPath))
        {
            if (line.StartsWith("Send throughput", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains("P50", StringComparison.OrdinalIgnoreCase))
                {
                    sendP50 = ParseThroughputValue(line);
                }
                else if (line.Contains("P95", StringComparison.OrdinalIgnoreCase))
                {
                    sendP95 = ParseThroughputValue(line);
                }
                else
                {
                    sendThroughput = ParseThroughputValue(line);
                }
            }
            else if (line.StartsWith("Publish throughput", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains("P50", StringComparison.OrdinalIgnoreCase))
                {
                    publishP50 = ParseThroughputValue(line);
                }
                else if (line.Contains("P95", StringComparison.OrdinalIgnoreCase))
                {
                    publishP95 = ParseThroughputValue(line);
                }
                else
                {
                    publishThroughput = ParseThroughputValue(line);
                }
            }
        }

        return new LoadThroughput(sendThroughput, sendP50, sendP95, publishThroughput, publishP50, publishP95);
    }

    private static double ParseThroughputValue(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex < 0)
        {
            return double.NaN;
        }

        var value = line[(colonIndex + 1)..]
            .Replace("ops/sec", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return ParseDoubleFlexible(value);
    }

    private static double ParseDoubleFlexible(string value)
    {
        var sanitized = value
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        string normalized;

        var lastComma = sanitized.LastIndexOf(',');
        var lastDot = sanitized.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            if (lastDot > lastComma)
            {
                normalized = sanitized.Replace(",", string.Empty);
            }
            else
            {
                normalized = sanitized.Replace(".", string.Empty).Replace(',', '.');
            }
        }
        else if (lastComma >= 0)
        {
            normalized = sanitized.Replace(".", string.Empty).Replace(',', '.');
        }
        else
        {
            normalized = sanitized;
        }

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var normalizedResult))
        {
            return normalizedResult;
        }

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out var currentResult))
        {
            return currentResult;
        }

        var fallback = normalized.Replace(",", string.Empty);
        if (double.TryParse(fallback, NumberStyles.Float, CultureInfo.InvariantCulture, out var fallbackResult))
        {
            return fallbackResult;
        }

        return double.NaN;
    }
}

internal readonly record struct LoadThroughput(
    double SendOpsPerSecond,
    double SendP50OpsPerSecond,
    double SendP95OpsPerSecond,
    double PublishOpsPerSecond,
    double PublishP50OpsPerSecond,
    double PublishP95OpsPerSecond)
{
    public static LoadThroughput Empty => new(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
}
