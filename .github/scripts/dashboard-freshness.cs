// dashboard-freshness.cs — Fails when a published GitHub Pages dashboard is older than a threshold (#1361, #1382).
//
// Reads the site registry (tools/ai/sites.json by default, #1382) and, for every registry entry
// that carries a maxAgeDays threshold, fetches <base-url>/<dataPath>, takes its top-level
// timestampField (default `timestamp`), prints one line per entry (name, timestamp, age in days,
// OK/STALE/ERROR) and exits 1 when any entry is STALE (older than its threshold) or ERROR
// (unreachable, not JSON, no usable timestamp). The live Pages data is authoritative: the copies
// tracked under docs/*/data are not read.
//
// Usage:
//   dotnet run --file .github/scripts/dashboard-freshness.cs -- \
//       [--registry tools/ai/sites.json] \
//       [--base-url https://dlrivada.github.io/Encina] \
//       [--max-age-days 8] \
//       [--dashboards coverage,mutations,benchmarks,load-tests] \
//       [--now 2026-05-01T00:00:00Z] \
//       [--summary "$GITHUB_STEP_SUMMARY"]
//   dotnet run --file .github/scripts/dashboard-freshness.cs -- --check-registry [--registry tools/ai/sites.json]
//
// Exit codes: 0 every entry is fresh (or --check-registry passes); 1 at least one is STALE or
// ERROR; 2 invalid arguments or an invalid registry (--check-registry).
// Requires: .NET 10+ (C# 14 file-based app).
#pragma warning disable CA1305, CA1310, CA1859, CA1852

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

string? registryPathArg = null;
string? baseUrlOverride = null;
double? maxAgeDaysOverride = null;
string[]? dashboardFilter = null;
DateTimeOffset? nowOverride = null;
string? summaryPath = null;
var checkRegistry = false;

for (var i = 0; i < args.Length; i++)
{
    var hasValue = i + 1 < args.Length;
    switch (args[i])
    {
        case "--check-registry":
            checkRegistry = true;
            break;
        case "--registry" when hasValue:
            registryPathArg = args[++i];
            break;
        case "--base-url" when hasValue:
            baseUrlOverride = args[++i];
            break;
        case "--max-age-days" when hasValue:
            if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMaxAge)
                || !double.IsFinite(parsedMaxAge) || parsedMaxAge <= 0)
                return Usage($"--max-age-days must be a finite number greater than 0, got '{args[i]}'");
            maxAgeDaysOverride = parsedMaxAge;
            break;
        case "--dashboards" when hasValue:
            dashboardFilter = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (dashboardFilter.Length == 0)
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

var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
var registryPath = registryPathArg ?? Path.Combine(repoRoot, "tools", "ai", "sites.json");
if (!Path.IsPathRooted(registryPath))
    registryPath = Path.GetFullPath(registryPath);

if (checkRegistry)
    return ValidateRegistryFile(registryPath);

SiteRegistry registry;
try
{
    registry = LoadRegistry(registryPath);
}
catch (Exception ex) when (ex is IOException or JsonException or RegistryException)
{
    Console.Error.WriteLine($"error: could not load registry '{registryPath}': {ex.Message}");
    return 2;
}

var baseUrl = baseUrlOverride ?? registry.BaseUrl;
var root = baseUrl.TrimEnd('/');

var judged = registry.Sites.Where(s => s.MaxAgeDays is not null || maxAgeDaysOverride is not null).ToList();
if (dashboardFilter is not null)
{
    var known = new HashSet<string>(judged.Select(s => s.Id), StringComparer.Ordinal);
    var unknown = dashboardFilter.Where(d => !known.Contains(d)).ToList();
    if (unknown.Count > 0)
        return Usage($"--dashboards names unknown registry id(s): {string.Join(", ", unknown)}");
    var wanted = new HashSet<string>(dashboardFilter, StringComparer.Ordinal);
    judged = judged.Where(s => wanted.Contains(s.Id)).ToList();
}

var now = nowOverride ?? TimeProvider.System.GetUtcNow();

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Encina-DashboardFreshness/1.0");

Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"Dashboard freshness at {now:yyyy-MM-ddTHH:mm:ssZ} (base {root}, registry {registryPath})"));

var results = new List<Result>();
foreach (var site in judged)
{
    var maxAgeDays = maxAgeDaysOverride ?? site.MaxAgeDays!.Value;
    var dataPath = site.DataPath ?? $"{site.Path.TrimEnd('/')}/data/latest.json";
    var url = $"{root}/{dataPath.TrimStart('/')}";
    var result = await CheckAsync(http, site.Id, url, now, maxAgeDays, site.TimestampField ?? "timestamp", site.TrackingIssue);
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
    md.AppendLine(CultureInfo.InvariantCulture, $"### Dashboard freshness");
    md.AppendLine();
    md.AppendLine("| Dashboard | Timestamp (UTC) | Age (days) | Max age (days) | Status | Detail |");
    md.AppendLine("| --- | --- | ---: | ---: | --- | --- |");
    foreach (var r in results)
        md.AppendLine(r.ToMarkdownRow());
    md.AppendLine();
    File.AppendAllText(summaryPath, md.ToString());
}

