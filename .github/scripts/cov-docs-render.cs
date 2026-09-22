// cov-docs-render.cs — Coverage DocRef citations (SPEC-001): expands covref markers in documentation
//                      from the live coverage docref-index.json, builds cited-by.json, and validates
//                      citations without any index (--check-dangling).
//
// Usage:
//   Render (Publish Coverage):
//     dotnet run .github/scripts/cov-docs-render.cs -- \
//       --docref-index docs/coverage/data/docref-index.json \
//       --docs-root docs [--scan-roots src] [--dry-run]
//
//   Gate (ci.yml, every pull request):
//     dotnet run .github/scripts/cov-docs-render.cs -- --check-dangling \
//       --docs-root docs --scan-roots src \
//       [--manifest-dir .github/coverage-manifest] [--src-root src]
//
// Markers (see docs/testing/coverage-measurement-methodology.md#docref-convention):
//   <!-- covref-table: cov:Encina.Marten/Projections/* -->  (generated table)  <!-- /covref-table -->
//   <!-- covref: cov:Encina.Marten/Foo.cs:coverage -->       (generated value)  <!-- /covref -->
// Prose mentions such as "see cov:Encina.Marten/Foo.cs" are citations too (cited-by + gate).
//
// Scanned files: every .md under --docs-root and each --scan-roots entry, plus the .md files at the
// repository root (README, CHANGELOG, CONTRIBUTING).
//
// Rules shared by render, cited-by and gate:
// - Content inside fenced code blocks is literal. A fence closes only on the same character, at
//   least as long as the opening run, with nothing after it (CommonMark), so ```` fences can hold
//   ``` examples.
// - Inline code spans (`...`) are literal too: markers and IDs quoted in them are neither
//   expanded, cited nor validated.
// - A marker block may not contain another covref opener: an unclosed opener is left untouched by
//   the renderer (with a warning) and is an error for the gate, so hand-written text between an
//   unclosed opener and a later closer is never swallowed (INV-001).
// - Hand-edited content outside marker blocks is never modified.
//
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var docrefIndexPath = "docs/coverage/data/docref-index.json";
var docsRoot = "docs";
var scanRoots = new List<string>();
var dryRun = false;
var checkDangling = false;
var manifestDir = ".github/coverage-manifest";
var srcRoot = "src";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--docref-index" && i + 1 < args.Length) docrefIndexPath = args[++i];
    if (args[i] == "--docs-root" && i + 1 < args.Length) docsRoot = args[++i];
    if (args[i] == "--scan-roots" && i + 1 < args.Length) scanRoots.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
    if (args[i] == "--dry-run") dryRun = true;
    if (args[i] == "--check-dangling") checkDangling = true;
    if (args[i] == "--manifest-dir" && i + 1 < args.Length) manifestDir = args[++i];
    if (args[i] == "--src-root" && i + 1 < args.Length) srcRoot = args[++i];
}

// ── Grammar ──────────────────────────────────────────────────────────────────
// A block's body may not contain another covref opener (tempered token), so an unclosed opener
// never pairs with a later block's closer.
var tableBlockRegex = new Regex(
    @"(?<open><!-- covref-table:\s*(?<pattern>[^\s]+)\s*-->)(?<body>(?:(?!<!--\s*covref).)*?)(?<close><!-- /covref-table -->)",
    RegexOptions.Singleline | RegexOptions.Compiled);
var inlineBlockRegex = new Regex(
    @"(?<open><!-- covref:\s*(?<id>cov:[^\s:]+):(?<field>[A-Za-z]+)\s*-->)(?<body>(?:(?!<!--\s*/?covref).)*?)(?<close><!-- /covref -->)",
    RegexOptions.Singleline | RegexOptions.Compiled);
// Anything that looks like a covref marker, well-formed or not (used to find malformed ones).
var anyMarkerRegex = new Regex(@"<!--\s*/?\s*covref", RegexOptions.Compiled);
var proseRegex = new Regex(@"cov:[A-Za-z][A-Za-z0-9.]*/[A-Za-z0-9./_\-]+\.cs", RegexOptions.Compiled);

// Scalar fields of an index entry (REQ-002) plus one per coverage flag.
var allowedFields = new HashSet<string>(StringComparer.Ordinal)
{
    "package", "path", "coverage", "obligations", "metObligations", "flags", "noData", "lastRun", "dashboardUrl",
    "unit", "guard", "contract", "property", "integration"
};

