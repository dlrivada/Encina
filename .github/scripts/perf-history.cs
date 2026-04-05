// perf-history.cs — Appends a performance snapshot to the history file
// Usage: dotnet run .github/scripts/perf-history.cs -- --latest <path> --history <path>
// Requires: .NET 10+ (C# 14 file-based app)
//
// Design notes (see ADR-025 + coverage-measurement-methodology.md):
// - Unlike coverage-history.cs, this file keeps ALL entries forever (no 100-entry cap).
//   Raw data is archived in the `perf-raw` orphan branch; history.json is the fast
//   lightweight index used by the dashboard.
// - Supports both `benchmarks` and `load-tests` kinds via --kind flag (default: benchmarks).
// - Entry schema is intentionally minimal: headline metrics only. Full per-method /
//   per-scenario data lives in the archived snapshot JSON referenced by `file`.
#pragma warning disable CA1305 // IFormatProvider not relevant for standalone scripts

using System.Text.Json;
using System.Text.Json.Nodes;

var kind = "benchmarks"; // or "load-tests"
var latestPath = "";
var historyPath = "";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--kind" && i + 1 < args.Length) kind = args[++i];
    if (args[i] == "--latest" && i + 1 < args.Length) latestPath = args[++i];
    if (args[i] == "--history" && i + 1 < args.Length) historyPath = args[++i];
}

// Defaults based on kind
if (string.IsNullOrEmpty(latestPath)) latestPath = $"docs/{kind}/data/latest.json";
if (string.IsNullOrEmpty(historyPath)) historyPath = $"docs/{kind}/data/history.json";

if (!File.Exists(latestPath))
{
    Console.WriteLine($"No latest.json found at {latestPath}, skipping history update.");
    return;
}

var latestJson = JsonNode.Parse(File.ReadAllText(latestPath));
if (latestJson is null)
{
    Console.WriteLine("Failed to parse latest.json");
    return;
}

var overall = latestJson["overall"];
if (overall is null)
{
    Console.WriteLine("No 'overall' section in latest.json");
    return;
}

// Build lightweight history entry. The full snapshot remains in the archived file.
var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
var snapshotFile = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ") + ".json";
var entry = new JsonObject
{
    ["timestamp"] = timestamp,
    ["file"] = snapshotFile,
    ["kind"] = kind
};

// runId enables retroactive recalculation via perf-recalculate.cs + perf-raw branch
var runId = latestJson["runId"];
if (runId is not null) entry["runId"] = runId.GetValue<long>();

// Commit sha for traceability
var sha = latestJson["sha"];
if (sha is not null) entry["sha"] = sha.GetValue<string>();

// Headline metrics depend on kind
if (kind == "benchmarks")
{
    // Summary metrics for trend rendering
    entry["totalMethods"] = overall["totalMethods"]?.GetValue<int>() ?? 0;
    entry["stableMethods"] = overall["stableMethods"]?.GetValue<int>() ?? 0;
    entry["unstableMethods"] = overall["unstableMethods"]?.GetValue<int>() ?? 0;
    entry["regressionCount"] = overall["regressionCount"]?.GetValue<int>() ?? 0;
    entry["totalModules"] = overall["totalModules"]?.GetValue<int>() ?? 0;

    // Aggregate metrics (geometric means where applicable, computed upstream by perf-report.cs)
    if (overall["meanLatencyNs"] is JsonValue ml) entry["meanLatencyNs"] = ml.GetValue<double>();
    if (overall["meanAllocatedBytes"] is JsonValue ma) entry["meanAllocatedBytes"] = ma.GetValue<double>();
}
else if (kind == "load-tests")
{
    entry["totalScenarios"] = overall["totalScenarios"]?.GetValue<int>() ?? 0;
    entry["passingScenarios"] = overall["passingScenarios"]?.GetValue<int>() ?? 0;
    entry["failingScenarios"] = overall["failingScenarios"]?.GetValue<int>() ?? 0;

    if (overall["meanThroughputOps"] is JsonValue mt) entry["meanThroughputOps"] = mt.GetValue<double>();
    if (overall["meanP95Ms"] is JsonValue mp) entry["meanP95Ms"] = mp.GetValue<double>();
    if (overall["meanErrorRate"] is JsonValue me) entry["meanErrorRate"] = me.GetValue<double>();
}

// Environment metadata (subset — full metadata is in the archived snapshot)
var metadata = latestJson["metadata"];
if (metadata is JsonObject md)
{
    var envSummary = new JsonObject();
    foreach (var key in new[] { "runnerOs", "runnerArch", "dotnetVersion" })
    {
        if (md[key] is JsonValue v) envSummary[key] = v.GetValue<string>();
    }
    if (envSummary.Count > 0) entry["env"] = envSummary;
}

// Load or create history array
JsonArray history;
if (File.Exists(historyPath))
{
    var existing = JsonNode.Parse(File.ReadAllText(historyPath));
    history = existing as JsonArray ?? [];
}
else
{
    history = [];
}

// Append — NO cap. Raw data lives forever (see ADR-025).
history.Add(entry);

var dir = Path.GetDirectoryName(historyPath);
if (dir is not null) Directory.CreateDirectory(dir);

File.WriteAllText(historyPath, history.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"{kind} history updated: {history.Count} entries, snapshot = {snapshotFile}");