return failed == 0 ? 0 : 1;

static async Task<Result> CheckAsync(HttpClient http, string name, string url, DateTimeOffset now, double maxAgeDays, string timestampField, int? trackingIssue)
{
    string body;
    try
    {
        body = await http.GetStringAsync(new Uri(url));
    }
    catch (HttpRequestException ex)
    {
        var code = ex.StatusCode is { } status ? ((int)status).ToString(CultureInfo.InvariantCulture) : "no response";
        return Result.Error(name, maxAgeDays, $"unreachable ({code}): {url}", trackingIssue);
    }
    catch (TaskCanceledException)
    {
        return Result.Error(name, maxAgeDays, $"timed out: {url}", trackingIssue);
    }

    JsonNode? json;
    try
    {
        json = JsonNode.Parse(body);
    }
    catch (JsonException)
    {
        return Result.Error(name, maxAgeDays, $"not JSON: {url}", trackingIssue);
    }

    if (json is not JsonObject obj || obj[timestampField] is not JsonValue value || !value.TryGetValue<string>(out var raw))
        return Result.Error(name, maxAgeDays, $"no top-level string '{timestampField}'", trackingIssue);

    if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp))
        return Result.Error(name, maxAgeDays, $"unparseable timestamp '{raw}'", trackingIssue);

    // A timestamp ahead of the clock would give a negative age and pass as fresh; one hour
    // of tolerance absorbs small clock skew between the publisher and this check.
    if (timestamp - now > TimeSpan.FromHours(1))
        return Result.Error(name, maxAgeDays,
            $"timestamp is in the future: {timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}", trackingIssue);

    var ageDays = (now - timestamp).TotalDays;
    return new Result(name, timestamp, ageDays, maxAgeDays, ageDays > maxAgeDays ? "STALE" : "OK", "", trackingIssue);
}

static int Usage(string message)
{
    Console.Error.WriteLine($"error: {message}");
    Console.Error.WriteLine("usage: dotnet run --file .github/scripts/dashboard-freshness.cs -- [--registry PATH] [--base-url URL] [--max-age-days N] [--dashboards a,b] [--now ISO-8601] [--summary PATH] | --check-registry [--registry PATH]");
    return 2;
}

// ---------------------------------------------------------------------------------------------
// Registry loading and validation (#1382).
// ---------------------------------------------------------------------------------------------

static SiteRegistry LoadRegistry(string path)
{
    if (!File.Exists(path))
        throw new RegistryException($"registry file not found: {path}");

    var json = File.ReadAllText(path);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    if (!root.TryGetProperty("schema", out var schemaEl) || schemaEl.GetInt32() != 1)
        throw new RegistryException("'schema' must be 1");
    if (!root.TryGetProperty("baseUrl", out var baseUrlEl) || baseUrlEl.GetString() is not { Length: > 0 } baseUrl)
        throw new RegistryException("'baseUrl' is required");
    if (!root.TryGetProperty("sites", out var sitesEl) || sitesEl.ValueKind != JsonValueKind.Array)
        throw new RegistryException("'sites' must be an array");

    var sites = new List<SiteEntry>();
    foreach (var siteEl in sitesEl.EnumerateArray())
    {
        var id = siteEl.GetProperty("id").GetString() ?? throw new RegistryException("a site entry is missing 'id'");
        var name = siteEl.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var kind = siteEl.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";
        var sitePath = siteEl.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
        var expectHtml = siteEl.TryGetProperty("expectHtml", out var eh) && eh.GetBoolean();
        var dataPath = siteEl.TryGetProperty("dataPath", out var dp) ? dp.GetString() : null;
        var timestampField = siteEl.TryGetProperty("timestampField", out var tf) ? tf.GetString() : null;
        var publisher = siteEl.TryGetProperty("publisher", out var pub) ? pub.GetString() : null;
        double? maxAgeDays = siteEl.TryGetProperty("maxAgeDays", out var mad) ? mad.GetDouble() : null;
        int? trackingIssue = siteEl.TryGetProperty("trackingIssue", out var ti) ? ti.GetInt32() : null;
        sites.Add(new SiteEntry(id, name, kind, sitePath, expectHtml, dataPath, timestampField, publisher, maxAgeDays, trackingIssue));
    }

    return new SiteRegistry(baseUrl, sites);
}

