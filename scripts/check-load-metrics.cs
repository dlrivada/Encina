using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

var options = LoadMetricsCheckOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());

if (!Directory.Exists(options.MetricsDirectory))
{
    Console.Error.WriteLine($"Metrics directory '{options.MetricsDirectory}' not found.");
    Environment.Exit(1);
}

var metricsFile = Directory.EnumerateFiles(options.MetricsDirectory, "metrics-*.csv", SearchOption.AllDirectories)
    .OrderBy(path => path)
    .LastOrDefault();

if (metricsFile is null)
{
    Console.Error.WriteLine($"No metrics files found under '{options.MetricsDirectory}'.");
    Environment.Exit(1);
}

Console.WriteLine($"Analyzing load metrics: {metricsFile}");

var parser = new MetricsParser(metricsFile);
var metrics = parser.Parse();
var throughput = LoadLogParser.Parse(metricsFile);

Console.WriteLine(
    string.Format(
        CultureInfo.InvariantCulture,
        "Mean CPU: {0:F2}% (max {6:F2}%) | Peak Working Set: {1:F2} MB (max {7:F2} MB) | Send: {2} | Publish: {3}{4}{5}",
        metrics.MeanCpuPercent,
        metrics.PeakWorkingSetBytes / 1_048_576.0,
        FormatThroughput(throughput.SendOpsPerSecond, throughput.SendP50OpsPerSecond, throughput.SendP95OpsPerSecond),
        FormatThroughput(throughput.PublishOpsPerSecond, throughput.PublishP50OpsPerSecond, throughput.PublishP95OpsPerSecond),
        FormatThroughputThresholds(options.MinSendMeanOps, options.MinSendP50Ops, options.MinSendP95Ops),
        FormatThroughputThresholds(options.MinPublishMeanOps, options.MinPublishP50Ops, options.MinPublishP95Ops, "Publish thresholds"),
        options.MaxMeanCpuPercent,
        options.MaxPeakWorkingSetMb));

if (throughput.HasErrors)
{
    PrintErrors("Send", throughput.SendErrors);
    PrintErrors("Publish", throughput.PublishErrors);
}

var summaryFile = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summaryFile))
{
    using var writer = File.AppendText(summaryFile);
    writer.WriteLine("### Load Harness Check");
    writer.WriteLine();
    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Metrics: `{0}`", metricsFile));
    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Mean CPU: {0:F2}% (threshold {1:F2}%)", metrics.MeanCpuPercent, options.MaxMeanCpuPercent));
    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Peak Working Set: {0:F2} MB (threshold {1:F2} MB)", metrics.PeakWorkingSetBytes / 1_048_576.0, options.MaxPeakWorkingSetMb));
    if (throughput.HasData)
    {
        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Send Throughput: {0}", FormatThroughput(throughput.SendOpsPerSecond, throughput.SendP50OpsPerSecond, throughput.SendP95OpsPerSecond)));
        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Publish Throughput: {0}", FormatThroughput(throughput.PublishOpsPerSecond, throughput.PublishP50OpsPerSecond, throughput.PublishP95OpsPerSecond)));
    }
    if (options.HasSendThroughputThresholds)
    {
        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Send thresholds: {0}", DescribeThresholds(options.MinSendMeanOps, options.MinSendP50Ops, options.MinSendP95Ops)));
    }
    if (options.HasPublishThroughputThresholds)
    {
        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "- Publish thresholds: {0}", DescribeThresholds(options.MinPublishMeanOps, options.MinPublishP50Ops, options.MinPublishP95Ops)));
    }
    if (throughput.HasErrors)
    {
        AppendErrors(writer, "Send", throughput.SendErrors);
        AppendErrors(writer, "Publish", throughput.PublishErrors);
    }
    writer.WriteLine();
}

var failures = new List<string>();

if (!double.IsNaN(metrics.MeanCpuPercent) && metrics.MeanCpuPercent > options.MaxMeanCpuPercent)
{
    failures.Add(string.Format(
        CultureInfo.InvariantCulture,
        "Mean CPU {0:F2}% exceeds maximum {1:F2}%",
        metrics.MeanCpuPercent,
        options.MaxMeanCpuPercent));
}

