// perf-recalculate.cs — Rebuild historical performance data from raw artifacts
//                       archived in the `perf-raw` orphan branch.
//
// Usage:
//   dotnet run .github/scripts/perf-recalculate.cs -- --kind benchmarks --perf-raw <dir>
//
// Design notes (see ADR-025):
// - The `perf-raw` orphan branch stores uncompressed raw BenchmarkDotNet /
//   NBomber outputs forever, one directory per CI run. This script iterates
//   those runs and reruns perf-report.cs against each, updating the history
//   and producing new snapshots that reflect the *current* formulas.
// - Unlike coverage-recalculate.cs which calls `gh run download` against
//   GitHub Actions artifacts (90-day retention), this script operates on a
//   local checkout of `perf-raw`. It is expected the caller has cloned the
//   branch: `git fetch origin perf-raw && git worktree add .perf-raw origin/perf-raw`.
// - The script is idempotent: running it twice produces the same output.
#pragma warning disable CA1305

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

var kind = "benchmarks";
var perfRawDir = ".perf-raw"; // worktree where perf-raw is checked out
var historyPath = "";
var dataDir = "";
var scriptDir = ".github/scripts";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--kind" && i + 1 < args.Length) kind = args[++i];
    if (args[i] == "--perf-raw" && i + 1 < args.Length) perfRawDir = args[++i];
    if (args[i] == "--history" && i + 1 < args.Length) historyPath = args[++i];
    if (args[i] == "--data-dir" && i + 1 < args.Length) dataDir = args[++i];
    if (args[i] == "--script-dir" && i + 1 < args.Length) scriptDir = args[++i];
}

if (string.IsNullOrEmpty(historyPath)) historyPath = $"docs/{kind}/data/history.json";
if (string.IsNullOrEmpty(dataDir)) dataDir = $"docs/{kind}/data";

var runsRoot = Path.Combine(perfRawDir, kind);
if (!Directory.Exists(runsRoot))
{
    Console.Error.WriteLine($"Raw runs directory not found: {runsRoot}");
    Console.Error.WriteLine("Ensure perf-raw branch is checked out. Example:");
    Console.Error.WriteLine("  git fetch origin perf-raw");
    Console.Error.WriteLine("  git worktree add .perf-raw origin/perf-raw");
    Environment.Exit(2);
}

Console.WriteLine($"Recalculating {kind} history from {runsRoot}");

// Layout: perf-raw/<kind>/<YYYY-MM-DD>/<runId>/...
var dateDirs = Directory.GetDirectories(runsRoot).OrderBy(d => d, StringComparer.Ordinal).ToList();
var newHistory = new JsonArray();
var processedCount = 0;
var skippedCount = 0;

foreach (var dateDir in dateDirs)
{
    var runDirs = Directory.GetDirectories(dateDir).OrderBy(d => d, StringComparer.Ordinal);
    foreach (var runDir in runDirs)
    {
        var runId = Path.GetFileName(runDir);
        var metadataFile = Path.Combine(runDir, "metadata.json");
        if (!File.Exists(metadataFile))
        {
            Console.WriteLine($"  SKIP {runId}: no metadata.json");
            skippedCount++;
            continue;
        }

        JsonNode? metadata;
        try
        {
            metadata = JsonNode.Parse(File.ReadAllText(metadataFile));
        }
        catch
        {
            Console.WriteLine($"  SKIP {runId}: unreadable metadata.json");
            skippedCount++;
            continue;
        }

        var sha = metadata?["sha"]?.GetValue<string>() ?? "";
        var timestamp = metadata?["timestamp"]?.GetValue<string>() ?? Path.GetFileName(dateDir);

        // Create a temporary output dir to avoid trashing docs/ data during iteration
        var tempOut = Path.Combine(Path.GetTempPath(), $"perf-recalc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempOut);

        try
        {
            var scriptPath = Path.Combine(scriptDir, "perf-report.cs");
            var psi = new ProcessStartInfo("dotnet",
                $"run \"{scriptPath}\" -- --kind {kind} --input \"{runDir}\" --output \"{tempOut}\" --run-id {runId} --sha {sha}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            var proc = Process.Start(psi)!;
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                Console.WriteLine($"  FAIL {runId}: perf-report.cs exited with {proc.ExitCode}");
                Console.WriteLine(proc.StandardError.ReadToEnd());
                skippedCount++;
                continue;
            }

            var latest = Path.Combine(tempOut, "latest.json");
            if (!File.Exists(latest))
            {
                Console.WriteLine($"  FAIL {runId}: no latest.json produced");
                skippedCount++;
                continue;
            }

            // Build history entry from the recalculated snapshot
            var snapshot = JsonNode.Parse(File.ReadAllText(latest));
            if (snapshot?["overall"] is not JsonObject overall) continue;

            var entry = new JsonObject
            {
                ["timestamp"] = timestamp,
                ["file"] = $"{timestamp.Replace(":", "").Replace("-", "")}.json",
                ["kind"] = kind,
                ["runId"] = long.TryParse(runId, out var rid) ? rid : 0,
                ["sha"] = sha
            };

            foreach (var (k, v) in overall)
            {
                if (v is not null) entry[k] = v.DeepClone();
            }

            newHistory.Add(entry);
            processedCount++;
        }
        finally
        {
            try { Directory.Delete(tempOut, recursive: true); } catch { /* best-effort */ }
        }
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);
File.WriteAllText(historyPath, newHistory.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Recalculation complete.");
Console.WriteLine($"  Processed: {processedCount} runs");
Console.WriteLine($"  Skipped:   {skippedCount} runs");
Console.WriteLine($"  History:   {historyPath}");
