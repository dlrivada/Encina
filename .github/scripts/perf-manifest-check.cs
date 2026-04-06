// perf-manifest-check.cs — Validate DocRef consistency between benchmarks and documentation.
//
// Usage:
//   dotnet run .github/scripts/perf-manifest-check.cs -- \
//     --manifest-dir .github/perf-manifest \
//     --docs-root docs \
//     --src-root tests/Encina.BenchmarkTests \
//     --load-tests-root tests/Encina.LoadTests tests/Encina.NBomber
//
// Checks performed:
//   1. DANGLING CITATIONS: DocRef markers in docs/**/*.md that reference a DocRef ID
//      not found in any benchmark manifest or load-test source → ERROR
//   2. ORPHAN DOCREFS: DocRef annotations in benchmark/load-test code that are never
//      cited in any docs/**/*.md → WARNING (benchmarks can exist without citation)
//   3. MANIFEST DRIFT: Benchmark classes/methods listed in manifests that no longer
//      exist in source code → ERROR
//   4. SOURCE DRIFT: Benchmark classes/methods in source code that are not listed in
//      the manifest → WARNING (run generate-perf-manifest.cs to fix)
//
// Exit codes:
//   0 = all checks passed (warnings are OK)
//   1 = at least one ERROR detected
//
// See ADR-025 §4 and performance-measurement-methodology.md.
#pragma warning disable CA1305

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var manifestDir = ".github/perf-manifest";
var docsRoot = "docs";
var srcRoots = new List<string> { "tests/Encina.BenchmarkTests" };
var loadTestRoots = new List<string> { "tests/Encina.LoadTests", "tests/Encina.NBomber" };
var strict = false; // --strict makes warnings into errors

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--manifest-dir" && i + 1 < args.Length) manifestDir = args[++i];
    if (args[i] == "--docs-root" && i + 1 < args.Length) docsRoot = args[++i];
    if (args[i] == "--src-root" && i + 1 < args.Length) { srcRoots.Clear(); srcRoots.Add(args[++i]); }
    if (args[i] == "--load-tests-root" && i + 1 < args.Length) loadTestRoots.Add(args[++i]);
    if (args[i] == "--strict") strict = true;
}

int errors = 0, warnings = 0;

// ──────────────────────────────────────────────────────────────────────────
// 1. Collect all known DocRef IDs from manifests + source code
// ──────────────────────────────────────────────────────────────────────────

// From benchmark manifests (bench:*)
var manifestDocRefs = new HashSet<string>(StringComparer.Ordinal);
var manifestMethods = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase); // project → methods

if (Directory.Exists(manifestDir))
{
    foreach (var file in Directory.GetFiles(manifestDir, "*.json"))
    {
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(file));
            var project = json?["project"]?.GetValue<string>() ?? "";
            var methods = new HashSet<string>(StringComparer.Ordinal);
            var classes = json?["classes"] as JsonArray;
            if (classes is null) continue;

            foreach (var cls in classes)
            {
                var className = cls?["name"]?.GetValue<string>() ?? "";
                var clsMethods = cls?["methods"] as JsonArray;
                if (clsMethods is null) continue;
                foreach (var m in clsMethods)
                {
                    var methodName = m?["name"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(methodName))
                        methods.Add($"{className}.{methodName}");

                    var docRef = m?["docRef"]?.GetValue<string?>();
                    if (!string.IsNullOrEmpty(docRef))
                        manifestDocRefs.Add(docRef!);
                }
            }
            if (methods.Count > 0)
                manifestMethods[project] = methods;
        }
        catch { /* skip malformed */ }
    }
}

// From load-test source (load:*)
var loadDocRefs = new HashSet<string>(StringComparer.Ordinal);
var docRefConstRegex = new Regex(
    @"public\s+const\s+string\s+DocRef\s*=\s*""([^""]+)""",
    RegexOptions.Compiled);

