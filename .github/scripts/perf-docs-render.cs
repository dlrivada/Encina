// perf-docs-render.cs — Auto-generate benchmark/load-test tables in documentation
//                       from the live docref-index.json, replacing marker blocks.
//
// Usage:
//   dotnet run .github/scripts/perf-docs-render.cs -- \
//     --docref-index docs/benchmarks/data/docref-index.json \
//     --docs-root docs
//
// Design notes (see ADR-025 §4 + performance-measurement-methodology.md):
// - Scans all .md files under docs-root for marker blocks of the form:
//     <!-- docref-table: bench:mediator/* -->
//     (existing content — will be replaced)
//     <!-- /docref-table -->
//   The pattern after "docref-table:" is a glob matched against DocRef IDs.
//
// - Also supports single-value inline markers:
//     <!-- docref: bench:mediator/send-command:medianNs -->
//     (existing value — will be replaced)
//     <!-- /docref -->
//   This injects a single metric value inline in prose.
//
// - Generated tables include a footer with the source run metadata and a
//   deep-link to the dashboard entry.
//
// - If a DocRef cited in a marker has no matching entry in the index, a
//   warning row is emitted: "⚠ DocRef not found: bench:xxx". This makes
//   dangling references visible without breaking the build.
//
// - Hand-edited content OUTSIDE marker blocks is never touched.
//
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var docrefIndexPath = "docs/benchmarks/data/docref-index.json";
var docsRoot = "docs";
var dryRun = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--docref-index" && i + 1 < args.Length) docrefIndexPath = args[++i];
    if (args[i] == "--docs-root" && i + 1 < args.Length) docsRoot = args[++i];
    if (args[i] == "--dry-run") dryRun = true;
}

// Load docref index
if (!File.Exists(docrefIndexPath))
{
    Console.Error.WriteLine($"DocRef index not found: {docrefIndexPath}");
    Console.Error.WriteLine("Run perf-report.cs first to generate it.");
    Environment.Exit(2);
}

var indexJson = JsonNode.Parse(File.ReadAllText(docrefIndexPath)) as JsonObject ?? new JsonObject();
Console.WriteLine($"Loaded {indexJson.Count} DocRef entries from {docrefIndexPath}");

// Regex for table markers: <!-- docref-table: <pattern> -->...<-- /docref-table -->
var tableMarkerRegex = new Regex(
    @"(<!-- docref-table:\s*(?<pattern>[^\s]+)\s*-->).*?(<!-- /docref-table -->)",
    RegexOptions.Singleline | RegexOptions.Compiled);

// Regex for inline markers: <!-- docref: <id>:<field> -->...<-- /docref -->
var inlineMarkerRegex = new Regex(
    @"(<!-- docref:\s*(?<id>[^\s:]+):(?<field>\w+)\s*-->).*?(<!-- /docref -->)",
    RegexOptions.Singleline | RegexOptions.Compiled);

// Find all .md files under docs root
var mdFiles = Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"))
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

int filesModified = 0, tablesGenerated = 0, inlinesGenerated = 0, warnings = 0;

foreach (var file in mdFiles)
{
    var content = File.ReadAllText(file);
    var original = content;

    // Process table markers
    content = tableMarkerRegex.Replace(content, match =>
    {
        var pattern = match.Groups["pattern"].Value;
        var table = GenerateTable(pattern, indexJson, ref warnings);
        tablesGenerated++;
        return $"{match.Groups[1].Value}\n{table}\n{match.Groups[2].Value}";
    });

    // Process inline markers
    content = inlineMarkerRegex.Replace(content, match =>
    {
        var id = match.Groups["id"].Value;
        var field = match.Groups["field"].Value;
        var value = LookupInlineValue(id, field, indexJson, ref warnings);
        inlinesGenerated++;
        return $"{match.Groups[1].Value}{value}{match.Groups[2].Value}";
    });

    if (content != original)
    {
        filesModified++;
        var relPath = Path.GetRelativePath(docsRoot, file);
        if (dryRun)
        {
            Console.WriteLine($"  [dry-run] Would update: {relPath}");
        }
        else
        {
            File.WriteAllText(file, content);
            Console.WriteLine($"  Updated: {relPath}");
        }
    }
}