// ── Files ────────────────────────────────────────────────────────────────────
var mdFiles = new List<string>();
if (Directory.Exists(docsRoot)) mdFiles.AddRange(Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories));
foreach (var root in scanRoots)
{
    if (Directory.Exists(root)) mdFiles.AddRange(Directory.GetFiles(root, "*.md", SearchOption.AllDirectories));
}
mdFiles.AddRange(Directory.GetFiles(".", "*.md", SearchOption.TopDirectoryOnly));
// Exclusions are matched on the path relative to the working directory, so running from a
// checkout that itself lives under an excluded name (e.g. a .claude/worktrees/ folder) still works.
string[] excludedSegments = ["node_modules", "bin", "obj", ".claude"];
mdFiles = mdFiles
    .Select(Path.GetFullPath)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Where(f => !RelPath(f).Split('/').Any(part => excludedSegments.Contains(part, StringComparer.Ordinal)))
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

if (checkDangling)
{
    return RunGate();
}

// ── Render mode ──────────────────────────────────────────────────────────────
if (!File.Exists(docrefIndexPath))
{
    Console.Error.WriteLine($"DocRef index not found: {docrefIndexPath}");
    Console.Error.WriteLine("Run coverage-report.cs first to generate it.");
    return 2;
}

var indexJson = JsonNode.Parse(File.ReadAllText(docrefIndexPath)) as JsonObject ?? new JsonObject();
Console.WriteLine($"Loaded {indexJson.Count} DocRef entries from {docrefIndexPath}");

int filesModified = 0, tablesGenerated = 0, inlinesGenerated = 0, warnings = 0;
var citedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);

