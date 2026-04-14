// mut-docs-render.cs — Auto-generate mutation tables in documentation
//                      from the live mutations docref-index.json, replacing marker blocks.
//
// Usage:
//   dotnet run .github/scripts/mut-docs-render.cs -- \
//     --docref-index docs/mutations/data/docref-index.json \
//     --docs-root docs \
//     [--scan-roots .]   // additional roots to scan (e.g. src for per-package READMEs)
//
// Design notes (mirrors perf-docs-render.cs — see mutation-measurement-methodology.md):
// - Scans .md files under docs-root + scan-roots for marker blocks of the form:
//     <!-- mutref-table: mut:Encina/Sharding/Migrations/Strategies/* -->
//     (existing content — will be replaced)
//     <!-- /mutref-table -->
//   The pattern after "mutref-table:" is a glob matched against DocRef IDs.
//
// - Single-value inline markers:
//     <!-- mutref: mut:Encina/Sharding/.../Foo.cs:score -->
//     (existing value — will be replaced)
//     <!-- /mutref -->
//   This injects a single metric value inline in prose.
//
// - Builds cited-by.json: reverse index DocRef -> citing locations
//   (file:line). Includes citations from markers AND from free-form prose
//   mentions like "see mut:Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs".
//
// - Hand-edited content OUTSIDE marker blocks is never touched.
//
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var docrefIndexPath = "docs/mutations/data/docref-index.json";
var docsRoot = "docs";
var scanRoots = new List<string>();
var dryRun = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--docref-index" && i + 1 < args.Length) docrefIndexPath = args[++i];
    if (args[i] == "--docs-root" && i + 1 < args.Length) docsRoot = args[++i];
    if (args[i] == "--scan-roots" && i + 1 < args.Length) scanRoots.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
    if (args[i] == "--dry-run") dryRun = true;
}

if (!File.Exists(docrefIndexPath))
{
    Console.Error.WriteLine($"DocRef index not found: {docrefIndexPath}");
    Console.Error.WriteLine("Run mutation-history.cs first to generate it.");
    Environment.Exit(2);
}

var indexJson = JsonNode.Parse(File.ReadAllText(docrefIndexPath)) as JsonObject ?? new JsonObject();
Console.WriteLine($"Loaded {indexJson.Count} DocRef entries from {docrefIndexPath}");

var tableMarkerRegex = new Regex(
    @"(<!-- mutref-table:\s*(?<pattern>[^\s]+)\s*-->).*?(<!-- /mutref-table -->)",
    RegexOptions.Singleline | RegexOptions.Compiled);

var inlineMarkerRegex = new Regex(
    @"(<!-- mutref:\s*(?<id>[^\s:]+:[^\s:]+):(?<field>\w+)\s*-->).*?(<!-- /mutref -->)",
    RegexOptions.Singleline | RegexOptions.Compiled);

// Collect .md files from docs root and any extra scan roots
var mdFiles = new List<string>();
mdFiles.AddRange(Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories));
foreach (var root in scanRoots)
{
    if (!Directory.Exists(root)) continue;
    mdFiles.AddRange(Directory.GetFiles(root, "*.md", SearchOption.AllDirectories));
}
mdFiles = mdFiles
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"))
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.claude{Path.DirectorySeparatorChar}"))
    .Distinct()
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

int filesModified = 0, tablesGenerated = 0, inlinesGenerated = 0, warnings = 0;

foreach (var file in mdFiles)
{
    var content = File.ReadAllText(file);
    var original = content;

    // Skip markers inside fenced code blocks (```...```). We tokenize the
    // file into runs of code-block / non-code-block, replace markers only
    // in the latter, and stitch back together. This lets the methodology
    // doc show example marker syntax in code fences without expansion.
    content = ProcessOutsideCodeFences(content, segment =>
    {
        segment = tableMarkerRegex.Replace(segment, match =>
        {
            var pattern = match.Groups["pattern"].Value;
            var table = GenerateTable(pattern, indexJson, file, ref warnings);
            tablesGenerated++;
            return $"{match.Groups[1].Value}\n{table}\n{match.Groups[2].Value}";
        });
        segment = inlineMarkerRegex.Replace(segment, match =>
        {
            var id = match.Groups["id"].Value;
            var field = match.Groups["field"].Value;
            var value = LookupInlineValue(id, field, indexJson, ref warnings);
            inlinesGenerated++;
            return $"{match.Groups[1].Value}{value}{match.Groups[2].Value}";
        });
        return segment;
    });

    if (content != original)
    {
        filesModified++;
        if (dryRun)
            Console.WriteLine($"  [dry-run] Would update: {file}");
        else
        {
            File.WriteAllText(file, content);
            Console.WriteLine($"  Updated: {file}");
        }
    }
}

// ── Build cited-by.json ──────────────────────────────────────────────
var citedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);
var tablePatternRegex = new Regex(@"<!-- mutref-table:\s*(?<pattern>[^\s]+)\s*-->", RegexOptions.Compiled);
var inlineIdRegex = new Regex(@"<!-- mutref:\s*(?<id>mut:[a-zA-Z0-9./_\-]+):", RegexOptions.Compiled);
// Free-form prose mentions: mut:<package>/<path-to>.cs
var proseRefRegex = new Regex(@"mut:[A-Za-z][A-Za-z0-9.]*\/[A-Za-z0-9./_\-]+\.cs", RegexOptions.Compiled);

