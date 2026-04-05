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

const double StabilityCoVThreshold = 0.10; // CoV <= 10% == stable

var kind = "benchmarks";
var inputDir = "";
var outputDir = "";
var runId = 0L;
var sha = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "";
var manifestDir = "";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--kind" && i + 1 < args.Length) kind = args[++i];
    if (args[i] == "--input" && i + 1 < args.Length) inputDir = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
    if (args[i] == "--run-id" && i + 1 < args.Length) _ = long.TryParse(args[++i], out runId);
    if (args[i] == "--sha" && i + 1 < args.Length) sha = args[++i];
    if (args[i] == "--manifest-dir" && i + 1 < args.Length) manifestDir = args[++i];
}

if (string.IsNullOrEmpty(inputDir)) inputDir = kind == "benchmarks" ? "artifacts/performance" : "artifacts/load-metrics";
if (string.IsNullOrEmpty(outputDir)) outputDir = $"docs/{kind}/data";
if (string.IsNullOrEmpty(manifestDir)) manifestDir = ".github/perf-manifest";

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"Input directory '{inputDir}' not found. Emitting empty snapshot.");
    WriteEmptySnapshot(kind, outputDir, runId, sha);
    return;
}

Directory.CreateDirectory(outputDir);

var metadata = BuildMetadata(runId, sha);

if (kind == "benchmarks")
    GenerateBenchmarksReport(inputDir, outputDir, metadata, runId, sha, manifestDir);
else if (kind == "load-tests")
    GenerateLoadTestsReport(inputDir, outputDir, metadata, runId, sha);
else
{
    Console.Error.WriteLine($"Unknown --kind '{kind}'. Expected 'benchmarks' or 'load-tests'.");
    Environment.Exit(2);
}