foreach (var file in mdFiles)
{
    var relPath = RelPath(file);
    var original = File.ReadAllText(file);
    var output = new StringBuilder();

    foreach (var segment in SplitSegments(original))
    {
        if (segment.InFence)
        {
            output.Append(segment.Text);
            continue;
        }

        // cited-by is built from the original text, before expansion, so line numbers are stable.
        CollectCitations(segment, relPath);

        foreach (var (line, _) in MalformedMarkers(segment))
        {
            warnings++;
            Console.WriteLine($"  ⚠ {relPath}:{line}: malformed or unclosed covref marker left untouched");
        }

        // Matches are found on the masked text (inline code blanked, same length) and applied to the
        // original from the end, so markers quoted in `code` are left alone.
        var masked = MaskInlineCode(segment.Text);
        var blocks = tableBlockRegex.Matches(masked).Cast<Match>()
            .Select(m => (Match: m, IsTable: true))
            .Concat(inlineBlockRegex.Matches(masked).Cast<Match>().Select(m => (Match: m, IsTable: false)))
            .OrderByDescending(b => b.Match.Index);
        var text = new StringBuilder(segment.Text);
        foreach (var (match, isTable) in blocks)
        {
            string replacement;
            if (isTable)
            {
                tablesGenerated++;
                var table = GenerateTable(match.Groups["pattern"].Value, indexJson, file, ref warnings);
                replacement = $"{match.Groups["open"].Value}\n{table}\n{match.Groups["close"].Value}";
            }
            else
            {
                inlinesGenerated++;
                var value = LookupInlineValue(match.Groups["id"].Value, match.Groups["field"].Value, indexJson, ref warnings);
                replacement = $"{match.Groups["open"].Value}{value}{match.Groups["close"].Value}";
            }
            text.Remove(match.Index, match.Length).Insert(match.Index, replacement);
        }
        output.Append(text);
    }

    var content = output.ToString();
    if (content != original)
    {
        filesModified++;
        if (dryRun)
            Console.WriteLine($"  [dry-run] Would update: {relPath}");
        else
        {
            File.WriteAllText(file, content);
            Console.WriteLine($"  Updated: {relPath}");
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
    if (!dryRun)
        File.WriteAllText(citedByPath, citedByJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"cited-by index: {citedBy.Count} DocRef(s) with citations → {citedByPath}");
}

Console.WriteLine();
Console.WriteLine($"Summary: {filesModified} file(s) modified, {tablesGenerated} table(s), {inlinesGenerated} inline(s), {warnings} warning(s)");
if (warnings > 0)
    Console.WriteLine("  ⚠ Some citations could not be rendered. Run with --check-dangling for the exact locations.");

return 0;

// ── Local functions (capture the regexes, the index and the cited-by map) ─────

void CollectCitations(Segment segment, string relPath)
{
    var masked = MaskInlineCode(segment.Text);
    foreach (Match m in tableBlockRegex.Matches(masked))
    {
        var glob = GlobToRegex(m.Groups["pattern"].Value);
        foreach (var (id, _) in indexJson)
        {
            if (glob.IsMatch(id)) AddCitation(citedBy, id, $"{relPath}:{segment.LineOf(m.Index)}");
        }
    }
    foreach (Match m in inlineBlockRegex.Matches(masked))
        AddCitation(citedBy, m.Groups["id"].Value, $"{relPath}:{segment.LineOf(m.Index)}");
    foreach (var (line, id) in ProseMentions(segment))
    {
        if (indexJson.ContainsKey(id)) AddCitation(citedBy, id, $"{relPath}:{line}");
    }
}

// Prose mentions outside marker blocks and inline code spans, with their line numbers.
IEnumerable<(int Line, string Id)> ProseMentions(Segment segment)
{
    var text = MaskInlineCode(segment.Text);
    text = BlankOut(text, tableBlockRegex);
    text = BlankOut(text, inlineBlockRegex);
    foreach (Match m in proseRegex.Matches(text))
        yield return (segment.LineOf(m.Index), m.Value);
}

// Every covref-looking token that is not the opener or closer of a well-formed block.
IEnumerable<(int Line, string Text)> MalformedMarkers(Segment segment)
{
    var masked = MaskInlineCode(segment.Text);
    var accounted = new System.Collections.Generic.HashSet<int>();
    foreach (Match m in tableBlockRegex.Matches(masked))
    {
        accounted.Add(m.Groups["open"].Index);
        accounted.Add(m.Groups["close"].Index);
    }
    foreach (Match m in inlineBlockRegex.Matches(masked))
    {
        accounted.Add(m.Groups["open"].Index);
        accounted.Add(m.Groups["close"].Index);
    }
    foreach (Match m in anyMarkerRegex.Matches(masked))
    {
        if (!accounted.Contains(m.Index))
        {
            var end = segment.Text.IndexOf('\n', m.Index);
            var snippet = segment.Text[m.Index..(end < 0 ? segment.Text.Length : end)].Trim();
            yield return (segment.LineOf(m.Index), snippet);
        }
    }
}

int RunGate()
{
    var validIds = LoadManifestIds(manifestDir, srcRoot);
    var errors = new List<string>();
    int citations = 0;

    foreach (var file in mdFiles)
    {
        var relPath = RelPath(file);
        foreach (var segment in SplitSegments(File.ReadAllText(file)))
        {
            if (segment.InFence) continue;

            foreach (var (line, snippet) in MalformedMarkers(segment))
            {
                citations++;
                errors.Add($"{relPath}:{line}: malformed or unclosed covref marker `{snippet}` (expected `<!-- covref-table: <glob> -->…<!-- /covref-table -->` or `<!-- covref: <id>:<field> -->…<!-- /covref -->`, opener on one line)");
            }

            var masked = MaskInlineCode(segment.Text);
            foreach (Match m in tableBlockRegex.Matches(masked))
            {
                citations++;
                var pattern = m.Groups["pattern"].Value;
                var glob = GlobToRegex(pattern);
                if (!validIds.Any(glob.IsMatch))
                    errors.Add($"{relPath}:{segment.LineOf(m.Index)}: covref-table pattern `{pattern}` matches no coverage DocRef");
            }

            foreach (Match m in inlineBlockRegex.Matches(masked))
            {
                citations++;
                var line = segment.LineOf(m.Index);
                var id = m.Groups["id"].Value;
                var field = m.Groups["field"].Value;
                if (!validIds.Contains(id))
                    errors.Add($"{relPath}:{line}: `{id}` is not a coverage DocRef (no manifest entry with applicable flags, or file missing under {srcRoot})");
                if (!allowedFields.Contains(field))
                    errors.Add($"{relPath}:{line}: `{field}` is not a coverage field (allowed: {string.Join(", ", allowedFields)})");
            }

            foreach (var (line, id) in ProseMentions(segment))
            {
                citations++;
                if (!validIds.Contains(id))
                    errors.Add($"{relPath}:{line}: `{id}` is not a coverage DocRef (no manifest entry with applicable flags, or file missing under {srcRoot})");
            }
        }
    }

    foreach (var err in errors)
        Console.Error.WriteLine(err);
    Console.WriteLine($"Checked {citations} citation(s) in {mdFiles.Count} file(s) against {validIds.Count} coverage DocRef(s): {errors.Count} error(s).");
    return errors.Count > 0 ? 1 : 0;
}

// ── Static helpers ───────────────────────────────────────────────────────────

static HashSet<string> LoadManifestIds(string manifestDir, string srcRoot)
{
    var ids = new HashSet<string>(StringComparer.Ordinal);
    if (!Directory.Exists(manifestDir)) return ids;

    foreach (var jsonFile in Directory.GetFiles(manifestDir, "*.json", SearchOption.TopDirectoryOnly))
    {
        if (Path.GetFileName(jsonFile) == "defaults.json") continue;
        if (JsonNode.Parse(File.ReadAllText(jsonFile)) is not JsonObject obj) continue;
        var package = obj["package"]?.GetValue<string>();
        if (package is null || obj["files"] is not JsonObject files) continue;

        foreach (var (key, node) in files)
        {
            if (node is not JsonObject entry) continue;
            var tests = (entry["override"] as JsonArray) ?? (entry["defaultTests"] as JsonArray);
            if (tests is null || !tests.Any(t => t?.GetValue<string>() is "unit" or "guard" or "contract" or "property" or "integration"))
                continue;

            var relPath = key.Replace('\\', '/');
            if (File.Exists(Path.Combine(srcRoot, package, relPath)))
                ids.Add($"cov:{package}/{relPath}");
        }
    }

    return ids;
}

// Splits a document into alternating fenced / non-fenced segments, keeping every character.
static List<Segment> SplitSegments(string content)
{
    var segments = new List<Segment>();
    var lines = content.Split('\n');
    var buffer = new StringBuilder();
    var bufferStart = 1;
    var inFence = false;
    char fenceChar = '\0';
    int fenceLength = 0;

    void Flush(int nextLine)
    {
        if (buffer.Length > 0) segments.Add(new Segment(inFence, bufferStart, buffer.ToString()));
        buffer.Clear();
        bufferStart = nextLine;
    }

    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        var lineText = i < lines.Length - 1 ? line + "\n" : line;
        var trimmed = line.TrimStart().TrimEnd('\r');
        var run = FenceRun(trimmed);

        if (!inFence && run.Length >= 3)
        {
            Flush(i + 1);
            inFence = true;
            fenceChar = run.Char;
            fenceLength = run.Length;
            buffer.Append(lineText);
            continue;
        }

        if (inFence && run.Length >= fenceLength && run.Char == fenceChar && trimmed[run.Length..].Trim().Length == 0)
        {
            buffer.Append(lineText);
            Flush(i + 2);
            inFence = false;
            continue;
        }

        buffer.Append(lineText);
    }

    Flush(lines.Length + 1);
    return segments;
}

static (char Char, int Length) FenceRun(string trimmed)
{
    if (trimmed.Length < 3 || (trimmed[0] != '`' && trimmed[0] != '~')) return ('\0', 0);
    var c = trimmed[0];
    var n = 0;
    while (n < trimmed.Length && trimmed[n] == c) n++;
    return n >= 3 ? (c, n) : ('\0', 0);
}

// Blanks inline code spans (same length, newlines kept): quoted markers and IDs are literal text.
static string MaskInlineCode(string text) =>
    Regex.Replace(text, "`[^`\n]*`", m => new string(' ', m.Length));

// Replaces every match with spaces (newlines kept) so indexes and line numbers stay valid.
static string BlankOut(string text, Regex regex) =>
    regex.Replace(text, m => Regex.Replace(m.Value, "[^\n]", " "));

static string RelPath(string file) => Path.GetRelativePath(".", file).Replace('\\', '/');

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
    sb.AppendLine("| File | Flags | Obligations | Coverage | Per flag | Last run |");
    sb.AppendLine("|------|-------|------------:|---------:|----------|----------|");

    foreach (var (id, entry) in matches)
    {
        var path = entry["path"]?.GetValue<string>() ?? id;
        var lastRun = entry["lastRun"]?.GetValue<string>() ?? "—";
        var lastRunShort = lastRun.Length >= 10 ? lastRun[..10] : lastRun;
        var dashUrl = entry["dashboardUrl"]?.GetValue<string>() ?? "";
        var anchorId = SanitizeAnchorId(id);
        var flags = Flags(entry);
        var perFlag = string.Join("; ", flags.Select(f => $"{f} {FlagValue(entry, f)}"));

        var fileCell = string.IsNullOrEmpty(dashUrl)
            ? $"<a id=\"{anchorId}\"></a>`{path}`"
            : $"<a id=\"{anchorId}\"></a>[`{path}`]({dashUrl})";

        sb.AppendLine($"| {fileCell} | {string.Join(", ", flags)} | {(int)GetD(entry, "metObligations")}/{(int)GetD(entry, "obligations")} | {CoverageValue(entry)} | {perFlag} | {lastRunShort} |");
    }

    var methodologyRel = ComputeRelativePath(sourceFile, "docs/testing/coverage-measurement-methodology.md");
    sb.AppendLine();
    sb.AppendLine($"*{matches.Count} file(s) matched `{pattern}`. Data from [coverage dashboard](https://dlrivada.github.io/Encina/coverage/). See [coverage-measurement-methodology.md]({methodologyRel}).*");
    return sb.ToString();
}