var peakWorkingSetMb = metrics.PeakWorkingSetBytes / 1_048_576.0;
if (!double.IsNaN(peakWorkingSetMb) && peakWorkingSetMb > options.MaxPeakWorkingSetMb)
{
    failures.Add(string.Format(
        CultureInfo.InvariantCulture,
        "Peak working set {0:F2} MB exceeds maximum {1:F2} MB",
        peakWorkingSetMb,
        options.MaxPeakWorkingSetMb));
}

EvaluateThroughput("send mean", throughput.SendOpsPerSecond, options.MinSendMeanOps, ref failures);
EvaluateThroughput("send P50", throughput.SendP50OpsPerSecond, options.MinSendP50Ops, ref failures);
EvaluateThroughput("send P95", throughput.SendP95OpsPerSecond, options.MinSendP95Ops, ref failures);
EvaluateThroughput("publish mean", throughput.PublishOpsPerSecond, options.MinPublishMeanOps, ref failures);
EvaluateThroughput("publish P50", throughput.PublishP50OpsPerSecond, options.MinPublishP50Ops, ref failures);
EvaluateThroughput("publish P95", throughput.PublishP95OpsPerSecond, options.MinPublishP95Ops, ref failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("Load metrics check failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(" - " + failure);
    }

    Environment.Exit(1);
}

Console.WriteLine("Load metrics within thresholds.");

static string FormatThroughput(double mean, double p50, double p95)
{
    if (double.IsNaN(mean))
    {
        return "N/A";
    }

    if (double.IsNaN(p50) || double.IsNaN(p95))
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:F2} ops/sec", mean);
    }

    return string.Format(CultureInfo.InvariantCulture, "{0:F2} ops/sec (P50 {1:F2}, P95 {2:F2})", mean, p50, p95);
}

static string FormatThroughputThresholds(double mean, double p50, double p95, string label = "Send thresholds")
{
    if (double.IsNaN(mean) && double.IsNaN(p50) && double.IsNaN(p95))
    {
        return string.Empty;
    }

    return string.Format(
        CultureInfo.InvariantCulture,
        " | {0}: {1}",
        label,
        DescribeThresholds(mean, p50, p95));
}

static void PrintErrors(string label, IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
    {
        return;
    }

    Console.WriteLine($"Sample {label.ToLowerInvariant()} errors:");
    foreach (var error in errors)
    {
        Console.WriteLine(" - " + error);
    }
}

static void AppendErrors(TextWriter writer, string label, IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
    {
        return;
    }

    writer.WriteLine($"- Sample {label.ToLowerInvariant()} errors:");
    foreach (var error in errors)
    {
        writer.WriteLine("  - " + error);
    }
}

static void EvaluateThroughput(string label, double actual, double threshold, ref List<string> failures)
{
    if (double.IsNaN(threshold))
    {
        return;
    }

    if (double.IsNaN(actual))
    {
        failures.Add($"Missing {label} throughput data to validate threshold {threshold:F2} ops/sec");
        return;
    }

    if (actual + double.Epsilon < threshold)
    {
        failures.Add(string.Format(
            CultureInfo.InvariantCulture,
            "{0} throughput {1:F2} ops/sec below minimum {2:F2} ops/sec",
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label),
            actual,
            threshold));
    }
}

static string DescribeThresholds(double mean, double p50, double p95)
{
    var parts = new List<string>(3);
    if (!double.IsNaN(mean))
    {
        parts.Add(string.Format(CultureInfo.InvariantCulture, "mean ≥ {0:F2}", mean));
    }

    if (!double.IsNaN(p50))
    {
        parts.Add(string.Format(CultureInfo.InvariantCulture, "P50 ≥ {0:F2}", p50));
    }

    if (!double.IsNaN(p95))
    {
        parts.Add(string.Format(CultureInfo.InvariantCulture, "P95 ≥ {0:F2}", p95));
    }

    return string.Join(", ", parts);
}