foreach (var root in loadTestRoots)
{
    if (!Directory.Exists(root)) continue;
    foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
    {
        if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
        if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
        try
        {
            var content = File.ReadAllText(file);
            foreach (Match m in docRefConstRegex.Matches(content))
                loadDocRefs.Add(m.Groups[1].Value);
        }
        catch { /* skip */ }
    }
}

var allKnownDocRefs = new HashSet<string>(manifestDocRefs, StringComparer.Ordinal);
foreach (var dr in loadDocRefs) allKnownDocRefs.Add(dr);

Console.WriteLine($"Known DocRefs: {allKnownDocRefs.Count} ({manifestDocRefs.Count} bench + {loadDocRefs.Count} load)");

// ──────────────────────────────────────────────────────────────────────────
// 2. Scan docs for DocRef citations
// ──────────────────────────────────────────────────────────────────────────

var citedDocRefs = new HashSet<string>(StringComparer.Ordinal);
var citationLocations = new Dictionary<string, List<string>>(StringComparer.Ordinal); // docRef → [file:line, ...]

// Marker patterns in docs
var tableMarkerRegex = new Regex(@"<!-- docref-table:\s*(?<pattern>[^\s]+)\s*-->", RegexOptions.Compiled);
// Inline markers: <!-- docref: bench:area/name:field -->
// The DocRef ID has format "bench:area/name" (contains a colon), so we match
// the prefix explicitly and capture until the LAST colon-delimited field.
var inlineMarkerRegex = new Regex(@"<!-- docref:\s*(?<id>(?:bench|load):[a-zA-Z0-9\-/]+):", RegexOptions.Compiled);
// Also search for DocRef IDs mentioned in prose (including backtick-quoted in Markdown).
// Require full area/name format with at least one slash to avoid false positives.
var proseDocRefRegex = new Regex(@"(?:bench|load):[a-z][a-z0-9\-]*/[a-z][a-z0-9\-]*(?:[a-z0-9\-])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

if (Directory.Exists(docsRoot))
{
    foreach (var file in Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories))
    {
        try
        {
            var lines = File.ReadAllLines(file);
            var relPath = Path.GetRelativePath(docsRoot, file).Replace('\\', '/');

            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                var line = lines[lineNum];

                // Table markers: <!-- docref-table: bench:mediator/* -->
                foreach (Match m in tableMarkerRegex.Matches(line))
                {
                    var pattern = m.Groups["pattern"].Value;
                    // Expand glob to find matching known DocRefs
                    var globRegex = GlobToRegex(pattern);
                    var matched = allKnownDocRefs.Where(dr => globRegex.IsMatch(dr)).ToList();
                    foreach (var dr in matched)
                        RecordCitation(dr, $"{relPath}:{lineNum + 1}");
                    if (matched.Count == 0)
                    {
                        // Pattern doesn't match any known DocRef — potential dangling
                        RecordCitation($"PATTERN:{pattern}", $"{relPath}:{lineNum + 1}");
                    }
                }

                // Inline markers: <!-- docref: bench:mediator/send-command:medianNs -->
                foreach (Match m in inlineMarkerRegex.Matches(line))
                {
                    var id = m.Groups["id"].Value;
                    RecordCitation(id, $"{relPath}:{lineNum + 1}");
                }

                // Prose references: `bench:mediator/send-command` or load:database/uow
                foreach (Match m in proseDocRefRegex.Matches(line))
                {
                    var id = m.Value;
                    // Skip if inside a marker (already caught above)
                    if (line.Contains("docref-table:") || line.Contains("docref:")) continue;
                    RecordCitation(id, $"{relPath}:{lineNum + 1}");
                }
            }
        }
        catch { /* skip */ }
    }
}

void RecordCitation(string docRef, string location)
{
    citedDocRefs.Add(docRef);
    if (!citationLocations.TryGetValue(docRef, out var locs))
    {
        locs = new List<string>();
        citationLocations[docRef] = locs;
    }
    locs.Add(location);
}