static string LookupInlineValue(string id, string field, JsonObject index, ref int warnings)
{
    if (!index.TryGetPropertyValue(id, out var node) || node is not JsonObject entry)
    {
        warnings++;
        return $"⚠ `{id}` not found";
    }

    switch (field)
    {
        case "coverage":
            return CoverageValue(entry);
        case "obligations":
            return $"{(int)GetD(entry, "metObligations")}/{(int)GetD(entry, "obligations")}";
        case "metObligations":
            return ((int)GetD(entry, "metObligations")).ToString(CultureInfo.InvariantCulture);
        case "flags":
            return string.Join(", ", Flags(entry));
        case "noData":
            return GetB(entry, "noData") ? "true" : "false";
        case "package" or "path" or "lastRun" or "dashboardUrl":
            return entry[field]?.GetValue<string>() ?? "—";
    }

    if ((entry["perFlag"] as JsonObject)?[field] is JsonObject)
        return FlagValue(entry, field);

    warnings++;
    return $"⚠ field `{field}` not found in `{id}`";
}

static string CoverageValue(JsonObject entry)
{
    if (GetB(entry, "noData") || entry["coverage"] is null) return "no data";
    return $"{GetD(entry, "coverage"):F2}%";
}

static string FlagValue(JsonObject entry, string flag)
{
    if ((entry["perFlag"] as JsonObject)?[flag] is not JsonObject f || GetB(f, "noData")) return "no data";
    var value = $"{(int)GetD(f, "covered")}/{(int)GetD(f, "total")}";
    var target = GetNullableD(f, "target");
    return target is null ? value : $"{value} (target {target.Value:0.##}%)";
}