internal sealed record LoadMetricsCheckOptions(
    string MetricsDirectory,
    double MaxMeanCpuPercent,
    double MaxPeakWorkingSetMb,
    double MinSendMeanOps,
    double MinSendP50Ops,
    double MinSendP95Ops,
    double MinPublishMeanOps,
    double MinPublishP50Ops,
    double MinPublishP95Ops)
{
    private const string MaxCpuEnvVariable = "SIMPLEMEDIATOR_LOAD_MAX_MEAN_CPU";
    private const string MaxWorkingSetEnvVariable = "SIMPLEMEDIATOR_LOAD_MAX_PEAK_MB";
    private const string MinSendMeanEnvVariable = "SIMPLEMEDIATOR_LOAD_MIN_SEND_MEAN_OPS";
    private const string MinSendP50EnvVariable = "SIMPLEMEDIATOR_LOAD_MIN_SEND_P50_OPS";
    private const string MinSendP95EnvVariable = "SIMPLEMEDIATOR_LOAD_MIN_SEND_P95_OPS";
    private const string MinPublishMeanEnvVariable = "SIMPLEMEDIATOR_LOAD_MIN_PUBLISH_MEAN_OPS";
    private const string MinPublishP50EnvVariable = "SIMPLEMEDIATOR_LOAD_MIN_PUBLISH_P50_OPS";
    private const string MinPublishP95EnvVariable = "SIMPLEMEDIATOR_LOAD_MIN_PUBLISH_P95_OPS";

    public static LoadMetricsCheckOptions Parse(string[] args)
    {
        var directory = Path.Combine("artifacts", "load-metrics");
        var maxCpu = 1.0;
        var maxWorkingSet = 100.0;
        var minSendMean = double.NaN;
        var minSendP50 = double.NaN;
        var minSendP95 = double.NaN;
        var minPublishMean = double.NaN;
        var minPublishP50 = double.NaN;
        var minPublishP95 = double.NaN;

        string? configPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--config")
            {
                configPath = ReadRequired(args, ref index);
            }
        }

        var config = LoadMetricsCheckConfig.Load(configPath);

        if (!string.IsNullOrWhiteSpace(config.MetricsDirectory))
        {
            directory = config.MetricsDirectory!;
        }

        maxCpu = config.MaxMeanCpuPercent ?? maxCpu;
        maxWorkingSet = config.MaxPeakWorkingSetMb ?? maxWorkingSet;
        minSendMean = config.MinSendMeanOps ?? minSendMean;
        minSendP50 = config.MinSendP50Ops ?? minSendP50;
        minSendP95 = config.MinSendP95Ops ?? minSendP95;
        minPublishMean = config.MinPublishMeanOps ?? minPublishMean;
        minPublishP50 = config.MinPublishP50Ops ?? minPublishP50;
        minPublishP95 = config.MinPublishP95Ops ?? minPublishP95;

        maxCpu = ReadEnvDouble(MaxCpuEnvVariable, maxCpu);
        maxWorkingSet = ReadEnvDouble(MaxWorkingSetEnvVariable, maxWorkingSet);
        minSendMean = ReadEnvDouble(MinSendMeanEnvVariable, minSendMean);
        minSendP50 = ReadEnvDouble(MinSendP50EnvVariable, minSendP50);
        minSendP95 = ReadEnvDouble(MinSendP95EnvVariable, minSendP95);
        minPublishMean = ReadEnvDouble(MinPublishMeanEnvVariable, minPublishMean);
        minPublishP50 = ReadEnvDouble(MinPublishP50EnvVariable, minPublishP50);
        minPublishP95 = ReadEnvDouble(MinPublishP95EnvVariable, minPublishP95);

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            switch (current)
            {
                case "--config":
                    // Value already consumed during config discovery
                    index++;
                    break;
                case "--directory":
                    directory = ReadRequired(args, ref index);
                    break;
                case "--max-mean-cpu":
                    maxCpu = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--max-peak-working-set":
                    maxWorkingSet = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--min-send-mean-ops":
                    minSendMean = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--min-send-p50-ops":
                    minSendP50 = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--min-send-p95-ops":
                    minSendP95 = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--min-publish-mean-ops":
                    minPublishMean = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--min-publish-p50-ops":
                    minPublishP50 = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                case "--min-publish-p95-ops":
                    minPublishP95 = double.Parse(ReadRequired(args, ref index), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{current}'.");
            }
        }

        return new LoadMetricsCheckOptions(
            directory,
            maxCpu,
            maxWorkingSet,
            minSendMean,
            minSendP50,
            minSendP95,
            minPublishMean,
            minPublishP50,
            minPublishP95);
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

    private static double ReadEnvDouble(string variableName, double defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantResult))
        {
            return invariantResult;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var cultureResult))
        {
            return cultureResult;
        }

        var normalized = value.Replace(".", string.Empty).Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var normalizedResult))
        {
            return normalizedResult;
        }

        Console.WriteLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "Warning: could not parse environment variable '{0}' value '{1}'. Using default {2}.",
                variableName,
                value,
                double.IsNaN(defaultValue) ? "(no threshold)" : defaultValue.ToString("F2", CultureInfo.InvariantCulture)));

        return defaultValue;
    }

    public bool HasSendThroughputThresholds =>
        !double.IsNaN(MinSendMeanOps) ||
        !double.IsNaN(MinSendP50Ops) ||
        !double.IsNaN(MinSendP95Ops);

    public bool HasPublishThroughputThresholds =>
        !double.IsNaN(MinPublishMeanOps) ||
        !double.IsNaN(MinPublishP50Ops) ||
        !double.IsNaN(MinPublishP95Ops);
}

