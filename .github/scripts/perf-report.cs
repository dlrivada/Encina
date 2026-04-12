// perf-report.cs — Parse BenchmarkDotNet and NBomber outputs, aggregate into
//                  a single unified snapshot JSON for the dashboards.
//
// Usage:
//   dotnet run .github/scripts/perf-report.cs -- --kind benchmarks \
//     --input artifacts/performance --output docs/benchmarks/data
//   dotnet run .github/scripts/perf-report.cs -- --kind load-tests \
//     --input artifacts/load-metrics --output docs/load-tests/data
//
// Design notes (see ADR-025 + performance-measurement-methodology.md):
// - Reads BenchmarkDotNet `*-report-full.json` files produced by `--exporters json`.
//   These contain Mean, Median, StdDev, CI, Min, Max, Allocated, Gen0/1/2, N.
// - Reads NBomber `*-nbomber-summary.json` files (single consolidated JSON per run).
// - Emits a single `latest.json` matching the shape consumed by the dashboard:
//   { timestamp, runId, sha, metadata, overall, modules: [ { name, benchmarks|scenarios: [...] } ] }
// - Stability flag per benchmark: stable if CoefficientOfVariation (StdDev/Mean) <= 0.10.
//   Unstable benchmarks are still recorded but marked and excluded from headline stats.
// - Confidence interval at 99% using Student's t critical values looked up by N.
//
// This script is intentionally dependency-free: stock System.Text.Json, no NuGet.
#pragma warning disable CA1305 // IFormatProvider not relevant for standalone scripts
#pragma warning disable CA1861 // Constant array arguments — used in small lookup tables

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

const double StabilityCoVThreshold = 0.10;       // N >= 10  → CoV must be <= 10%
const double LowNStrictCoVThreshold = 0.03;     // 3 <= N < 10 → CoV must be <= 3% (stricter bar to compensate)
const int MinimumIterations = 3;                // N < 3 → always unstable (BDN dry runs)

var kind = "benchmarks";
var inputDir = "";
var outputDir = "";
var runId = 0L;
var sha = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "";
var manifestDir = "";
var fingerprintsFile = "";         // Phase 2: current run's fingerprints
var carryForwardFrom = "";         // Phase 2: previous snapshot to carry forward unchanged modules

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--kind" && i + 1 < args.Length) kind = args[++i];
    if (args[i] == "--input" && i + 1 < args.Length) inputDir = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
    if (args[i] == "--run-id" && i + 1 < args.Length) _ = long.TryParse(args[++i], out runId);
    if (args[i] == "--sha" && i + 1 < args.Length) sha = args[++i];
    if (args[i] == "--manifest-dir" && i + 1 < args.Length) manifestDir = args[++i];
    if (args[i] == "--fingerprints-file" && i + 1 < args.Length) fingerprintsFile = args[++i];
    if (args[i] == "--carry-forward-from" && i + 1 < args.Length) carryForwardFrom = args[++i];
}

if (string.IsNullOrEmpty(inputDir)) inputDir = kind == "benchmarks" ? "artifacts/performance" : "artifacts/load-metrics";
if (string.IsNullOrEmpty(outputDir)) outputDir = $"docs/{kind}/data";
if (string.IsNullOrEmpty(manifestDir)) manifestDir = ".github/perf-manifest";

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"Input directory '{inputDir}' not found — no fresh benchmark data to process.");
    // Still emit a (possibly carried-forward) snapshot if we have a previous one.
    if (kind == "benchmarks" && !string.IsNullOrEmpty(carryForwardFrom) && File.Exists(carryForwardFrom))
    {
        Directory.CreateDirectory(outputDir);
        var metadata0 = BuildMetadata(runId, sha);
        GenerateBenchmarksReport(inputDir, outputDir, metadata0, runId, sha, manifestDir, fingerprintsFile, carryForwardFrom, noInput: true);
        return;
    }
    WriteEmptySnapshot(kind, outputDir, runId, sha);
    return;
}

Directory.CreateDirectory(outputDir);

var metadata = BuildMetadata(runId, sha);

if (kind == "benchmarks")
    GenerateBenchmarksReport(inputDir, outputDir, metadata, runId, sha, manifestDir, fingerprintsFile, carryForwardFrom, noInput: false);
else if (kind == "load-tests")
    GenerateLoadTestsReport(inputDir, outputDir, metadata, runId, sha, carryForwardFrom);
else
{
    Console.Error.WriteLine($"Unknown --kind '{kind}'. Expected 'benchmarks' or 'load-tests'.");
    Environment.Exit(2);
}

