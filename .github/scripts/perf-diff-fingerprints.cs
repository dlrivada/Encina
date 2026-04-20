// perf-diff-fingerprints.cs — Compare current fingerprints against a previous snapshot
//                              and emit a filtered matrix for the benchmarks workflow.
//
// Usage:
//   dotnet run .github/scripts/perf-diff-fingerprints.cs -- \
//     --current artifacts/perf-output/fingerprints.json \
//     --previous artifacts/perf-previous/latest.json \
//     --full-matrix full-matrix.json \
//     --changed-out /tmp/changed.json \
//     --skipped-out /tmp/skipped.json
//
// Design notes (see ADR-025 §7 + performance-measurement-methodology.md):
// - `current` is a flat { project → fingerprint } map produced by perf-fingerprint.cs.
// - `previous` is the full latest.json snapshot from the last successful run.
//   Each module in `previous.modules[]` may have a `fingerprint` field. A
//   module whose fingerprint matches the current value is unchanged; its
//   matrix entry is removed from `changed-out` and added to `skipped-out`.
// - `full-matrix` is the static list of every benchmark project (kept inline in
//   benchmarks.yml). Each entry has { name, path, project, os }.
// - A project with no previous fingerprint (new, renamed, or previous run
//   was forced-full but didn't publish fingerprints) is treated as changed
//   and included in the run.
// - A project whose source fingerprint changed is included in the run.
// - A project whose fingerprint is identical to the previous one is skipped
//   and will be carried forward by perf-report.cs in the aggregate job.
#pragma warning disable CA1305

using System.Text.Json;
using System.Text.Json.Nodes;

var currentPath = "";
var previousPath = "";
var fullMatrixPath = "";
var changedOut = "/tmp/changed.json";
var skippedOut = "/tmp/skipped.json";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--current" && i + 1 < args.Length) currentPath = args[++i];
    if (args[i] == "--previous" && i + 1 < args.Length) previousPath = args[++i];
    if (args[i] == "--full-matrix" && i + 1 < args.Length) fullMatrixPath = args[++i];
    if (args[i] == "--changed-out" && i + 1 < args.Length) changedOut = args[++i];
    if (args[i] == "--skipped-out" && i + 1 < args.Length) skippedOut = args[++i];
}

if (string.IsNullOrEmpty(currentPath) || !File.Exists(currentPath))
{
    Console.Error.WriteLine($"--current file missing: {currentPath}");
    Environment.Exit(2);
}
if (string.IsNullOrEmpty(fullMatrixPath) || !File.Exists(fullMatrixPath))
{
    Console.Error.WriteLine($"--full-matrix file missing: {fullMatrixPath}");
    Environment.Exit(2);
}

// Load current fingerprints: { project-name → sha256 }
var currentFps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var currentJson = JsonNode.Parse(File.ReadAllText(currentPath));
if (currentJson is JsonObject currentObj)
{
    foreach (var (key, value) in currentObj)
    {
        if (value is JsonValue v) currentFps[key] = v.GetValue<string>();
    }
}

// Load previous fingerprints from the latest.json snapshot (per-module).
// Key by both the short module name (as in matrix: "Refit", "Core", …) and the
// full project name when we can recover it from the stored metadata.
var previousFps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
if (!string.IsNullOrEmpty(previousPath) && File.Exists(previousPath))
{
    try
    {
        var prev = JsonNode.Parse(File.ReadAllText(previousPath));
        var modules = prev?["modules"] as JsonArray;
        if (modules is not null)
        {
            foreach (var m in modules)
            {
                if (m is not JsonObject mo) continue;
                var name = mo["name"]?.GetValue<string>();
                var fp = mo["fingerprint"]?.GetValue<string?>();
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(fp))
                    previousFps[name!] = fp!;
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to load previous snapshot: {ex.Message}");
    }
}

// Load full matrix and filter
var fullMatrix = JsonNode.Parse(File.ReadAllText(fullMatrixPath)) as JsonArray ?? new JsonArray();
var changed = new JsonArray();
var skipped = new JsonArray();

foreach (var entry in fullMatrix)
{
    if (entry is not JsonObject eo) continue;
    var matrixName = eo["name"]?.GetValue<string>() ?? "";
    var projectKey = eo["project"]?.GetValue<string>() ?? "";

    // Current fingerprint is keyed by project name (e.g. "Encina.Refit.Benchmarks").
    if (!currentFps.TryGetValue(projectKey, out var currentFp) || string.IsNullOrEmpty(currentFp))
    {
        Console.WriteLine($"  [run]  {matrixName,-25} no current fingerprint available");
        changed.Add(eo.DeepClone());
        continue;
    }

    // Previous fingerprint is keyed by short matrix name (e.g. "Refit"), because
    // that is what perf-report.cs writes into latest.json modules[].name.
    if (!previousFps.TryGetValue(matrixName, out var prevFp) || string.IsNullOrEmpty(prevFp))
    {
        Console.WriteLine($"  [run]  {matrixName,-25} no previous fingerprint");
        changed.Add(eo.DeepClone());
        continue;
    }

    if (string.Equals(currentFp, prevFp, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"  [skip] {matrixName,-25} fingerprint unchanged ({currentFp[..12]}...)");
        skipped.Add(JsonValue.Create(matrixName));
    }
    else
    {
        Console.WriteLine($"  [run]  {matrixName,-25} fingerprint changed ({prevFp[..12]} → {currentFp[..12]})");
        changed.Add(eo.DeepClone());
    }
}

File.WriteAllText(changedOut, changed.ToJsonString());
File.WriteAllText(skippedOut, skipped.ToJsonString());

Console.WriteLine();
Console.WriteLine($"Summary: {changed.Count} project(s) to run, {skipped.Count} to carry forward.");