Console.WriteLine($"Cited DocRefs in docs: {citedDocRefs.Count}");
Console.WriteLine();

// ──────────────────────────────────────────────────────────────────────────
// CHECK 1: Dangling citations (DocRef in docs but not in any manifest/source)
// ──────────────────────────────────────────────────────────────────────────

Console.WriteLine("## Check 1: Dangling citations");
var dangling = citedDocRefs
    .Where(dr => !dr.StartsWith("PATTERN:", StringComparison.Ordinal))
    .Where(dr => !allKnownDocRefs.Contains(dr))
    .OrderBy(dr => dr, StringComparer.Ordinal)
    .ToList();

if (dangling.Count == 0)
{
    Console.WriteLine("  ✅ No dangling citations found.");
}
else
{
    foreach (var dr in dangling)
    {
        var locs = citationLocations.TryGetValue(dr, out var l) ? string.Join(", ", l) : "?";
        Console.WriteLine($"  ❌ ERROR: DocRef '{dr}' cited in docs but has no benchmark/load-test");
        Console.WriteLine($"           Locations: {locs}");
        errors++;
    }
}

// Check for unresolved patterns
var unresolvedPatterns = citedDocRefs
    .Where(dr => dr.StartsWith("PATTERN:", StringComparison.Ordinal))
    .Select(dr => dr["PATTERN:".Length..])
    .ToList();

if (unresolvedPatterns.Count > 0)
{
    foreach (var pat in unresolvedPatterns)
    {
        var locs = citationLocations.TryGetValue($"PATTERN:{pat}", out var l) ? string.Join(", ", l) : "?";
        Console.WriteLine($"  ⚠ WARNING: docref-table pattern '{pat}' matches no known DocRefs");
        Console.WriteLine($"             Locations: {locs}");
        warnings++;
    }
}

Console.WriteLine();

// ──────────────────────────────────────────────────────────────────────────
// CHECK 2: Orphan DocRefs (in benchmark code but never cited in docs)
// ──────────────────────────────────────────────────────────────────────────

Console.WriteLine("## Check 2: Orphan DocRefs (benchmarks not cited in any doc)");
var orphans = allKnownDocRefs
    .Where(dr => !citedDocRefs.Contains(dr))
    .OrderBy(dr => dr, StringComparer.Ordinal)
    .ToList();

if (orphans.Count == 0)
{
    Console.WriteLine("  ✅ All DocRefs are cited in documentation.");
}
else
{
    Console.WriteLine($"  ⚠ {orphans.Count} DocRef(s) exist in code but are not cited in any docs/**/*.md:");
    foreach (var dr in orphans)
    {
        Console.WriteLine($"     {dr}");
        if (strict) errors++; else warnings++;
    }
}

Console.WriteLine();

// ──────────────────────────────────────────────────────────────────────────
// CHECK 3: Manifest drift (methods in manifest but not in source)
// ──────────────────────────────────────────────────────────────────────────

Console.WriteLine("## Check 3: Manifest → source drift");
var benchmarkAttrRegex = new Regex(@"\[Benchmark(?:\([^)]*\))?\]", RegexOptions.Compiled);
var methodDeclRegex = new Regex(@"public\s+(?:async\s+)?(?:\w+(?:<[^>]+>)?\s+)+(\w+)\s*\(", RegexOptions.Compiled);
var classDeclRegex = new Regex(@"(?:public|internal)\s+(?:sealed\s+|partial\s+)?class\s+(\w+)", RegexOptions.Compiled);
int manifestDriftErrors = 0;