static List<string> Flags(JsonObject entry) =>
    (entry["flags"] as JsonArray)?.Select(f => f?.GetValue<string>()).OfType<string>().ToList() ?? [];

static string ComputeRelativePath(string sourceFile, string targetPath)
{
    var sourceDir = Path.GetDirectoryName(Path.GetFullPath(sourceFile)) ?? ".";
    return Path.GetRelativePath(sourceDir, Path.GetFullPath(targetPath)).Replace('\\', '/');
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

static double? GetNullableD(JsonObject obj, string key)
{
    var v = obj[key];
    if (v is null) return null;
    try { return v.GetValue<double>(); } catch { return null; }
}

static bool GetB(JsonObject obj, string key)
{
    var v = obj[key];
    if (v is null) return false;
    try { return v.GetValue<bool>(); } catch { return false; }
}

static string SanitizeAnchorId(string docRef) =>
    "covref-" + docRef.Replace(':', '-').Replace('/', '-').Replace('.', '-');

static void AddCitation(Dictionary<string, List<string>> citedBy, string docRef, string location)
{
    if (!citedBy.TryGetValue(docRef, out var locs))
    {
        locs = [];
        citedBy[docRef] = locs;
    }
    locs.Add(location);
}

/// <summary>A run of document text that is either inside or outside a fenced code block.</summary>
sealed record Segment(bool InFence, int StartLine, string Text)
{
    /// <summary>1-based line number, in the whole document, of the character at <paramref name="index"/>.</summary>
    public int LineOf(int index)
    {
        var line = StartLine;
        for (int i = 0; i < index && i < Text.Length; i++)
        {
            if (Text[i] == '\n') line++;
        }
        return line;
    }
}