foreach (var file in mdFiles)
{
    var relPath = Path.GetRelativePath(".", file).Replace('\\', '/');
    var lines = File.ReadAllLines(file);
    var inFence = false;
    string? fenceMarker = null;
    for (int lineNum = 0; lineNum < lines.Length; lineNum++)
    {
        var line = lines[lineNum];

        // Track fenced code blocks — citations inside fences don't count.
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
        {
            var marker = trimmed.StartsWith("```", StringComparison.Ordinal) ? "```" : "~~~";
            if (!inFence) { inFence = true; fenceMarker = marker; continue; }
            if (marker == fenceMarker) { inFence = false; fenceMarker = null; continue; }
        }
        if (inFence) continue;

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

        foreach (Match m in inlineIdRegex.Matches(line))
            AddCitation(citedBy, m.Groups["id"].Value, $"{relPath}:{lineNum + 1}");

        // Prose references — capture even outside markers but skip lines that are markers themselves.
        if (!line.Contains("mutref-table:") && !line.Contains("mutref:"))
        {
            foreach (Match m in proseRefRegex.Matches(line))
            {
                // Only count if it appears in our docref index — avoids matching arbitrary `mut:` strings.
                if (indexJson.ContainsKey(m.Value))
                    AddCitation(citedBy, m.Value, $"{relPath}:{lineNum + 1}");
            }
        }
    }
}

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
    Console.WriteLine($"cited-by index: {citedBy.Count} DocRef(s) with citations → {citedByPath}");
}

Console.WriteLine();
Console.WriteLine($"Summary: {filesModified} file(s) modified, {tablesGenerated} table(s), {inlinesGenerated} inline(s), {warnings} warning(s)");
if (warnings > 0)
    Console.WriteLine("  ⚠ Some DocRefs were not found in the index. Check the generated tables for details.");

return 0;


static string GenerateTable(string pattern, JsonObject index, string sourceFile, ref int warnings)
{
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

    matches.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));

    var sb = new StringBuilder();
    sb.AppendLine();
    sb.AppendLine("| File | Score | Killed | Survived | NoCov | Total | Last run |");
    sb.AppendLine("|------|------:|-------:|---------:|------:|------:|----------|");

    foreach (var (id, entry) in matches)
    {
        var path = entry["path"]?.GetValue<string>() ?? id;
        var score = GetD(entry, "score");
        var killed = (int)GetD(entry, "killed");
        var survived = (int)GetD(entry, "survived");
        var noCov = (int)GetD(entry, "noCoverage");
        var total = (int)GetD(entry, "total");
        var lastRun = entry["lastRun"]?.GetValue<string>() ?? "—";
        var lastRunShort = lastRun.Length >= 10 ? lastRun.Substring(0, 10) : lastRun;
        var dashUrl = entry["dashboardUrl"]?.GetValue<string>() ?? "";
        var anchorId = SanitizeAnchorId(id);

        // Show the file path as link to dashboard. Add anchor for inbound links.
        var fileCell = string.IsNullOrEmpty(dashUrl)
            ? $"<a id=\"{anchorId}\"></a>`{path}`"
            : $"<a id=\"{anchorId}\"></a>[`{path}`]({dashUrl})";

        sb.AppendLine($"| {fileCell} | {score:F2}% | {killed} | {survived} | {noCov} | {total} | {lastRunShort} |");
    }

    var methodologyRel = ComputeRelativePath(sourceFile, "docs/testing/mutation-measurement-methodology.md");
    sb.AppendLine();
    sb.AppendLine($"*{matches.Count} file(s) matched `{pattern}`. Data from [mutations dashboard](https://dlrivada.github.io/Encina/mutations/). See [mutation-measurement-methodology.md]({methodologyRel}).*");
    return sb.ToString();
}

static string ComputeRelativePath(string sourceFile, string targetPath)
{
    var sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourceFile)) ?? ".";
    var targetFull = Path.GetFullPath(targetPath);
    var rel = Path.GetRelativePath(sourceDir, targetFull);
    return rel.Replace('\\', '/');
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

    if (field == "score")
    {
        try { return $"{val.GetValue<double>():F2}%"; } catch { return val.ToString(); }
    }
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

static string SanitizeAnchorId(string docRef)
{
    return "mutref-" + docRef.Replace(':', '-').Replace('/', '-').Replace('.', '-');
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

/// <summary>
/// Applies <paramref name="transform"/> to every region of the input that is
/// NOT inside a fenced code block (lines wrapped by ``` or ~~~ fences).
/// Code-fenced content is preserved unchanged so docs can show example
/// marker syntax without triggering expansion.
/// </summary>
static string ProcessOutsideCodeFences(string input, Func<string, string> transform)
{
    var lines = input.Split('\n');
    var sb = new StringBuilder();
    var buffer = new StringBuilder();
    var inFence = false;
    string? fenceMarker = null;

    void FlushBuffer()
    {
        if (buffer.Length == 0) return;
        sb.Append(transform(buffer.ToString()));
        buffer.Clear();
    }

    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        var trimmed = line.TrimStart();
        var isFence = trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal);

        if (isFence)
        {
            var marker = trimmed.StartsWith("```", StringComparison.Ordinal) ? "```" : "~~~";
            if (!inFence)
            {
                FlushBuffer();
                inFence = true;
                fenceMarker = marker;
                sb.Append(line);
                if (i < lines.Length - 1) sb.Append('\n');
                continue;
            }
            else if (marker == fenceMarker)
            {
                inFence = false;
                fenceMarker = null;
                sb.Append(line);
                if (i < lines.Length - 1) sb.Append('\n');
                continue;
            }
        }

        if (inFence)
        {
            sb.Append(line);
            if (i < lines.Length - 1) sb.Append('\n');
        }
        else
        {
            buffer.Append(line);
            if (i < lines.Length - 1) buffer.Append('\n');
        }
    }
    FlushBuffer();
    return sb.ToString();
}
