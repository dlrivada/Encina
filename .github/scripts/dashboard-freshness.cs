// dashboard-freshness.cs — Fails when a published GitHub Pages dashboard is older than a threshold (#1361).
//
// Reads <base-url>/<dashboard>/data/latest.json for every dashboard, takes its top-level `timestamp`,
// prints one line per dashboard (name, timestamp, age in days, OK/STALE/ERROR) and exits 1 when any
// dashboard is STALE (older than --max-age-days) or ERROR (unreachable, not JSON, no usable timestamp).
// The live Pages data is authoritative: the copies tracked under docs/*/data are not read.
//
// Usage:
//   dotnet run --file .github/scripts/dashboard-freshness.cs -- \
//       [--base-url https://dlrivada.github.io/Encina] \
//       [--max-age-days 8] \
//       [--dashboards coverage,mutations,benchmarks,load-tests] \
//       [--now 2026-05-01T00:00:00Z] \
//       [--summary "$GITHUB_STEP_SUMMARY"]
//
// Exit codes: 0 every dashboard is fresh; 1 at least one is STALE or ERROR; 2 invalid arguments.
// Requires: .NET 10+ (C# 14 file-based app).

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var baseUrl = "https://dlrivada.github.io/Encina";
var maxAgeDays = 8.0;
string[] dashboards = ["coverage", "mutations", "benchmarks", "load-tests"];
DateTimeOffset? nowOverride = null;
string? summaryPath = null;

for (var i = 0; i < args.Length; i++)
{
    var hasValue = i + 1 < args.Length;
    switch (args[i])
    {
        case "--base-url" when hasValue:
            baseUrl = args[++i];
            break;
        case "--max-age-days" when hasValue:
            if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out maxAgeDays) || maxAgeDays < 0)
                return Usage($"--max-age-days must be a non-negative number, got '{args[i]}'");
            break;
        case "--dashboards" when hasValue:
            dashboards = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (dashboards.Length == 0)
                return Usage("--dashboards needs at least one name");
            break;
        case "--now" when hasValue:
            if (!DateTimeOffset.TryParse(args[++i], CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedNow))
                return Usage($"--now must be an ISO 8601 timestamp, got '{args[i]}'");
            nowOverride = parsedNow;
            break;
        case "--summary" when hasValue:
            summaryPath = args[++i];
            break;
        default:
            return Usage($"unknown or incomplete argument '{args[i]}'");
    }
}

var now = nowOverride ?? TimeProvider.System.GetUtcNow();
var root = baseUrl.TrimEnd('/');

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Encina-DashboardFreshness/1.0");

Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"Dashboard freshness at {now:yyyy-MM-ddTHH:mm:ssZ} (max age {maxAgeDays:0.##} days, base {root})"));

var results = new List<Result>();
foreach (var name in dashboards)
{
    var url = $"{root}/{name}/data/latest.json";
    var result = await CheckAsync(http, name, url, now, maxAgeDays);
    results.Add(result);
    Console.WriteLine(result.ToLine());
}

var failed = results.Count(r => r.Status != "OK");
Console.WriteLine(failed == 0
    ? "All dashboards are fresh."
    : string.Create(CultureInfo.InvariantCulture, $"{failed} dashboard(s) are STALE or in ERROR."));

if (!string.IsNullOrWhiteSpace(summaryPath))
{
    var md = new StringBuilder();
    md.AppendLine(CultureInfo.InvariantCulture, $"### Dashboard freshness (max age {maxAgeDays:0.##} days)");
    md.AppendLine();
    md.AppendLine("| Dashboard | Timestamp (UTC) | Age (days) | Status | Detail |");
    md.AppendLine("| --- | --- | ---: | --- | --- |");
    foreach (var r in results)
        md.AppendLine(r.ToMarkdownRow());
    md.AppendLine();
    File.AppendAllText(summaryPath, md.ToString());
}

return failed == 0 ? 0 : 1;

static async Task<Result> CheckAsync(HttpClient http, string name, string url, DateTimeOffset now, double maxAgeDays)
{
    string body;
    try
    {
        body = await http.GetStringAsync(new Uri(url));
    }
    catch (HttpRequestException ex)
    {
        var code = ex.StatusCode is { } status ? ((int)status).ToString(CultureInfo.InvariantCulture) : "no response";
        return Result.Error(name, $"unreachable ({code}): {url}");
    }
    catch (TaskCanceledException)
    {
        return Result.Error(name, $"timed out: {url}");
    }

    JsonNode? json;
    try
    {
        json = JsonNode.Parse(body);
    }
    catch (JsonException)
    {
        return Result.Error(name, $"not JSON: {url}");
    }

    if (json is not JsonObject obj || obj["timestamp"] is not JsonValue value || !value.TryGetValue<string>(out var raw))
        return Result.Error(name, "no top-level string 'timestamp'");

    if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
        return Result.Error(name, $"unparseable timestamp '{raw}'");

    var ageDays = (now - timestamp).TotalDays;
    return new Result(name, timestamp, ageDays, ageDays > maxAgeDays ? "STALE" : "OK", "");
}

static int Usage(string message)
{
    Console.Error.WriteLine($"error: {message}");
    Console.Error.WriteLine("usage: dotnet run --file .github/scripts/dashboard-freshness.cs -- [--base-url URL] [--max-age-days N] [--dashboards a,b] [--now ISO-8601] [--summary PATH]");
    return 2;
}

sealed record Result(string Name, DateTimeOffset? Timestamp, double? AgeDays, string Status, string Detail)
{
    public static Result Error(string name, string detail) => new(name, null, null, "ERROR", detail);

    public string ToLine() => string.Create(CultureInfo.InvariantCulture,
        $"{Name,-12} {FormatTimestamp(),-20} {FormatAge(),9} days  {Status}{(Detail.Length > 0 ? " - " + Detail : "")}");

    public string ToMarkdownRow() => string.Create(CultureInfo.InvariantCulture,
        $"| {Name} | {FormatTimestamp()} | {FormatAge()} | {Status} | {Detail.Replace("|", "\\|", StringComparison.Ordinal)} |");

    private string FormatTimestamp() =>
        Timestamp is { } ts ? ts.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) : "-";

    private string FormatAge() =>
        AgeDays is { } age ? age.ToString("0.0", CultureInfo.InvariantCulture) : "-";
}
