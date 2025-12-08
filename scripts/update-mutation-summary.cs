using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

var report = ResolveLatestReport();
if (report is null)
{
    return 1;
}

var (reportPath, reportJson) = report.Value;
using (reportJson)
{
    var summaryResult = ComputeSummary(reportJson);
    if (summaryResult is null)
    {
        Console.Error.WriteLine("Unable to compute mutation summary from report.");
        return 1;
    }

    var summary = summaryResult;
    PrintSummary(reportPath, summary);
    PersistTextSummary(reportPath, summary);

    var readmePath = FindFile("README.md");
    if (readmePath is null)
    {
        Console.Error.WriteLine("README.md not found; skipping badge update.");
        return 0;
    }

    UpdateBadge(readmePath, summary);
    return 0;
}

static (string ReportPath, JsonDocument ReportJson)? ResolveLatestReport()
{
    var searchRoot = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(searchRoot))
    {
        var candidate = Path.Combine(searchRoot, "StrykerOutput");
        if (Directory.Exists(candidate))
        {
            var latestRun = Directory
                .EnumerateDirectories(candidate)
                .OrderByDescending(path => path)
                .FirstOrDefault();

            if (latestRun is null)
            {
                Console.Error.WriteLine($"No Stryker runs found under {candidate}.");
                return null;
            }

            var jsonPath = Path.Combine(latestRun, "reports", "mutation-report.json");
            if (!File.Exists(jsonPath))
            {
                Console.Error.WriteLine($"JSON report not found at {jsonPath}.");
                return null;
            }

            var jsonBytes = File.ReadAllBytes(jsonPath);
            var document = JsonDocument.Parse(jsonBytes);
            return (jsonPath, document);
        }

        searchRoot = Directory.GetParent(searchRoot)?.FullName;
    }

    Console.Error.WriteLine("StrykerOutput folder not found. Run Stryker before executing this script.");
    return null;
}