// ──────────────────────────────────────────────────────────────────────────
// BENCHMARKS
// ──────────────────────────────────────────────────────────────────────────
static void GenerateBenchmarksReport(
    string inputDir,
    string outputDir,
    JsonObject metadata,
    long runId,
    string sha,
    string manifestDir,
    string fingerprintsFile,
    string carryForwardFrom,
    bool noInput)
{
    // Load current fingerprints (Phase 2) — keyed by project name (e.g. "Encina.Caching.Benchmarks")
    var fingerprints = LoadFingerprints(fingerprintsFile);

    // Discover BenchmarkDotNet full JSON reports recursively. The `--exporters json` CLI
    // arg in BenchmarkDotNet maps to JsonExporter.FullCompressed which produces files
    // named *-report-full-compressed.json. We also accept *-report-full.json in case a
    // future benchmark uses a different exporter variant. Both formats share the same
    // schema (the "compressed" variant is just unindented).
    var reportFiles = noInput
        ? new List<string>()
        : Directory.GetFiles(inputDir, "*-report-full*.json", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("-report-full-compressed-compressed.json", StringComparison.Ordinal))
            .ToList();

    Console.WriteLine($"Found {reportFiles.Count} BenchmarkDotNet reports in {inputDir}");
    if (reportFiles.Count == 0)
    {
        Console.WriteLine("Directory tree probed for diagnostics:");
        try
        {
            foreach (var entry in Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories).Take(40))
                Console.WriteLine($"  {entry}");
        }
        catch { /* ignore */ }
    }

    var modulesByName = new SortedDictionary<string, BenchmarkModule>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in reportFiles)
    {
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(file));
            if (json is null) continue;

            var title = json["Title"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(file);
            var hostEnv = json["HostEnvironmentInfo"];

            // The top-level "Title" encodes assembly + date; use parent dir as module name.
            // Layout is typically: artifacts/performance/<ModuleName>/BenchmarkDotNet.Artifacts/results/<file>
            var moduleName = InferModuleNameFromPath(file, inputDir);

            if (!modulesByName.TryGetValue(moduleName, out var module))
            {
                module = new BenchmarkModule { Name = moduleName };
                modulesByName[moduleName] = module;
            }

            var benchmarksArray = json["Benchmarks"] as JsonArray;
            if (benchmarksArray is null) continue;

            foreach (var b in benchmarksArray)
            {
                if (b is null) continue;
                var entry = ParseBenchmarkEntry(b);
                if (entry is not null) module.Benchmarks.Add(entry);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse '{file}': {ex.Message}");
        }
    }

    // Attach current fingerprints to freshly-measured modules.
    foreach (var m in modulesByName.Values)
    {
        if (fingerprints.TryGetValue(m.Name, out var fp)) m.Fingerprint = fp;
        else if (TryMatchProject(fingerprints, m.Name, out var altFp)) m.Fingerprint = altFp;
    }

    // Inject DocRefs from perf-manifest files. BDN does NOT emit [BenchmarkCategory]
    // in JSON output, so we read DocRefs from the committed manifests instead.
    InjectDocRefsFromManifests(modulesByName, manifestDir);

    // Palanca 3 — Apply stabilityOverrides from perf-manifest files. Benchmarks
    // listed here are known to have high CoV by design (e.g. lock-contention
    // benchmarks, sub-nanosecond jitter) and should be excluded from the
    // headline stable/unstable ratio. They remain visible on the dashboard but
    // carry an "expectedUnstable" flag and a reason string.
    ApplyStabilityOverridesFromManifests(modulesByName, manifestDir);

    // Auto-generate DocRefs for methods that don't have an explicit annotation.
    // This ensures every method in the dashboard shows a DocRef instead of "—".
    // Auto-generated DocRefs use the pattern: bench:<module>/<method> (lowercase, _ → -).
    // Explicit DocRefs from manifests take precedence and are never overwritten.
    AutoGenerateDocRefs(modulesByName);

    // Phase 2: carry forward unchanged modules from the previous snapshot.
    // A module is carried forward iff:
    //   - it exists in the previous snapshot
    //   - its name is NOT in modulesByName (we did not measure it this run)
    //   - the previous fingerprint matches the current fingerprint (proves source unchanged)
    // Otherwise it is either a genuine regression/new module, or a fingerprint mismatch
    // which should never happen if the workflow is correct — if it does, we fall back
    // to leaving the module out (the dashboard will show a gap that triggers investigation).
    var carriedForward = new List<JsonObject>();
    int carriedMethodsStable = 0, carriedMethodsUnstable = 0, carriedMethodsExpectedUnstable = 0;
    if (!string.IsNullOrEmpty(carryForwardFrom) && File.Exists(carryForwardFrom))
    {
        try
        {
            var prevSnapshot = JsonNode.Parse(File.ReadAllText(carryForwardFrom));
            var prevModules = prevSnapshot?["modules"] as JsonArray;
            if (prevModules is not null)
            {
                foreach (var pm in prevModules)
                {
                    if (pm is not JsonObject pmObj) continue;
                    var name = pmObj["name"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (modulesByName.ContainsKey(name)) continue; // freshly measured — skip

                    // Require a current fingerprint to carry forward. If we don't know the
                    // current fingerprint for this project (e.g. project was deleted), skip.
                    var currentFp = ResolveCurrentFingerprint(fingerprints, name);
                    if (string.IsNullOrEmpty(currentFp)) continue;

                    var prevFp = pmObj["fingerprint"]?.GetValue<string>() ?? "";
                    if (prevFp != currentFp)
                    {
                        Console.WriteLine($"  [carry-forward] {name}: fingerprint mismatch — skipping (prev={Truncate(prevFp, 8)}, cur={Truncate(currentFp, 8)})");
                        continue;
                    }

                    var carried = (JsonObject)pmObj.DeepClone();
                    carried["fingerprint"] = currentFp;
                    carried["carriedForward"] = true;
                    carriedForward.Add(carried);

                    var bms = carried["benchmarks"] as JsonArray;
                    if (bms is not null)
                    {
                        foreach (var b in bms)
                        {
                            var isExpectedUnstable = b?["expectedUnstable"]?.GetValue<bool>() == true;
                            var isStable = b?["stable"]?.GetValue<bool>() == true;
                            if (isExpectedUnstable) carriedMethodsExpectedUnstable++;
                            else if (isStable) carriedMethodsStable++;
                            else carriedMethodsUnstable++;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load previous snapshot for carry-forward: {ex.Message}");
        }
    }

    Console.WriteLine($"  Fresh modules: {modulesByName.Count}, carried forward: {carriedForward.Count}");

    // Compute overall stats over fresh + carried modules.
    // Palanca 3: methods marked ExpectedUnstable via the manifest stabilityOverrides
    // are counted in totalMethods but excluded from the stable/unstable headline so the
    // dashboard percentage reflects only benchmarks whose stability is actually a signal.
    int totalMethods = 0, stable = 0, unstable = 0, expectedUnstable = 0;
    double sumMeanNs = 0, sumAllocatedBytes = 0;
    int countMean = 0, countAlloc = 0;

    foreach (var m in modulesByName.Values)
    {
        foreach (var b in m.Benchmarks)
        {
            totalMethods++;
            // An override only hides the benchmark from the unstable bucket if the
            // benchmark is actually unstable on this run. If a benchmark is both
            // marked as expected-unstable AND happens to be Stable (e.g., because
            // Palanca 4 amortized its jitter), count it as Stable — overrides should
            // not penalize methods that are genuinely stable on a given measurement.
            if (b.ExpectedUnstable && !b.Stable) expectedUnstable++;
            else if (b.Stable) stable++;
            else unstable++;
            if (b.MeanNs > 0) { sumMeanNs += b.MeanNs; countMean++; }
            if (b.AllocatedBytes > 0) { sumAllocatedBytes += b.AllocatedBytes; countAlloc++; }
        }
    }
    // Include carried-forward contributions
    foreach (var cm in carriedForward)
    {
        var bms = cm["benchmarks"] as JsonArray;
        if (bms is null) continue;
        foreach (var b in bms)
        {
            totalMethods++;
            var bMean = b?["meanNs"]?.GetValue<double>() ?? 0;
            var bAlloc = b?["allocatedBytes"]?.GetValue<double>() ?? 0;
            if (bMean > 0) { sumMeanNs += bMean; countMean++; }
            if (bAlloc > 0) { sumAllocatedBytes += bAlloc; countAlloc++; }
        }
    }
    stable += carriedMethodsStable;
    unstable += carriedMethodsUnstable;
    expectedUnstable += carriedMethodsExpectedUnstable;

    var overall = new JsonObject
    {
        ["totalModules"] = modulesByName.Count + carriedForward.Count,
        ["freshModules"] = modulesByName.Count,
        ["carriedForwardModules"] = carriedForward.Count,
        ["totalMethods"] = totalMethods,
        ["stableMethods"] = stable,
        ["unstableMethods"] = unstable,
        ["expectedUnstableMethods"] = expectedUnstable,
        ["regressionCount"] = 0, // populated in Phase 3 when diffing against previous snapshot
        ["meanLatencyNs"] = countMean > 0 ? Math.Round(sumMeanNs / countMean, 3) : 0,
        ["meanAllocatedBytes"] = countAlloc > 0 ? Math.Round(sumAllocatedBytes / countAlloc, 3) : 0
    };

    var modulesArray = new JsonArray();
    foreach (var m in modulesByName.Values)
        modulesArray.Add(m.ToJson());
    foreach (var cm in carriedForward)
        modulesArray.Add(cm);

    // Phase 3.3 — variance report: per-class CoV for dashboard stability analysis.
    // This is a flat array of { module, class, cov, stable, n } entries,
    // sorted by CoV descending (most unstable first). The dashboard uses this
    // to render a "most unstable classes" table and to calibrate thresholds.
    var varianceReport = new JsonArray();
    foreach (var m in modulesByName.Values)
    {
        foreach (var b in m.Benchmarks)
        {
            varianceReport.Add(new JsonObject
            {
                ["module"] = m.Name,
                ["class"] = b.Class,
                ["method"] = b.Method,
                ["cov"] = Math.Round(b.Cov, 4),
                ["stable"] = b.Stable,
                ["n"] = b.N,
                ["meanNs"] = Math.Round(b.MeanNs, 1)
            });
        }
    }

    var snapshot = new JsonObject
    {
        ["kind"] = "benchmarks",
        ["timestamp"] = DateTime.UtcNow.ToString("o"),
        ["runId"] = runId > 0 ? JsonValue.Create(runId) : null,
        ["sha"] = string.IsNullOrEmpty(sha) ? null : JsonValue.Create(sha),
        ["metadata"] = metadata,
        ["overall"] = overall,
        ["modules"] = modulesArray,
        ["varianceReport"] = varianceReport
    };

    var latestPath = Path.Combine(outputDir, "latest.json");
    File.WriteAllText(latestPath, snapshot.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    // Archive snapshot with timestamp
    var stamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ");
    var archivePath = Path.Combine(outputDir, $"{stamp}.json");
    File.Copy(latestPath, archivePath, overwrite: true);

    // Build docref-index.json (reverse: DocRef → { dashboard anchor, latest value, stability })
    // Covers both freshly-measured and carried-forward benchmarks so the index remains
    // complete across incremental runs.
    var docrefIndex = new JsonObject();
    foreach (var m in modulesByName.Values)
    {
        foreach (var b in m.Benchmarks)
        {
            if (!string.IsNullOrEmpty(b.DocRef))
            {
                // The anchor matches the HTML id generated by perf-docs-render.cs:
                // "docref-bench-mediator-send-command" for DocRef "bench:mediator/send-command"
                var docAnchor = "docref-" + b.DocRef!.Replace(':', '-').Replace('/', '-');
                docrefIndex[b.DocRef!] = new JsonObject
                {
                    ["module"] = m.Name,
                    ["method"] = b.Method,
                    ["meanNs"] = b.MeanNs,
                    ["medianNs"] = b.MedianNs,
                    ["stdDevNs"] = b.StdDevNs,
                    ["allocatedBytes"] = b.AllocatedBytes,
                    ["stable"] = b.Stable,
                    ["carriedForward"] = false,
                    ["anchor"] = docAnchor
                };
            }
        }
    }
    foreach (var cm in carriedForward)
    {
        var modName = cm["name"]?.GetValue<string>() ?? "";
        var bms = cm["benchmarks"] as JsonArray;
        if (bms is null) continue;
        foreach (var b in bms)
        {
            if (b is not JsonObject bo) continue;
            var docRef = bo["docRef"]?.GetValue<string?>();
            if (string.IsNullOrEmpty(docRef)) continue;
            var entry = (JsonObject)bo.DeepClone();
            entry["module"] = modName;
            entry["carriedForward"] = true;
            var cfAnchor = "docref-" + docRef!.Replace(':', '-').Replace('/', '-');
            entry["anchor"] = cfAnchor;
            docrefIndex[docRef!] = entry;
        }
    }
    File.WriteAllText(Path.Combine(outputDir, "docref-index.json"),
        docrefIndex.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    // Persist the full current fingerprint file alongside the snapshot so downstream
    // consumers (publish-benchmarks.yml, dashboard) can access it without another step.
    if (fingerprints.Count > 0)
    {
        var fpJson = new JsonObject();
        foreach (var (key, value) in fingerprints) fpJson[key] = value;
        File.WriteAllText(Path.Combine(outputDir, "fingerprints.json"),
            fpJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    Console.WriteLine($"Benchmarks snapshot written: {latestPath}");
    Console.WriteLine($"  Fresh: {modulesByName.Count}, Carried: {carriedForward.Count}, Total methods: {totalMethods} (stable: {stable}, unstable: {unstable})");
    _ = manifestDir; // manifest validation is deferred to perf-manifest-check.cs
}

// Phase 2 helper: load a fingerprints.json file into a case-insensitive dictionary.
static SortedDictionary<string, string> LoadFingerprints(string path)
{
    var result = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;
    try
    {
        var json = JsonNode.Parse(File.ReadAllText(path));
        if (json is JsonObject obj)
        {
            foreach (var (key, value) in obj)
            {
                if (value is JsonValue v) result[key] = v.GetValue<string>();
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to load fingerprints from '{path}': {ex.Message}");
    }
    return result;
}

// Resolve a module name (e.g. "Refit") to a fingerprint entry keyed by project name
// (e.g. "Encina.Refit.Benchmarks"). The matrix in benchmarks.yml uses short names as
// module identifiers, while the fingerprint file uses full project names.
static string ResolveCurrentFingerprint(SortedDictionary<string, string> fingerprints, string moduleName)
{
    if (fingerprints.TryGetValue(moduleName, out var direct)) return direct;
    TryMatchProject(fingerprints, moduleName, out var matched);
    return matched;
}

static bool TryMatchProject(SortedDictionary<string, string> fingerprints, string moduleName, out string fingerprint)
{
    // Module → project mapping. The general rule strips the "Encina." prefix and
    // ".Benchmarks" suffix from the full project name and collapses dots:
    //   "Encina.Refit.Benchmarks"              → "Refit"
    //   "Encina.Security.Encryption.Benchmarks" → "SecurityEncryption"
    //   "Encina.Audit.Marten.Benchmarks"        → "AuditMarten"
    //
    // Two project names break that rule and need an explicit mapping:
    //   "Core"   → "Encina.Benchmarks"                         (no area suffix)
    //   "EFCore" → "Encina.EntityFrameworkCore.Benchmarks"     (abbreviation)
    var explicitMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Core"] = "Encina.Benchmarks",
        ["EFCore"] = "Encina.EntityFrameworkCore.Benchmarks"
    };
    if (explicitMap.TryGetValue(moduleName, out var explicitProject)
        && fingerprints.TryGetValue(explicitProject, out var explicitFp))
    {
        fingerprint = explicitFp;
        return true;
    }

    foreach (var (key, value) in fingerprints)
    {
        // Strip "Encina." prefix and ".Benchmarks" suffix, then collapse dots.
        var normalized = key;
        if (normalized.StartsWith("Encina.", StringComparison.Ordinal))
            normalized = normalized["Encina.".Length..];
        if (normalized.EndsWith(".Benchmarks", StringComparison.Ordinal))
            normalized = normalized[..^".Benchmarks".Length];
        normalized = normalized.Replace(".", "");

        if (string.Equals(normalized, moduleName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, moduleName.Replace(".", ""), StringComparison.OrdinalIgnoreCase))
        {
            fingerprint = value;
            return true;
        }
    }
    fingerprint = "";
    return false;
}

static string Truncate(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s[..n]);

static double ParseDbl(string[] cols, int idx) =>
    idx >= 0 && idx < cols.Length && double.TryParse(cols[idx], System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

static long ParseLong(string[] cols, int idx) =>
    idx >= 0 && idx < cols.Length && long.TryParse(cols[idx], out var v) ? v : 0;

/// <summary>
/// Auto-generates DocRef IDs for benchmark methods that don't have an explicit
/// annotation in the manifest. Uses the pattern bench:<module>/<method> with
/// the method name lowercased and underscores replaced by dashes.
/// This ensures the dashboard DocRef column is never empty.
/// </summary>
static void AutoGenerateDocRefs(SortedDictionary<string, BenchmarkModule> modulesByName)
{
    int generated = 0;
    foreach (var module in modulesByName.Values)
    {
        var moduleLower = module.Name.ToLowerInvariant();
        foreach (var b in module.Benchmarks)
        {
            if (!string.IsNullOrEmpty(b.DocRef)) continue;
            // Skip Setup/Cleanup lifecycle methods
            if (b.Method.Contains("Setup", StringComparison.OrdinalIgnoreCase) ||
                b.Method.Contains("Cleanup", StringComparison.OrdinalIgnoreCase))
                continue;

            var methodLower = b.Method.ToLowerInvariant().Replace('_', '-');
            b.DocRef = $"bench:{moduleLower}/{methodLower}";
            generated++;
        }
    }
    if (generated > 0)
        Console.WriteLine($"  DocRefs auto-generated: {generated} (for methods without explicit annotation)");
}

/// <summary>
/// Inject DocRef IDs from perf-manifest JSON files into benchmark entries.
/// BDN does not emit [BenchmarkCategory] in its JSON output, so the only
/// way to associate DocRefs with methods is via the committed manifests
/// which were generated by generate-perf-manifest.cs from source code.
/// </summary>
static void InjectDocRefsFromManifests(
    SortedDictionary<string, BenchmarkModule> modulesByName,
    string manifestDir)
{
    if (string.IsNullOrEmpty(manifestDir) || !Directory.Exists(manifestDir)) return;

    // Build a lookup: "ClassName.MethodName" → DocRef
    var docRefLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in Directory.GetFiles(manifestDir, "*.json"))
    {
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(file));
            var classes = json?["classes"] as JsonArray;
            if (classes is null) continue;

            foreach (var cls in classes)
            {
                var className = cls?["name"]?.GetValue<string>() ?? "";
                var methods = cls?["methods"] as JsonArray;
                if (methods is null) continue;

                foreach (var m in methods)
                {
                    var methodName = m?["name"]?.GetValue<string>() ?? "";
                    var docRef = m?["docRef"]?.GetValue<string?>();
                    if (!string.IsNullOrEmpty(methodName) && !string.IsNullOrEmpty(docRef))
                        docRefLookup[$"{className}.{methodName}"] = docRef!;
                }
            }
        }
        catch { /* skip malformed */ }
    }

    if (docRefLookup.Count == 0) return;

    int injected = 0;
    foreach (var module in modulesByName.Values)
    {
        foreach (var b in module.Benchmarks)
        {
            if (!string.IsNullOrEmpty(b.DocRef)) continue; // already set (from BDN Categories fallback)
            if (docRefLookup.TryGetValue($"{b.Class}.{b.Method}", out var dr))
            {
                b.DocRef = dr;
                injected++;
            }
        }
    }

    Console.WriteLine($"  DocRefs injected from manifests: {injected} (lookup has {docRefLookup.Count} entries)");
}

/// <summary>
/// Apply stabilityOverrides from perf-manifest files to benchmark entries.
/// The manifest format is:
///   "stabilityOverrides": {
///     "ClassName.MethodName": "reason string (why this is expected-unstable)",
///     "ClassName.MethodName(param=value)": "reason string"
///   }
/// Entries matching a key are marked ExpectedUnstable and carry the reason
/// into latest.json. Matching is exact on "Class.Method" and additionally on
/// "Class.Method(Parameters)" for parameterized benchmarks.
/// Palanca 3 — see docs/testing/performance-measurement-methodology.md.
/// </summary>
static void ApplyStabilityOverridesFromManifests(
    SortedDictionary<string, BenchmarkModule> modulesByName,
    string manifestDir)
{
    if (string.IsNullOrEmpty(manifestDir) || !Directory.Exists(manifestDir)) return;

    // Build a lookup: "ClassName.MethodName[(params)]" → reason
    var overrideLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in Directory.GetFiles(manifestDir, "*.json"))
    {
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(file));
            var overrides = json?["stabilityOverrides"] as JsonObject;
            if (overrides is null) continue;

            foreach (var kv in overrides)
            {
                if (kv.Key.StartsWith('$')) continue; // skip $comment etc
                var reason = kv.Value?.GetValue<string>() ?? "expected-unstable";
                overrideLookup[kv.Key] = reason;
            }
        }
        catch { /* skip malformed */ }
    }

    if (overrideLookup.Count == 0) return;

    int applied = 0;
    foreach (var module in modulesByName.Values)
    {
        foreach (var b in module.Benchmarks)
        {
            // Try most specific first: Class.Method(Parameters), then Class.Method.
            var keyWithParams = string.IsNullOrEmpty(b.Parameters)
                ? null
                : $"{b.Class}.{b.Method}({b.Parameters})";
            var keyPlain = $"{b.Class}.{b.Method}";

            if (keyWithParams is not null && overrideLookup.TryGetValue(keyWithParams, out var reasonP))
            {
                b.ExpectedUnstable = true;
                b.ExpectedUnstableReason = reasonP;
                applied++;
            }
            else if (overrideLookup.TryGetValue(keyPlain, out var reason))
            {
                b.ExpectedUnstable = true;
                b.ExpectedUnstableReason = reason;
                applied++;
            }
        }
    }

    Console.WriteLine($"  Stability overrides applied: {applied} (lookup has {overrideLookup.Count} entries)");
}

static BenchmarkEntry? ParseBenchmarkEntry(JsonNode node)
{
    // BenchmarkDotNet full JSON schema:
    // { "DisplayInfo": "Class.Method: ...", "Namespace": "...", "Type": "Class",
    //   "Method": "MethodName", "MethodTitle": "MethodName",
    //   "Parameters": "", "FullName": "...",
    //   "Statistics": {
    //     "N": 15, "Min": 1.23, "Mean": 1.45, "Median": 1.44,
    //     "StandardDeviation": 0.05, "Max": 1.6,
    //     "ConfidenceInterval": { "Level": "L999", "Mean": 1.45, ... }
    //   },
    //   "Memory": { "Gen0Collections": 0, "Gen1Collections": 0, "Gen2Collections": 0,
    //               "TotalOperations": 1000, "BytesAllocatedPerOperation": 40 },
    //   "Params": [{ "Name": "BatchSize", "Value": 10 }],
    //   "Categories": ["DocRef:bench:mediator/send-command", "Hot"]
    // }

    var type = node["Type"]?.GetValue<string>();
    var method = node["Method"]?.GetValue<string>();
    if (string.IsNullOrEmpty(method)) return null;

    var stats = node["Statistics"];
    if (stats is null) return null;

    var mean = GetDouble(stats, "Mean");
    var median = GetDouble(stats, "Median");
    var stdDev = GetDouble(stats, "StandardDeviation");
    var min = GetDouble(stats, "Min");
    var max = GetDouble(stats, "Max");
    var n = (int)GetDouble(stats, "N");

    var entry = new BenchmarkEntry
    {
        Class = type ?? "",
        Method = method,
        FullName = node["FullName"]?.GetValue<string>() ?? $"{type}.{method}",
        Parameters = node["Parameters"]?.GetValue<string>() ?? "",
        MeanNs = mean,
        MedianNs = median,
        StdDevNs = stdDev,
        MinNs = min,
        MaxNs = max,
        N = n
    };

    // Memory
    var memory = node["Memory"];
    if (memory is not null)
    {
        entry.AllocatedBytes = GetDouble(memory, "BytesAllocatedPerOperation");
        entry.Gen0 = (int)GetDouble(memory, "Gen0Collections");
        entry.Gen1 = (int)GetDouble(memory, "Gen1Collections");
        entry.Gen2 = (int)GetDouble(memory, "Gen2Collections");
    }

    // Stability (CoV = StdDev/Mean). Use 0 for degenerate cases (mean=0) instead
    // of Infinity, which crashes JSON serialization.
    entry.Cov = mean > 0 ? stdDev / mean : 0;

    // Two-tier stability rule (Palanca 1):
    //   N <  3          → always Unstable (BDN dry/minimal runs carry no statistical weight).
    //   3 <= N < 10     → Stable only if CoV is very tight (<= 3%). Fast benchmarks where BDN
    //                     auto-shortens to N=4-9 can still be trusted when the observed variance
    //                     is genuinely tiny; the stricter threshold compensates for the low N.
    //   N >= 10         → Standard rule: CoV <= 10%.
    //
    // Rationale: see docs/testing/performance-measurement-methodology.md ("Stability rule").
    // The old rule was a flat `n < 10 → unstable` which rejected hundreds of sub-microsecond
    // benchmarks with CoV < 1% as false positives, capping the dashboard around 82 % stable.
    entry.Stable = mean > 0
        && n >= MinimumIterations
        && entry.Cov <= (n < 10 ? LowNStrictCoVThreshold : StabilityCoVThreshold);

    // 99% confidence interval (approximation using z=2.576 when N is not tiny;
    // for strictly correct values use Student's t distribution per N. The
    // methodology doc describes the tradeoff.)
    var se = n > 0 ? stdDev / Math.Sqrt(n) : 0;
    var tCrit = 2.576; // normal approximation
    entry.CI99LowerNs = mean - tCrit * se;
    entry.CI99UpperNs = mean + tCrit * se;

    // DocRef extraction: BDN does NOT emit [BenchmarkCategory] in JSON output,
    // so we cannot read it from the Categories field. Instead, DocRef is injected
    // later from the perf-manifest files (see InjectDocRefsFromManifests below).
    // The legacy Categories scan is kept as a fallback in case a future BDN
    // version adds this field.
    var categories = node["Categories"] as JsonArray;
    if (categories is not null)
    {
        foreach (var c in categories)
        {
            var val = c?.GetValue<string>();
            if (val is not null && val.StartsWith("DocRef:", StringComparison.Ordinal))
            {
                entry.DocRef = val.Substring("DocRef:".Length);
                break;
            }
        }
    }

    return entry;
}

static string InferModuleNameFromPath(string file, string inputDir)
{
    // artifacts/performance/<ModuleName>/BenchmarkDotNet.Artifacts/results/<Assembly.Class-report-full.json>
    var relative = Path.GetRelativePath(inputDir, file);
    var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
    return segments.Length > 0 ? segments[0] : "Unknown";
}

// ──────────────────────────────────────────────────────────────────────────
// LOAD TESTS
// ──────────────────────────────────────────────────────────────────────────
static void GenerateLoadTestsReport(string inputDir, string outputDir, JsonObject metadata, long runId, string sha, string carryForwardFrom = "")
{
    // NBomber produces nbomber-report.csv with one row per scenario:
    // test_suite,test_name,scenario,duration,step_name,request_count,ok,failed,ok_rps,
    //   ok_min,ok_mean,ok_max,ok_50_percent,ok_75_percent,ok_95_percent,ok_99_percent,ok_std_dev,...
    // The nbomber-summary.json in this project is a CUSTOM simplified format from
    // summarize-nbomber-run.cs, NOT the standard NBomber JSON schema. We parse the CSV
    // which has the complete per-scenario data.
    var csvFiles = Directory.Exists(inputDir)
        ? Directory.GetFiles(inputDir, "nbomber-report.csv", SearchOption.AllDirectories).ToList()
        : new List<string>();

    // Additionally, console harness writes metrics-<stamp>.csv
    var metricsFiles = Directory.Exists(inputDir)
        ? Directory.GetFiles(inputDir, "metrics-*.csv", SearchOption.AllDirectories).ToList()
        : new List<string>();

    Console.WriteLine($"Found {csvFiles.Count} NBomber report CSVs and {metricsFiles.Count} harness metrics CSVs in {inputDir}");

    var scenarios = new List<LoadScenario>();

    foreach (var file in csvFiles)
    {
        try
        {
            var lines = File.ReadAllLines(file);
            if (lines.Length < 2) continue; // header + at least 1 data row
            // Parse CSV header to find column indices
            var headers = lines[0].Split(',');
            int iScenario = Array.IndexOf(headers, "scenario");
            int iRequestCount = Array.IndexOf(headers, "request_count");
            int iOk = Array.IndexOf(headers, "ok");
            int iFailed = Array.IndexOf(headers, "failed");
            int iOkRps = Array.IndexOf(headers, "ok_rps");
            int iOkMean = Array.IndexOf(headers, "ok_mean");
            int iOk50 = Array.IndexOf(headers, "ok_50_percent");
            int iOk95 = Array.IndexOf(headers, "ok_95_percent");
            int iOk99 = Array.IndexOf(headers, "ok_99_percent");
            int iOkStdDev = Array.IndexOf(headers, "ok_std_dev");
            int iOkMin = Array.IndexOf(headers, "ok_min");
            int iOkMax = Array.IndexOf(headers, "ok_max");

            int iTestName = Array.IndexOf(headers, "test_name");
            if (iScenario < 0 && iTestName < 0) continue;

            // Infer the provider/category from the artifact directory path.
            // e.g. ".../broker-load-metrics-kafka/..." → "kafka"
            //      ".../database-load-metrics-efcore-sqlserver/..." → "efcore-sqlserver"
            //      ".../load-metrics/..." → "harness"
            var provider = InferProviderFromPath(file, inputDir);

            for (int row = 1; row < lines.Length; row++)
            {
                var cols = lines[row].Split(',');
                if (cols.Length < 3) continue;

                // Build a meaningful scenario name:
                // test_name (e.g. "db-uow") + provider (e.g. "efcore-sqlserver")
                // → "db-uow (efcore-sqlserver)"
                var testName = iTestName >= 0 && iTestName < cols.Length ? cols[iTestName] : "";
                var rawScenario = iScenario >= 0 && iScenario < cols.Length ? cols[iScenario] : "";
                var scenarioName = !string.IsNullOrEmpty(testName) && testName != rawScenario
                    ? (string.IsNullOrEmpty(provider) || provider == "harness"
                        ? testName
                        : $"{testName} ({provider})")
                    : (!string.IsNullOrEmpty(provider) && provider != "harness"
                        ? $"{rawScenario} ({provider})"
                        : rawScenario);

                var sc = new LoadScenario
                {
                    Name = scenarioName,
                    RequestCount = ParseLong(cols, iRequestCount),
                    Rps = ParseDbl(cols, iOkRps),
                    MeanMs = ParseDbl(cols, iOkMean),
                    P50Ms = ParseDbl(cols, iOk50),
                    P95Ms = ParseDbl(cols, iOk95),
                    P99Ms = ParseDbl(cols, iOk99),
                    StdDevMs = ParseDbl(cols, iOkStdDev),
                    MinMs = ParseDbl(cols, iOkMin),
                    MaxMs = ParseDbl(cols, iOkMax),
                    FailCount = ParseLong(cols, iFailed)
                };
                var totalOps = sc.RequestCount + sc.FailCount;
                sc.ErrorRate = totalOps > 0 ? (double)sc.FailCount / totalOps : 0;
                scenarios.Add(sc);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse NBomber CSV '{file}': {ex.Message}");
        }
    }

    // Group by area (prefix of scenario name before first '-' or '/')
    var modulesByArea = new SortedDictionary<string, List<LoadScenario>>(StringComparer.OrdinalIgnoreCase);
    foreach (var sc in scenarios)
    {
        var area = ExtractArea(sc.Name);
        if (!modulesByArea.TryGetValue(area, out var list))
        {
            list = new List<LoadScenario>();
            modulesByArea[area] = list;
        }
        list.Add(sc);
    }

    int totalScenarios = scenarios.Count;
    int passing = scenarios.Count(s => s.ErrorRate < 0.01);
    int failing = totalScenarios - passing;

    double sumRps = 0, sumP95 = 0, sumErr = 0;
    int countRps = 0, countP95 = 0, countErr = 0;
    foreach (var s in scenarios)
    {
        if (s.Rps > 0) { sumRps += s.Rps; countRps++; }
        if (s.P95Ms > 0) { sumP95 += s.P95Ms; countP95++; }
        sumErr += s.ErrorRate;
        countErr++;
    }

    // overall is computed AFTER carry-forward below, so it includes carried modules.
    var modulesArray = new JsonArray();
    foreach (var (area, list) in modulesByArea)
    {
        var scArray = new JsonArray();
        foreach (var sc in list) scArray.Add(sc.ToJson());
        modulesArray.Add(new JsonObject { ["name"] = area, ["carriedForward"] = false, ["scenarios"] = scArray });
    }

    // Phase 2.2: Carry forward modules from previous snapshot that are NOT present
    // in this run. This handles partial workflow_dispatch runs where only some
    // categories are enabled (e.g., only database tests, not caching/messaging).
    int carriedForwardCount = 0;
    if (!string.IsNullOrEmpty(carryForwardFrom) && File.Exists(carryForwardFrom))
    {
        try
        {
            var prevSnapshot = JsonNode.Parse(File.ReadAllText(carryForwardFrom));
            var prevModules = prevSnapshot?["modules"] as JsonArray;
            if (prevModules is not null)
            {
                foreach (var pm in prevModules)
                {
                    if (pm is not JsonObject pmObj) continue;
                    var name = pmObj["name"]?.GetValue<string>();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (modulesByArea.ContainsKey(name)) continue; // freshly measured

                    var carried = (JsonObject)pmObj.DeepClone();
                    carried["carriedForward"] = true;
                    modulesArray.Add(carried);
                    carriedForwardCount++;

                    // Include carried-forward scenarios in overall stats
                    var carriedScenarios = carried["scenarios"] as JsonArray;
                    if (carriedScenarios is not null)
                    {
                        foreach (var cs in carriedScenarios)
                        {
                            totalScenarios++;
                            var csErr = cs?["errorRate"]?.GetValue<double>() ?? 0;
                            if (csErr < 0.01) passing++; else failing++;
                            var csRps = cs?["rps"]?.GetValue<double>() ?? 0;
                            var csP95 = cs?["p95Ms"]?.GetValue<double>() ?? 0;
                            if (csRps > 0) { sumRps += csRps; countRps++; }
                            if (csP95 > 0) { sumP95 += csP95; countP95++; }
                            sumErr += csErr; countErr++;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load previous snapshot for carry-forward: {ex.Message}");
        }
    }

    // Recompute overall with carried-forward contributions
    var overall = new JsonObject
    {
        ["totalScenarios"] = totalScenarios,
        ["freshScenarios"] = scenarios.Count,
        ["carriedForwardModules"] = carriedForwardCount,
        ["passingScenarios"] = passing,
        ["failingScenarios"] = failing,
        ["meanThroughputOps"] = countRps > 0 ? Math.Round(sumRps / countRps, 2) : 0,
        ["meanP95Ms"] = countP95 > 0 ? Math.Round(sumP95 / countP95, 3) : 0,
        ["meanErrorRate"] = countErr > 0 ? Math.Round(sumErr / countErr, 5) : 0
    };

    var snapshot = new JsonObject
    {
        ["kind"] = "load-tests",
        ["timestamp"] = DateTime.UtcNow.ToString("o"),
        ["runId"] = runId > 0 ? JsonValue.Create(runId) : null,
        ["sha"] = string.IsNullOrEmpty(sha) ? null : JsonValue.Create(sha),
        ["metadata"] = metadata,
        ["overall"] = overall,
        ["modules"] = modulesArray
    };

    var latestPath = Path.Combine(outputDir, "latest.json");
    File.WriteAllText(latestPath, snapshot.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    var stamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ");
    File.Copy(latestPath, Path.Combine(outputDir, $"{stamp}.json"), overwrite: true);

    // Placeholder docref-index.json for load tests
    File.WriteAllText(Path.Combine(outputDir, "docref-index.json"),
        new JsonObject().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"Load-tests snapshot written: {latestPath}");
    Console.WriteLine($"  Fresh: {scenarios.Count} scenarios, Carried forward: {carriedForwardCount} module(s)");
    Console.WriteLine($"  Total: {totalScenarios} (pass: {passing}, fail: {failing})");
}

/// <summary>
/// Infer the provider/category from the artifact directory path.
/// e.g. "broker-load-metrics-kafka" → "kafka"
///      "database-load-metrics-efcore-sqlserver" → "efcore-sqlserver"
///      "caching-load-metrics-redis" → "redis"
///      "load-metrics" → "harness"
/// </summary>
static string InferProviderFromPath(string filePath, string inputDir)
{
    var rel = Path.GetRelativePath(inputDir, filePath).Replace('\\', '/');
    var firstSegment = rel.Split('/').FirstOrDefault() ?? "";

    // Strip the category prefix to get the provider
    var prefixes = new[] { "database-load-metrics-", "caching-load-metrics-",
                           "locking-load-metrics-", "broker-load-metrics-",
                           "messaging-load-metrics" };
    foreach (var prefix in prefixes)
    {
        if (firstSegment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return firstSegment[prefix.Length..].TrimEnd('-');
    }

    if (firstSegment == "load-metrics") return "harness";
    return firstSegment;
}

static string ExtractArea(string scenarioName)
{
    // e.g. "db-uow" → "database", "cache-memory" → "caching", etc.
    // Best-effort heuristic; authoritative mapping will come from manifests in Phase 2.
    if (scenarioName.StartsWith("db-", StringComparison.OrdinalIgnoreCase)) return "database";
    if (scenarioName.StartsWith("cache-", StringComparison.OrdinalIgnoreCase)) return "caching";
    if (scenarioName.StartsWith("lock-", StringComparison.OrdinalIgnoreCase)) return "locking";
    if (scenarioName.StartsWith("broker-", StringComparison.OrdinalIgnoreCase)) return "brokers";
    if (scenarioName.StartsWith("msg-", StringComparison.OrdinalIgnoreCase)) return "messaging";
    if (scenarioName.StartsWith("idgen-", StringComparison.OrdinalIgnoreCase)) return "id-generation";
    var dashIdx = scenarioName.IndexOf('-', StringComparison.Ordinal);
    return dashIdx > 0 ? scenarioName.Substring(0, dashIdx) : scenarioName;
}

// ──────────────────────────────────────────────────────────────────────────
// METADATA
// ──────────────────────────────────────────────────────────────────────────
static JsonObject BuildMetadata(long runId, string sha)
{
    return new JsonObject
    {
        ["runnerOs"] = Environment.OSVersion.Platform.ToString(),
        ["runnerOsVersion"] = Environment.OSVersion.VersionString,
        ["runnerArch"] = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
        ["cpuCount"] = Environment.ProcessorCount,
        ["dotnetVersion"] = Environment.Version.ToString(),
        ["runId"] = runId,
        ["sha"] = sha,
        ["workflow"] = Environment.GetEnvironmentVariable("GITHUB_WORKFLOW") ?? "",
        ["eventType"] = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") ?? "",
        // Default is "unknown" (not "short") so a misconfigured job that forgets to
        // propagate PERF_JOB_TYPE fails the publish gate LOUDLY instead of silently
        // being treated as a short run and skipped. The publish-benchmarks.yml gate
        // treats anything other than "medium"/"default" as non-publishable, so
        // "unknown" will still skip — but the jobType value in latest.json will show
        // "unknown", which is diagnosable. Before, a misconfigured medium run looked
        // indistinguishable from a real short run.
        ["jobType"] = Environment.GetEnvironmentVariable("PERF_JOB_TYPE") ?? "unknown"
    };
}

static void WriteEmptySnapshot(string kind, string outputDir, long runId, string sha)
{
    Directory.CreateDirectory(outputDir);
    var empty = new JsonObject
    {
        ["kind"] = kind,
        ["timestamp"] = DateTime.UtcNow.ToString("o"),
        ["runId"] = runId > 0 ? JsonValue.Create(runId) : null,
        ["sha"] = string.IsNullOrEmpty(sha) ? null : JsonValue.Create(sha),
        ["metadata"] = BuildMetadata(runId, sha),
        ["overall"] = new JsonObject(),
        ["modules"] = new JsonArray(),
        ["note"] = "No input data found — empty snapshot emitted."
    };
    File.WriteAllText(Path.Combine(outputDir, "latest.json"),
        empty.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
}

static double GetDouble(JsonNode? node, string property)
{
    if (node is null) return 0;
    var v = node[property];
    if (v is null) return 0;
    // JsonValue with null underlying value (seen in BenchmarkDotNet when N=0, e.g. benchmarks
    // that ran in Dry mode but never produced workload measurements).
    try
    {
        var str = v.ToString();
        if (string.IsNullOrEmpty(str) || str == "null") return 0;
        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        return v.GetValue<double>();
    }
    catch
    {
        return 0;
    }
}

// ──────────────────────────────────────────────────────────────────────────
// MODEL CLASSES
// ──────────────────────────────────────────────────────────────────────────
sealed class BenchmarkModule
{
    public string Name { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public List<BenchmarkEntry> Benchmarks { get; } = new();

    public JsonObject ToJson()
    {
        var arr = new JsonArray();
        foreach (var b in Benchmarks) arr.Add(b.ToJson());

        // Phase 3.3 — per-module stability summary for the dashboard.
        // ExpectedUnstable methods are counted separately so the module tile shows
        // its true stability ratio without being dragged down by known-noisy benchmarks.
        int stableCount = 0, unstableCount = 0, expectedUnstableCount = 0;
        double covSum = 0; int covCount = 0;
        foreach (var b in Benchmarks)
        {
            if (b.ExpectedUnstable && !b.Stable) expectedUnstableCount++;
            else if (b.Stable) stableCount++;
            else unstableCount++;
            if (b.Cov is > 0 and < double.PositiveInfinity) { covSum += b.Cov; covCount++; }
        }

        var obj = new JsonObject { ["name"] = Name };
        if (!string.IsNullOrEmpty(Fingerprint)) obj["fingerprint"] = Fingerprint;
        obj["carriedForward"] = false;
        obj["stableMethods"] = stableCount;
        obj["unstableMethods"] = unstableCount;
        obj["expectedUnstableMethods"] = expectedUnstableCount;
        obj["meanCov"] = covCount > 0 ? Math.Round(covSum / covCount, 4) : 0;
        obj["benchmarks"] = arr;
        return obj;
    }
}

sealed class BenchmarkEntry
{
    public string Class { get; set; } = "";
    public string Method { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Parameters { get; set; } = "";
    public string? DocRef { get; set; }
    public double MeanNs { get; set; }
    public double MedianNs { get; set; }
    public double StdDevNs { get; set; }
    public double MinNs { get; set; }
    public double MaxNs { get; set; }
    public double CI99LowerNs { get; set; }
    public double CI99UpperNs { get; set; }
    public int N { get; set; }
    public double AllocatedBytes { get; set; }
    public int Gen0 { get; set; }
    public int Gen1 { get; set; }
    public int Gen2 { get; set; }
    public double Cov { get; set; }
    public bool Stable { get; set; }

    // Palanca 3: methods explicitly listed in the manifest's stabilityOverrides
    // map. Expected-unstable benchmarks are left in the dashboard (so the numbers
    // are visible) but excluded from the stable/unstable headline ratio and from
    // regression detection. The reason string is surfaced in the dashboard tooltip.
    public bool ExpectedUnstable { get; set; }
    public string? ExpectedUnstableReason { get; set; }

    public JsonObject ToJson() => new()
    {
        ["class"] = Class,
        ["method"] = Method,
        ["fullName"] = FullName,
        ["parameters"] = Parameters,
        ["docRef"] = DocRef,
        ["meanNs"] = Safe(MeanNs, 3),
        ["medianNs"] = Safe(MedianNs, 3),
        ["stdDevNs"] = Safe(StdDevNs, 3),
        ["minNs"] = Safe(MinNs, 3),
        ["maxNs"] = Safe(MaxNs, 3),
        ["ci99LowerNs"] = Safe(CI99LowerNs, 3),
        ["ci99UpperNs"] = Safe(CI99UpperNs, 3),
        ["n"] = N,
        ["allocatedBytes"] = Safe(AllocatedBytes, 3),
        ["gen0"] = Gen0,
        ["gen1"] = Gen1,
        ["gen2"] = Gen2,
        ["cov"] = Safe(Cov, 4),
        ["stable"] = Stable,
        ["expectedUnstable"] = ExpectedUnstable,
        ["expectedUnstableReason"] = ExpectedUnstableReason
    };

    /// <summary>Round and sanitize: NaN/Infinity → 0 (JSON does not support named floats).</summary>
    private static double Safe(double v, int digits) =>
        double.IsNaN(v) || double.IsInfinity(v) ? 0 : Math.Round(v, digits);
}

sealed class LoadScenario
{
    public string Name { get; set; } = "";
    public long RequestCount { get; set; }
    public long FailCount { get; set; }
    public double Rps { get; set; }
    public double MeanMs { get; set; }
    public double P50Ms { get; set; }
    public double P95Ms { get; set; }
    public double P99Ms { get; set; }
    public double StdDevMs { get; set; }
    public double MinMs { get; set; }
    public double MaxMs { get; set; }
    public double ErrorRate { get; set; }

    public JsonObject ToJson() => new()
    {
        ["name"] = Name,
        ["requestCount"] = RequestCount,
        ["failCount"] = FailCount,
        ["rps"] = Math.Round(Rps, 2),
        ["meanMs"] = Math.Round(MeanMs, 3),
        ["p50Ms"] = Math.Round(P50Ms, 3),
        ["p95Ms"] = Math.Round(P95Ms, 3),
        ["p99Ms"] = Math.Round(P99Ms, 3),
        ["stdDevMs"] = Math.Round(StdDevMs, 3),
        ["minMs"] = Math.Round(MinMs, 3),
        ["maxMs"] = Math.Round(MaxMs, 3),
        ["errorRate"] = Math.Round(ErrorRate, 5)
    };
}