internal sealed class LoadMetricsCheckConfig
{
    public string? MetricsDirectory { get; set; }
    public double? MaxMeanCpuPercent { get; set; }
    public double? MaxPeakWorkingSetMb { get; set; }
    public double? MinSendMeanOps { get; set; }
    public double? MinSendP50Ops { get; set; }
    public double? MinSendP95Ops { get; set; }
    public double? MinPublishMeanOps { get; set; }
    public double? MinPublishP50Ops { get; set; }
    public double? MinPublishP95Ops { get; set; }

    public static LoadMetricsCheckConfig Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new LoadMetricsCheckConfig();
        }

        if (!File.Exists(path))
        {
            Console.WriteLine($"Warning: config file '{path}' not found. Falling back to defaults.");
            return new LoadMetricsCheckConfig();
        }

        try
        {
            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            });

            var config = new LoadMetricsCheckConfig();

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var name = property.Name.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    switch (name.ToLowerInvariant())
                    {
                        case "metricsdirectory":
                            config.MetricsDirectory = property.Value.ValueKind == JsonValueKind.String
                                ? property.Value.GetString()
                                : config.MetricsDirectory;
                            break;
                        case "maxmeancpupercent":
                            config.MaxMeanCpuPercent = TryGetDouble(property.Value) ?? config.MaxMeanCpuPercent;
                            break;
                        case "maxpeakworkingsetmb":
                            config.MaxPeakWorkingSetMb = TryGetDouble(property.Value) ?? config.MaxPeakWorkingSetMb;
                            break;
                        case "minsendmeanops":
                            config.MinSendMeanOps = TryGetDouble(property.Value) ?? config.MinSendMeanOps;
                            break;
                        case "minsendp50ops":
                            config.MinSendP50Ops = TryGetDouble(property.Value) ?? config.MinSendP50Ops;
                            break;
                        case "minsendp95ops":
                            config.MinSendP95Ops = TryGetDouble(property.Value) ?? config.MinSendP95Ops;
                            break;
                        case "minpublishmeanops":
                            config.MinPublishMeanOps = TryGetDouble(property.Value) ?? config.MinPublishMeanOps;
                            break;
                        case "minpublishp50ops":
                            config.MinPublishP50Ops = TryGetDouble(property.Value) ?? config.MinPublishP50Ops;
                            break;
                        case "minpublishp95ops":
                            config.MinPublishP95Ops = TryGetDouble(property.Value) ?? config.MinPublishP95Ops;
                            break;
                    }
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to parse config file '{path}': {ex.Message}. Using defaults.");
            return new LoadMetricsCheckConfig();
        }
    }

    private static double? TryGetDouble(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number when element.TryGetDouble(out var numeric):
                return numeric;
            case JsonValueKind.String:
                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }

                break;
        }

        return null;
    }
}

internal sealed class MetricsParser
{
    private readonly string _path;

    public MetricsParser(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public LoadMetrics Parse()
    {
        var lines = File.ReadAllLines(_path);
        if (lines.Length <= 1)
        {
            return LoadMetrics.Empty;
        }

        var cpuSamples = new List<double>();
        var workingSetSamples = new List<long>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.None);
            if (parts.Length < 4)
            {
                continue;
            }

            if (double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var cpu))
            {
                cpuSamples.Add(cpu);
            }