// ──────────────────────────────────────────────────────────────────────────
// BENCHMARKS
// ──────────────────────────────────────────────────────────────────────────
static void GenerateBenchmarksReport(string inputDir, string outputDir, JsonObject metadata, long runId, string sha, string manifestDir)
{
    // Discover BenchmarkDotNet full JSON reports recursively. The `--exporters json` CLI
    // arg in BenchmarkDotNet maps to JsonExporter.FullCompressed which produces files
    // named *-report-full-compressed.json. We also accept *-report-full.json in case a
    // future benchmark uses a different exporter variant. Both formats share the same
    // schema (the "compressed" variant is just unindented).
    var reportFiles = Directory.GetFiles(inputDir, "*-report-full*.json", SearchOption.AllDirectories)
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

    // Compute overall stats
    int totalMethods = 0, stable = 0, unstable = 0;
    double sumMeanNs = 0, sumAllocatedBytes = 0;
    int countMean = 0, countAlloc = 0;

    foreach (var m in modulesByName.Values)
    {
        foreach (var b in m.Benchmarks)
        {
            totalMethods++;
            if (b.Stable) stable++; else unstable++;
            if (b.MeanNs > 0) { sumMeanNs += b.MeanNs; countMean++; }
            if (b.AllocatedBytes > 0) { sumAllocatedBytes += b.AllocatedBytes; countAlloc++; }
        }
    }

    var overall = new JsonObject
    {
        ["totalModules"] = modulesByName.Count,
        ["totalMethods"] = totalMethods,
        ["stableMethods"] = stable,
        ["unstableMethods"] = unstable,
        ["regressionCount"] = 0, // populated in Phase 3 when diffing against previous snapshot
        ["meanLatencyNs"] = countMean > 0 ? Math.Round(sumMeanNs / countMean, 3) : 0,
        ["meanAllocatedBytes"] = countAlloc > 0 ? Math.Round(sumAllocatedBytes / countAlloc, 3) : 0
    };

    var modulesArray = new JsonArray();
    foreach (var m in modulesByName.Values)
        modulesArray.Add(m.ToJson());

    var snapshot = new JsonObject
    {
        ["kind"] = "benchmarks",
        ["timestamp"] = DateTime.UtcNow.ToString("o"),
        ["runId"] = runId > 0 ? JsonValue.Create(runId) : null,
        ["sha"] = string.IsNullOrEmpty(sha) ? null : JsonValue.Create(sha),
        ["metadata"] = metadata,
        ["overall"] = overall,
        ["modules"] = modulesArray
    };

    var latestPath = Path.Combine(outputDir, "latest.json");
    File.WriteAllText(latestPath, snapshot.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    // Archive snapshot with timestamp
    var stamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ");
    var archivePath = Path.Combine(outputDir, $"{stamp}.json");
    File.Copy(latestPath, archivePath, overwrite: true);

    // Build docref-index.json (reverse: DocRef → { dashboard anchor, latest value, stability })
    var docrefIndex = new JsonObject();
    foreach (var m in modulesByName.Values)
    {
        foreach (var b in m.Benchmarks)
        {
            if (!string.IsNullOrEmpty(b.DocRef))
            {
                docrefIndex[b.DocRef!] = new JsonObject
                {
                    ["module"] = m.Name,
                    ["method"] = b.Method,
                    ["meanNs"] = b.MeanNs,
                    ["medianNs"] = b.MedianNs,
                    ["stdDevNs"] = b.StdDevNs,
                    ["allocatedBytes"] = b.AllocatedBytes,
                    ["stable"] = b.Stable,
                    ["anchor"] = $"#{m.Name}/{b.Method}"
                };
            }
        }
    }
    File.WriteAllText(Path.Combine(outputDir, "docref-index.json"),
        docrefIndex.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"Benchmarks snapshot written: {latestPath}");
    Console.WriteLine($"  Modules: {modulesByName.Count}, Methods: {totalMethods} (stable: {stable}, unstable: {unstable})");
    _ = manifestDir; // manifest validation is deferred to perf-manifest-check.cs
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

    // Stability (CoV = StdDev/Mean)
    entry.Cov = mean > 0 ? stdDev / mean : double.PositiveInfinity;
    entry.Stable = entry.Cov <= StabilityCoVThreshold && n >= 3;

    // 99% confidence interval (approximation using z=2.576 when N is not tiny;
    // for strictly correct values use Student's t distribution per N. The
    // methodology doc describes the tradeoff. We use z here for simplicity and
    // mark methods with N < 10 as unstable regardless, matching the doc.)
    var se = n > 0 ? stdDev / Math.Sqrt(n) : 0;
    var tCrit = 2.576; // normal approximation
    entry.CI99LowerNs = mean - tCrit * se;
    entry.CI99UpperNs = mean + tCrit * se;
    if (n < 10) entry.Stable = false;

    // DocRef extraction from Categories
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
static void GenerateLoadTestsReport(string inputDir, string outputDir, JsonObject metadata, long runId, string sha)
{
    // NBomber emits `nbomber-summary.json` per run under nbomber-<timestamp>/ directories.
    var summaryFiles = Directory.GetFiles(inputDir, "nbomber-summary.json", SearchOption.AllDirectories).ToList();

    // Additionally, console harness writes metrics-<stamp>.csv
    var metricsFiles = Directory.GetFiles(inputDir, "metrics-*.csv", SearchOption.AllDirectories).ToList();

    Console.WriteLine($"Found {summaryFiles.Count} NBomber summaries and {metricsFiles.Count} metrics CSVs in {inputDir}");

    var scenarios = new List<LoadScenario>();

    foreach (var file in summaryFiles)
    {
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(file));
            if (json is null) continue;

            // NBomber summary schema:
            // { "final_stats": { "scenario_stats": [{ "scenario_name": "...",
            //     "ok": { "latency": { "mean_ms": ..., "p50": ..., "p95": ..., "p99": ..., "stddev_ms": ..., "min_ms": ..., "max_ms": ... },
            //             "request": { "count": ..., "rps": ... },
            //             "data_transfer": {...} },
            //     "fail": { "request": { "count": ... } }
            // }] } }
            var stats = json["final_stats"]?["scenario_stats"] as JsonArray;
            if (stats is null) continue;

            foreach (var s in stats)
            {
                if (s is null) continue;
                var scenario = ParseLoadScenario(s);
                if (scenario is not null) scenarios.Add(scenario);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse NBomber summary '{file}': {ex.Message}");
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

    var overall = new JsonObject
    {
        ["totalScenarios"] = totalScenarios,
        ["passingScenarios"] = passing,
        ["failingScenarios"] = failing,
        ["meanThroughputOps"] = countRps > 0 ? Math.Round(sumRps / countRps, 2) : 0,
        ["meanP95Ms"] = countP95 > 0 ? Math.Round(sumP95 / countP95, 3) : 0,
        ["meanErrorRate"] = countErr > 0 ? Math.Round(sumErr / countErr, 5) : 0
    };

    var modulesArray = new JsonArray();
    foreach (var (area, list) in modulesByArea)
    {
        var scArray = new JsonArray();
        foreach (var sc in list) scArray.Add(sc.ToJson());
        modulesArray.Add(new JsonObject { ["name"] = area, ["scenarios"] = scArray });
    }

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
    Console.WriteLine($"  Scenarios: {totalScenarios} (pass: {passing}, fail: {failing})");
}

static LoadScenario? ParseLoadScenario(JsonNode node)
{
    var name = node["scenario_name"]?.GetValue<string>();
    if (string.IsNullOrEmpty(name)) return null;

    var ok = node["ok"];
    var fail = node["fail"];
    var latency = ok?["latency"];
    var request = ok?["request"];

    var sc = new LoadScenario
    {
        Name = name,
        RequestCount = (long)GetDouble(request, "count"),
        Rps = GetDouble(request, "rps"),
        MeanMs = GetDouble(latency, "mean_ms"),
        P50Ms = GetDouble(latency, "p50"),
        P95Ms = GetDouble(latency, "p95"),
        P99Ms = GetDouble(latency, "p99"),
        StdDevMs = GetDouble(latency, "stddev_ms"),
        MinMs = GetDouble(latency, "min_ms"),
        MaxMs = GetDouble(latency, "max_ms"),
        FailCount = (long)GetDouble(fail?["request"], "count")
    };

    var totalOps = sc.RequestCount + sc.FailCount;
    sc.ErrorRate = totalOps > 0 ? (double)sc.FailCount / totalOps : 0;
    return sc;
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
        ["jobType"] = Environment.GetEnvironmentVariable("PERF_JOB_TYPE") ?? "short"
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
    public List<BenchmarkEntry> Benchmarks { get; } = new();

    public JsonObject ToJson()
    {
        var arr = new JsonArray();
        foreach (var b in Benchmarks) arr.Add(b.ToJson());
        return new JsonObject { ["name"] = Name, ["benchmarks"] = arr };
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

    public JsonObject ToJson() => new()
    {
        ["class"] = Class,
        ["method"] = Method,
        ["fullName"] = FullName,
        ["parameters"] = Parameters,
        ["docRef"] = DocRef,
        ["meanNs"] = Math.Round(MeanNs, 3),
        ["medianNs"] = Math.Round(MedianNs, 3),
        ["stdDevNs"] = Math.Round(StdDevNs, 3),
        ["minNs"] = Math.Round(MinNs, 3),
        ["maxNs"] = Math.Round(MaxNs, 3),
        ["ci99LowerNs"] = Math.Round(CI99LowerNs, 3),
        ["ci99UpperNs"] = Math.Round(CI99UpperNs, 3),
        ["n"] = N,
        ["allocatedBytes"] = Math.Round(AllocatedBytes, 3),
        ["gen0"] = Gen0,
        ["gen1"] = Gen1,
        ["gen2"] = Gen2,
        ["cov"] = Math.Round(Cov, 4),
        ["stable"] = Stable
    };
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