foreach (var (project, expectedMethods) in manifestMethods)
{
    // Find the project directory
    string? projDir = null;
    foreach (var root in srcRoots)
    {
        var candidate = Path.Combine(root, project);
        if (Directory.Exists(candidate)) { projDir = candidate; break; }
    }
    if (projDir is null) continue;

    // Scan source for actual methods
    var actualMethods = new HashSet<string>(StringComparer.Ordinal);
    foreach (var file in Directory.GetFiles(projDir, "*.cs", SearchOption.AllDirectories))
    {
        if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
        if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
        if (Path.GetFileName(file).Equals("Program.cs", StringComparison.OrdinalIgnoreCase)) continue;
        if (Path.GetFileName(file).Equals("GlobalSuppressions.cs", StringComparison.OrdinalIgnoreCase)) continue;

        try
        {
            var content = File.ReadAllText(file);
            if (!benchmarkAttrRegex.IsMatch(content)) continue;

            var classMatch = classDeclRegex.Match(content);
            if (!classMatch.Success) continue;
            var className = classMatch.Groups[1].Value;

            var lines = content.Split('\n');
            var expectingMethod = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (benchmarkAttrRegex.IsMatch(lines[i])) { expectingMethod = true; continue; }
                if (expectingMethod)
                {
                    var m = methodDeclRegex.Match(lines[i]);
                    if (m.Success)
                    {
                        actualMethods.Add($"{className}.{m.Groups[1].Value}");
                        expectingMethod = false;
                    }
                }
            }
        }
        catch { /* skip */ }
    }

    // Compare
    var inManifestNotSource = expectedMethods.Except(actualMethods, StringComparer.Ordinal).ToList();
    var inSourceNotManifest = actualMethods.Except(expectedMethods, StringComparer.Ordinal).ToList();

    if (inManifestNotSource.Count > 0)
    {
        Console.WriteLine($"  ❌ {project}: {inManifestNotSource.Count} method(s) in manifest but NOT in source:");
        foreach (var m in inManifestNotSource.Take(10))
            Console.WriteLine($"       {m}");
        if (inManifestNotSource.Count > 10)
            Console.WriteLine($"       ... and {inManifestNotSource.Count - 10} more");
        manifestDriftErrors += inManifestNotSource.Count;
        errors += inManifestNotSource.Count;
    }

    if (inSourceNotManifest.Count > 0)
    {
        Console.WriteLine($"  ⚠ {project}: {inSourceNotManifest.Count} method(s) in source but NOT in manifest (run generate-perf-manifest.cs):");
        foreach (var m in inSourceNotManifest.Take(10))
            Console.WriteLine($"       {m}");
        if (inSourceNotManifest.Count > 10)
            Console.WriteLine($"       ... and {inSourceNotManifest.Count - 10} more");
        warnings += inSourceNotManifest.Count;
    }
}

if (manifestDriftErrors == 0)
    Console.WriteLine("  ✅ All manifest methods exist in source.");

Console.WriteLine();

// ──────────────────────────────────────────────────────────────────────────
// SUMMARY
// ──────────────────────────────────────────────────────────────────────────

Console.WriteLine("## Summary");
Console.WriteLine($"  Known DocRefs:       {allKnownDocRefs.Count} ({manifestDocRefs.Count} bench + {loadDocRefs.Count} load)");
Console.WriteLine($"  Cited in docs:       {citedDocRefs.Count - unresolvedPatterns.Count}");
Console.WriteLine($"  Orphan DocRefs:      {orphans.Count}");
Console.WriteLine($"  Dangling citations:  {dangling.Count}");
Console.WriteLine($"  Manifest drift:      {manifestDriftErrors} error(s)");
Console.WriteLine($"  Errors:              {errors}");
Console.WriteLine($"  Warnings:            {warnings}");

if (errors > 0)
{
    Console.WriteLine();
    Console.WriteLine($"❌ FAILED with {errors} error(s).");
    Environment.Exit(1);
}
else
{
    Console.WriteLine();
    Console.WriteLine("✅ All checks passed.");
}


static Regex GlobToRegex(string glob)
{
    var escaped = Regex.Escape(glob).Replace("\\*", ".*").Replace("\\?", ".");
    return new Regex($"^{escaped}$", RegexOptions.IgnoreCase);
}