// Phase 4.1 — Build cited-by.json: reverse index of DocRef → citing documents.
// This is consumed by the dashboard to show a "Cited in" column.
var citedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);
var tablePatternRegex = new Regex(@"<!-- docref-table:\s*(?<pattern>[^\s]+)\s*-->", RegexOptions.Compiled);
var inlineIdRegex = new Regex(@"<!-- docref:\s*(?<id>(?:bench|load):[a-zA-Z0-9\-/]+):", RegexOptions.Compiled);
var proseRefRegex = new Regex(@"(?:bench|load):[a-z][a-z0-9\-]*/[a-z][a-z0-9\-]*(?:[a-z0-9\-])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

foreach (var file in mdFiles)
{
    var relPath = Path.GetRelativePath(docsRoot, file).Replace('\\', '/');
    var lines = File.ReadAllLines(file);
    for (int lineNum = 0; lineNum < lines.Length; lineNum++)
    {
        var line = lines[lineNum];

        // Table markers — expand glob against known DocRefs
        foreach (Match m in tablePatternRegex.Matches(line))
        {
            var pattern = m.Groups["pattern"].Value;
            var glob = GlobToRegex(pattern);
            foreach (var (id, _) in indexJson)
            {
                if (glob.IsMatch(id))
                    AddCitation(citedBy, id, $"{relPath}:{lineNum + 1}");
            }
        }

        // Inline markers
        foreach (Match m in inlineIdRegex.Matches(line))
            AddCitation(citedBy, m.Groups["id"].Value, $"{relPath}:{lineNum + 1}");

        // Prose references (outside markers)
        if (!line.Contains("docref-table:") && !line.Contains("docref:"))
        {
            foreach (Match m in proseRefRegex.Matches(line))
                AddCitation(citedBy, m.Value, $"{relPath}:{lineNum + 1}");
        }
    }
}

// Emit cited-by.json next to the docref-index
var citedByDir = Path.GetDirectoryName(docrefIndexPath);
if (!string.IsNullOrEmpty(citedByDir))
{
    var citedByJson = new JsonObject();
    foreach (var (docRef, locations) in citedBy.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        var arr = new JsonArray();
        foreach (var loc in locations.Distinct().OrderBy(l => l, StringComparer.Ordinal))
            arr.Add(JsonValue.Create(loc));
        citedByJson[docRef] = arr;
    }
    var citedByPath = Path.Combine(citedByDir, "cited-by.json");
    File.WriteAllText(citedByPath, citedByJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Cited-by index: {citedBy.Count} DocRef(s) with citations → {citedByPath}");
}

Console.WriteLine();
Console.WriteLine($"Summary: {filesModified} file(s) modified, {tablesGenerated} table(s), {inlinesGenerated} inline(s), {warnings} warning(s)");
if (warnings > 0)
    Console.WriteLine("  ⚠ Some DocRefs were not found in the index. Check the generated tables for details.");


static string GenerateTable(string pattern, JsonObject index, ref int warnings)
{
    // Match DocRef IDs against the glob pattern.
    // Simple glob: "bench:mediator/*" matches "bench:mediator/send-command" etc.
    // "*" alone matches everything.
    var regex = GlobToRegex(pattern);
    var matches = new List<(string Id, JsonObject Entry)>();

    foreach (var (id, node) in index)
    {
        if (node is JsonObject entry && regex.IsMatch(id))
            matches.Add((id, entry));
    }

    if (matches.Count == 0)
    {
        warnings++;
        return $"\n> ⚠ No DocRef entries matched pattern `{pattern}`\n";
    }

    // Sort by module then method
    matches.Sort((a, b) =>
    {
        var cmp = string.Compare(
            a.Entry["module"]?.GetValue<string>() ?? "",
            b.Entry["module"]?.GetValue<string>() ?? "", StringComparison.Ordinal);
        return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
    });

    var sb = new StringBuilder();
    sb.AppendLine();

    // Phase 4.1 — Deep-link anchors: each row gets an HTML anchor tag so docs
    // can jump to a specific benchmark via #docref-<sanitized-id>. GitHub/Jekyll
    // renders raw HTML in Markdown, so <a id="..."></a> works as a target.
    // The dashboard links to these anchors via the "anchor" field in docref-index.json.
    sb.AppendLine("| DocRef | Method | Median | StdDev | CI99 | Allocated | Stable |");
    sb.AppendLine("|--------|--------|--------|--------|------|-----------|--------|");

    foreach (var (id, entry) in matches)
    {
        var method = entry["method"]?.GetValue<string>() ?? id;
        var median = FormatNs(GetD(entry, "medianNs"));
        var stddev = FormatNs(GetD(entry, "stdDevNs"));
        var ci99L = GetD(entry, "ci99LowerNs");
        var ci99U = GetD(entry, "ci99UpperNs");
        var ci99 = ci99L > 0 && ci99U > 0 ? $"{FormatNs(ci99L)} – {FormatNs(ci99U)}" : "—";
        var alloc = FormatBytes(GetD(entry, "allocatedBytes"));
        var stable = entry["stable"]?.GetValue<bool>() == true ? "✅" : "⚠️";
        var carried = entry["carriedForward"]?.GetValue<bool>() == true ? " *(cf)*" : "";
        var anchorId = SanitizeAnchorId(id);

        sb.AppendLine($"| <a id=\"{anchorId}\"></a>`{id}` | {method}{carried} | {median} | {stddev} | {ci99} | {alloc} | {stable} |");
    }

    sb.AppendLine();
    sb.AppendLine($"*{matches.Count} benchmark(s) matched `{pattern}`. Data from [dashboard](https://dlrivada.github.io/Encina/benchmarks/dashboard/).*");
    return sb.ToString();
}

static string LookupInlineValue(string id, string field, JsonObject index, ref int warnings)
{
    if (!index.TryGetPropertyValue(id, out var node) || node is not JsonObject entry)
    {
        warnings++;
        return $"⚠ `{id}` not found";
    }

    var val = entry[field];
    if (val is null)
    {
        warnings++;
        return $"⚠ field `{field}` not found in `{id}`";
    }

    // Format based on field name
    if (field.EndsWith("Ns", StringComparison.Ordinal))
        return FormatNs(val.GetValue<double>());
    if (field.EndsWith("Bytes", StringComparison.Ordinal))
        return FormatBytes(val.GetValue<double>());
    return val.ToString();
}

static Regex GlobToRegex(string glob)
{
    var escaped = Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".");
    return new Regex($"^{escaped}$", RegexOptions.IgnoreCase);
}

static double GetD(JsonObject obj, string key)
{
    var v = obj[key];
    if (v is null) return 0;
    try { return v.GetValue<double>(); } catch { return 0; }
}

static string FormatNs(double v)
{
    if (v <= 0 || double.IsNaN(v)) return "—";
    if (v < 1000) return $"{v:F2} ns";
    if (v < 1e6) return $"{v / 1000:F2} μs";
    if (v < 1e9) return $"{v / 1e6:F2} ms";
    return $"{v / 1e9:F2} s";
}

static string FormatBytes(double v)
{
    if (v <= 0 || double.IsNaN(v)) return "—";
    if (v < 1024) return $"{v:F0} B";
    if (v < 1024 * 1024) return $"{v / 1024:F2} KB";
    return $"{v / (1024 * 1024):F2} MB";
}

/// <summary>
/// Converts a DocRef ID like "bench:mediator/send-command" into a valid
/// HTML anchor ID like "docref-bench-mediator-send-command". GitHub
/// renders raw HTML in Markdown, so <a id="..."></a> works as a link target.
/// </summary>
static string SanitizeAnchorId(string docRef)
{
    return "docref-" + docRef.Replace(':', '-').Replace('/', '-');
}

static void AddCitation(Dictionary<string, List<string>> citedBy, string docRef, string location)
{
    if (!citedBy.TryGetValue(docRef, out var locs))
    {
        locs = new List<string>();
        citedBy[docRef] = locs;
    }
    locs.Add(location);
}