            if (long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var workingSet))
            {
                workingSetSamples.Add(workingSet);
            }
        }

        var meanCpu = cpuSamples.Count > 0 ? cpuSamples.Average() : double.NaN;
        var peakWorkingSet = workingSetSamples.Count > 0 ? workingSetSamples.Max() : double.NaN;

        return new LoadMetrics(meanCpu, peakWorkingSet);
    }
}

internal readonly record struct LoadMetrics(double MeanCpuPercent, double PeakWorkingSetBytes)
{
    public static LoadMetrics Empty => new(double.NaN, double.NaN);
}

internal static class LoadLogParser
{
    public static LoadThroughput Parse(string metricsFilePath)
    {
        if (string.IsNullOrWhiteSpace(metricsFilePath))
        {
            return LoadThroughput.Empty;
        }

        var directory = Path.GetDirectoryName(metricsFilePath);
        var token = Path.GetFileNameWithoutExtension(metricsFilePath)?.Replace("metrics-", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(token))
        {
            return LoadThroughput.Empty;
        }

        var logPath = Path.Combine(directory, $"harness-{token}.log");
        if (!File.Exists(logPath))
        {
            return LoadThroughput.Empty;
        }

        double sendMean = double.NaN;
        double sendP50 = double.NaN;
        double sendP95 = double.NaN;
        double publishMean = double.NaN;
        double publishP50 = double.NaN;
        double publishP95 = double.NaN;
        var sendErrors = new List<string>();
        var publishErrors = new List<string>();

        var lines = File.ReadAllLines(logPath);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line.StartsWith("Send throughput", StringComparison.OrdinalIgnoreCase))
            {
                AssignValue(line, ref sendMean, ref sendP50, ref sendP95);
            }
            else if (line.StartsWith("Publish throughput", StringComparison.OrdinalIgnoreCase))
            {
                AssignValue(line, ref publishMean, ref publishP50, ref publishP95);
            }
            else if (line.StartsWith("Sample send errors", StringComparison.OrdinalIgnoreCase))
            {
                index = CaptureErrors(lines, index + 1, sendErrors);
            }
            else if (line.StartsWith("Sample publish errors", StringComparison.OrdinalIgnoreCase))
            {
                index = CaptureErrors(lines, index + 1, publishErrors);
            }
        }

        return new LoadThroughput(sendMean, sendP50, sendP95, publishMean, publishP50, publishP95, sendErrors.ToArray(), publishErrors.ToArray());
    }

    private static int CaptureErrors(string[] lines, int startIndex, List<string> sink)
    {
        var index = startIndex;
        for (; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!line.StartsWith("- ", StringComparison.Ordinal))
            {
                break;
            }

            var message = line[2..].Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                sink.Add(message);
            }
        }

        return index - 1;
    }

    private static void AssignValue(string line, ref double mean, ref double p50, ref double p95)
    {
        if (line.Contains("P50", StringComparison.OrdinalIgnoreCase))
        {
            p50 = ParseThroughputValue(line);
            return;
        }

        if (line.Contains("P95", StringComparison.OrdinalIgnoreCase))
        {
            p95 = ParseThroughputValue(line);
            return;
        }

        mean = ParseThroughputValue(line);
    }

    private static double ParseThroughputValue(string line)
    {
        var colon = line.IndexOf(':');
        if (colon < 0)
        {
            return double.NaN;
        }

        var value = line[(colon + 1)..]
            .Replace("ops/sec", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return ParseDoubleFlexible(value);
    }

    private static double ParseDoubleFlexible(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.NaN;
        }

        var sanitized = value
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        string normalized;

        var lastComma = sanitized.LastIndexOf(',');
        var lastDot = sanitized.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            normalized = lastDot > lastComma
                ? sanitized.Replace(",", string.Empty)
                : sanitized.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (lastComma >= 0)
        {
            normalized = sanitized.Replace(".", string.Empty).Replace(',', '.');
        }
        else
        {
            normalized = sanitized;
        }

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantResult))
        {
            return invariantResult;
        }

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out var cultureResult))
        {
            return cultureResult;
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
    double PublishP95OpsPerSecond,
    string[] SendErrors,
    string[] PublishErrors)
{
    public static LoadThroughput Empty => new(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, Array.Empty<string>(), Array.Empty<string>());

    public bool HasData => !double.IsNaN(SendOpsPerSecond) || !double.IsNaN(PublishOpsPerSecond);

    public bool HasErrors => (SendErrors.Length + PublishErrors.Length) > 0;
}