static int ValidateRegistryFile(string path)
{
    var errors = new List<string>();
    string[] knownKinds = ["docs", "api", "dashboard"];

    JsonDocument doc;
    string json;
    try
    {
        json = File.ReadAllText(path);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"error: could not read registry '{path}': {ex.Message}");
        return 2;
    }

    try
    {
        doc = JsonDocument.Parse(json);
    }
    catch (JsonException ex)
    {
        Console.Error.WriteLine($"error: registry '{path}' is not valid JSON: {ex.Message}");
        return 2;
    }

    using (doc)
    {
        var root = doc.RootElement;

        if (!root.TryGetProperty("schema", out var schemaEl) || schemaEl.ValueKind != JsonValueKind.Number || schemaEl.GetInt32() != 1)
            errors.Add("'schema' must be 1");
        if (!root.TryGetProperty("baseUrl", out var baseUrlEl) || baseUrlEl.GetString() is not { Length: > 0 })
            errors.Add("'baseUrl' is required");

        if (!root.TryGetProperty("sites", out var sitesEl) || sitesEl.ValueKind != JsonValueKind.Array)
        {
            errors.Add("'sites' must be an array");
        }
        else
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;
            foreach (var siteEl in sitesEl.EnumerateArray())
            {
                var where = $"sites[{index}]";
                index++;

                var id = siteEl.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add($"{where}.id is required");
                }
                else if (!seenIds.Add(id))
                {
                    errors.Add($"duplicate site id '{id}'");
                }

                var kind = siteEl.TryGetProperty("kind", out var kindEl) ? kindEl.GetString() : null;
                if (string.IsNullOrEmpty(kind) || !knownKinds.Contains(kind))
                    errors.Add($"{where}.kind has an unknown value '{kind}' (expected one of: {string.Join(", ", knownKinds)})");

                var sitePath = siteEl.TryGetProperty("path", out var pathEl) ? pathEl.GetString() : null;
                if (sitePath is null)
                {
                    errors.Add($"{where}.path is required");
                }
                else if (sitePath.StartsWith('/'))
                {
                    errors.Add($"{where}.path must not start with '/': '{sitePath}'");
                }

                var hasDataPath = siteEl.TryGetProperty("dataPath", out var dataPathEl) && dataPathEl.ValueKind != JsonValueKind.Null;
                if (hasDataPath)
                {
                    var dataPath = dataPathEl.GetString();
                    if (!string.IsNullOrEmpty(dataPath) && dataPath.StartsWith('/'))
                        errors.Add($"{where}.dataPath must not start with '/': '{dataPath}'");

                    var hasMaxAge = siteEl.TryGetProperty("maxAgeDays", out var madEl) && madEl.ValueKind != JsonValueKind.Null;
                    if (!hasMaxAge && kind != "dashboard")
                        errors.Add($"{where}.dataPath requires either 'maxAgeDays' or kind 'dashboard' (id: {id ?? "?"})");
                }
            }
        }
    }

    if (errors.Count > 0)
    {
        foreach (var e in errors)
            Console.Error.WriteLine($"error: {path}: {e}");
        return 2;
    }

    Console.WriteLine($"Registry OK: {path}");
    return 0;
}

static string FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(Path.GetFullPath(startDir));
    while (dir is not null)
    {
        // In a git worktree or submodule, .git is a file (pointing at the real gitdir), not a
        // directory, so both are checked.
        var gitPath = Path.Combine(dir.FullName, ".git");
        if (Directory.Exists(gitPath) || File.Exists(gitPath)) return dir.FullName;
        dir = dir.Parent;
    }
    return Directory.GetCurrentDirectory();
}

sealed class RegistryException(string message) : Exception(message);

sealed record SiteRegistry(string BaseUrl, List<SiteEntry> Sites);

sealed record SiteEntry(
    string Id,
    string Name,
    string Kind,
    string Path,
    bool ExpectHtml,
    string? DataPath,
    string? TimestampField,
    string? Publisher,
    double? MaxAgeDays,
    int? TrackingIssue);

sealed record Result(string Name, DateTimeOffset? Timestamp, double? AgeDays, double MaxAgeDays, string Status, string Detail, int? TrackingIssue)
{
    public static Result Error(string name, double maxAgeDays, string detail, int? trackingIssue) => new(name, null, null, maxAgeDays, "ERROR", detail, trackingIssue);

    public string ToLine() => string.Create(CultureInfo.InvariantCulture,
        $"{Name,-12} {FormatTimestamp(),-20} {FormatAge(),9} / {MaxAgeDays,-6:0.##} days  {Status}{(Detail.Length > 0 ? " - " + Detail : "")}{TrackedSuffix()}");

    public string ToMarkdownRow() => string.Create(CultureInfo.InvariantCulture,
        $"| {Name} | {FormatTimestamp()} | {FormatAge()} | {MaxAgeDays:0.##} | {Status} | {(Detail + TrackedSuffix()).Replace("|", "\\|", StringComparison.Ordinal)} |");

    private string TrackedSuffix() => TrackingIssue is { } n && Status != "OK" ? $" (tracked in #{n})" : "";

    private string FormatTimestamp() =>
        Timestamp is { } ts ? ts.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) : "-";

    private string FormatAge() =>
        AgeDays is { } age ? age.ToString("0.0", CultureInfo.InvariantCulture) : "-";
}