static MutationSummary? ComputeSummary(JsonDocument report)
{
    var root = report.RootElement;
    if (!root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
    {
        Console.Error.WriteLine("Report format does not expose a 'files' object.");
        return null;
    }

    var counts = new MutationCounts();

    foreach (var fileEntry in files.EnumerateObject())
    {
        if (!fileEntry.Value.TryGetProperty("mutants", out var mutants) || mutants.ValueKind != JsonValueKind.Array)
        {
            continue;
        }

        foreach (var mutant in mutants.EnumerateArray())
        {
            if (!mutant.TryGetProperty("status", out var statusElement) || statusElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var status = statusElement.GetString();
            if (status is null)
            {
                continue;
            }

            counts.Register(status);
        }
    }

    var thresholds = TryParseThresholds(root);

    if (counts.TotalConsidered == 0)
    {
        Console.Error.WriteLine("No eligible mutants found in report; cannot compute score.");
        return null;
    }

    var mutationScore = 100.0 * counts.Detected / counts.TotalConsidered;
    var roundedScore = Math.Round(mutationScore, 2, MidpointRounding.AwayFromZero);

    var summary = new MutationSummary(
        roundedScore,
        counts.TotalConsidered,
        counts.Detected,
        counts.Killed,
        counts.Survived,
        counts.NoCoverage,
        counts.RuntimeErrors,
        counts.CompileErrors,
        counts.Timeouts,
        counts.Ignored,
        thresholds);

    return summary;
}

static MutationThresholds TryParseThresholds(JsonElement root)
{
    if (root.TryGetProperty("thresholds", out var thresholdsElement) && thresholdsElement.ValueKind == JsonValueKind.Object)
    {
        var high = thresholdsElement.TryGetProperty("high", out var highElement) && highElement.TryGetDouble(out var highValue)
            ? highValue
            : 85d;
        var low = thresholdsElement.TryGetProperty("low", out var lowElement) && lowElement.TryGetDouble(out var lowValue)
            ? lowValue
            : 70d;

        return new MutationThresholds(high, low);
    }

    return new MutationThresholds(85, 70);
}

static void PrintSummary(string reportPath, MutationSummary summary)
{
    Console.WriteLine($"Report     : {reportPath}");
    Console.WriteLine($"Score      : {summary.MutationScore.ToString("F2", CultureInfo.InvariantCulture)}%");
    Console.WriteLine($"Total      : {summary.Total}");
    Console.WriteLine($"Killed     : {summary.Killed}");
    Console.WriteLine($"Detected   : {summary.Detected}");
    Console.WriteLine($"Survived   : {summary.Survived}");
    Console.WriteLine($"No coverage: {summary.NoCoverage}");
    Console.WriteLine($"Timeouts   : {summary.Timeouts}");
    Console.WriteLine($"RuntimeErr : {summary.RuntimeErrors}");
    Console.WriteLine($"CompileErr : {summary.CompileErrors}");
    Console.WriteLine($"Ignored    : {summary.Ignored}");
}

static void PersistTextSummary(string reportPath, MutationSummary summary)
{
    var reportsDirectory = Path.GetDirectoryName(reportPath);
    if (string.IsNullOrEmpty(reportsDirectory))
    {
        return;
    }

    var summaryPath = Path.Combine(reportsDirectory, "mutation-report.txt");
    var lines = new[]
    {
        $"Mutation Score: {summary.MutationScore.ToString("F2", CultureInfo.InvariantCulture)}%",
        $"Total Mutants : {summary.Total}",
        $"Detected      : {summary.Detected}",
        $"Killed        : {summary.Killed}",
        $"Survived      : {summary.Survived}",
        $"No Coverage   : {summary.NoCoverage}",
        $"Timeouts      : {summary.Timeouts}",
        $"Runtime Errors: {summary.RuntimeErrors}",
        $"Compile Errors: {summary.CompileErrors}",
        $"Ignored       : {summary.Ignored}"
    };

    File.WriteAllLines(summaryPath, lines);
}

static void UpdateBadge(string readmePath, MutationSummary summary)
{
    var original = File.ReadAllText(readmePath);
    var badgeRegex = new Regex(@"!\[Mutation\]\(https://img\.shields\.io/badge/mutation-[0-9]+(\.[0-9]+)?%25-[A-F0-9]{6}\.svg\)", RegexOptions.Compiled);

    var color = SelectColor(summary.MutationScore, summary.Thresholds);
    var replacement = $"![Mutation](https://img.shields.io/badge/mutation-{summary.MutationScore.ToString("F2", CultureInfo.InvariantCulture)}%25-{color}.svg)";

    if (!badgeRegex.IsMatch(original))
    {
        Console.Error.WriteLine("Mutation badge not found in README; printing suggested badge below.\n");
        Console.WriteLine(replacement);
        return;
    }

    var updated = badgeRegex.Replace(original, replacement, 1);
    if (updated == original)
    {
        return;
    }

    File.WriteAllText(readmePath, updated);
    Console.WriteLine($"README updated with mutation badge {summary.MutationScore.ToString("F2", CultureInfo.InvariantCulture)}%.");
}

static string SelectColor(double mutationScore, MutationThresholds thresholds)
{
    if (mutationScore >= thresholds.High)
    {
        return "4C934C";
    }

    if (mutationScore >= thresholds.Low)
    {
        return "E4B51C";
    }

    return "C15C59";
}

static string? FindFile(string relativePath)
{
    var searchRoot = Directory.GetCurrentDirectory();
    while (!string.IsNullOrEmpty(searchRoot))
    {
        var candidate = Path.Combine(searchRoot, relativePath);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        searchRoot = Directory.GetParent(searchRoot)?.FullName;
    }

    return null;
}

sealed record MutationSummary(
    double MutationScore,
    int Total,
    int Detected,
    int Killed,
    int Survived,
    int NoCoverage,
    int RuntimeErrors,
    int CompileErrors,
    int Timeouts,
    int Ignored,
    MutationThresholds Thresholds);

sealed record MutationThresholds(double High, double Low);

file sealed class MutationCounts
{
    private int _killed;
    private int _survived;
    private int _noCoverage;
    private int _runtimeErrors;
    private int _compileErrors;
    private int _timeouts;
    private int _ignored;

    public int Detected => _killed + _timeouts + _runtimeErrors;
    public int Killed => _killed;
    public int Survived => _survived;
    public int NoCoverage => _noCoverage;
    public int RuntimeErrors => _runtimeErrors;
    public int CompileErrors => _compileErrors;
    public int Timeouts => _timeouts;
    public int Ignored => _ignored;

    public int TotalConsidered { get; private set; }

    public void Register(string status)
    {
        switch (status)
        {
            case "Killed":
                _killed++;
                IncrementTotal();
                break;
            case "Survived":
                _survived++;
                IncrementTotal();
                break;
            case "NoCoverage":
                _noCoverage++;
                IncrementTotal();
                break;
            case "RuntimeError":
                _runtimeErrors++;
                IncrementTotal();
                break;
            case "CompileError":
                _compileErrors++;
                break;
            case "Timeout":
            case "TimedOut":
                _timeouts++;
                IncrementTotal();
                break;
            case "Ignored":
            case "Skipped":
            case "Pending":
                _ignored++;
                break;
            default:
                IncrementTotal();
                break;
        }
    }

    private void IncrementTotal()
    {
        TotalConsidered++;
    }
}
