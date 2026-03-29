// coverage-recalculate.cs — Recalculates historical coverage from raw Cobertura XML artifacts
// Downloads raw artifacts from GitHub Actions and re-runs coverage-report.cs with current formula.
//
// Usage: dotnet run .github/scripts/coverage-recalculate.cs -- [--runs <N|all>] [--history <path>]
//
// Prerequisites:
//   - GitHub CLI (gh) installed and authenticated
//   - .NET 10+ (C# 14 file-based app)
//
// This script:
//   1. Reads history.json to find runs with runId
//   2. Downloads coverage-raw-{runId} artifacts via gh CLI
//   3. Re-runs coverage-report.cs on each set of XMLs
//   4. Updates history.json with recalculated values
#pragma warning disable CA1305, CA1310, CA1852

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

var historyPath = "docs/coverage/data/history.json";
var maxRuns = "all";
var repoOwner = "dlrivada/Encina";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--runs" && i + 1 < args.Length) maxRuns = args[++i];
    if (args[i] == "--history" && i + 1 < args.Length) historyPath = args[++i];
    if (args[i] == "--repo" && i + 1 < args.Length) repoOwner = args[++i];
}

// ─── Find history.json ─────────────────────────────────────────────────────
if (!File.Exists(historyPath))
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "docs", "coverage", "data", "history.json");
        if (File.Exists(candidate)) { historyPath = candidate; break; }
        dir = dir.Parent;
    }
}

if (!File.Exists(historyPath))
{
    Console.WriteLine($"ERROR: history.json not found at {historyPath}");
    return;
}

var history = JsonNode.Parse(File.ReadAllText(historyPath)) as JsonArray;
if (history is null || history.Count == 0)
{
    Console.WriteLine("ERROR: history.json is empty or invalid");
    return;
}

Console.WriteLine($"Loaded history with {history.Count} entries");

// ─── Identify runs to recalculate ──────────────────────────────────────────
var entriesToRecalc = new List<(int Index, long RunId, string Timestamp)>();

for (int i = 0; i < history.Count; i++)
{
    var entry = history[i];
    var runId = entry?["runId"]?.GetValue<long>() ?? 0;
    var timestamp = entry?["timestamp"]?.GetValue<string>() ?? "?";

    if (runId > 0)
        entriesToRecalc.Add((i, runId, timestamp));
    else
        Console.WriteLine($"  SKIP: entry {i} ({timestamp}) — no runId");
}

if (maxRuns != "all" && int.TryParse(maxRuns.Replace("last-", ""), out var limit))
    entriesToRecalc = entriesToRecalc.TakeLast(limit).ToList();

Console.WriteLine($"Will recalculate {entriesToRecalc.Count} entries\n");

// ─── Find coverage-report.cs ───────────────────────────────────────────────
var scriptPath = ".github/scripts/coverage-report.cs";
if (!File.Exists(scriptPath))
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, ".github", "scripts", "coverage-report.cs");
        if (File.Exists(candidate)) { scriptPath = candidate; break; }
        dir = dir.Parent;
    }
}

if (!File.Exists(scriptPath))
{
    Console.WriteLine("ERROR: coverage-report.cs not found");
    return;
}

// ─── Process each run ──────────────────────────────────────────────────────
var tempBase = Path.Combine(Path.GetTempPath(), "encina-recalc");
Directory.CreateDirectory(tempBase);

foreach (var (index, runId, timestamp) in entriesToRecalc)
{
    Console.WriteLine($"═══ Recalculating run {runId} ({timestamp}) ═══");

    var tempDir = Path.Combine(tempBase, runId.ToString());
    var inputDir = Path.Combine(tempDir, "test-results");
    var outputDir = Path.Combine(tempDir, "output");

    try
    {
        // Download raw artifact
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);

        var artifactName = $"coverage-raw-{runId}";
        Console.WriteLine($"  Downloading artifact: {artifactName}");

        var dlResult = RunProcess("gh", $"run download {runId} --name {artifactName} --dir \"{inputDir}\" --repo {repoOwner}");
        if (dlResult.ExitCode != 0)
        {
            // Try downloading individual test-results-* artifacts
            Console.WriteLine($"  No consolidated artifact. Trying individual test-results-* artifacts...");
            dlResult = RunProcess("gh", $"run download {runId} --pattern \"test-results-*\" --dir \"{inputDir}\" --repo {repoOwner}");
            if (dlResult.ExitCode != 0)
            {
                Console.WriteLine($"  FAILED: Could not download artifacts for run {runId}: {dlResult.Stderr}");
                continue;
            }
        }

        // Count XML files found
        var xmlFiles = Directory.GetFiles(inputDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
        Console.WriteLine($"  Found {xmlFiles.Length} Cobertura XML files");

        if (xmlFiles.Length == 0)
        {
            Console.WriteLine("  SKIP: No XML files found in artifact");
            continue;
        }

        // Run coverage-report.cs on the downloaded data
        Console.WriteLine("  Running coverage-report.cs...");
        var reportResult = RunProcess("dotnet", $"run \"{scriptPath}\" -- --input \"{inputDir}\" --output \"{outputDir}\"");

        if (reportResult.ExitCode != 0)
        {
            Console.WriteLine($"  FAILED: coverage-report.cs exited with code {reportResult.ExitCode}");
            Console.WriteLine($"  Stderr: {reportResult.Stderr}");
            continue;
        }

        // Read the recalculated summary
        var summaryPath = Path.Combine(outputDir, "encina-coverage-summary.json");
        if (!File.Exists(summaryPath))
        {
            Console.WriteLine("  FAILED: No summary output generated");
            continue;
        }

        var newData = JsonNode.Parse(File.ReadAllText(summaryPath));
        var newOverall = newData?["overall"];
        if (newOverall is null)
        {
            Console.WriteLine("  FAILED: No overall section in recalculated data");
            continue;
        }

        // Update history entry
        var entry = history[index]!;
        var oldCov = entry["coverage"]?.GetValue<int>() ?? 0;
        var newCov = (int)(newOverall["coverage"]?.GetValue<double>() ?? 0);
        var newCovered = Math.Round(newOverall["covered"]?.GetValue<double>() ?? 0, 2);

        entry["coverage"] = newCov;
        entry["lines"] = newOverall["lines"]?.GetValue<int>() ?? 0;
        entry["covered"] = newCovered;

        // Update per-category
        var newCats = new JsonObject();
        if (newData?["categories"] is JsonArray cats)
        {
            foreach (var cat in cats)
            {
                var name = cat?["name"]?.GetValue<string>();
                var cov = cat?["coverage"]?.GetValue<double>() ?? 0;
                if (name is not null) newCats[name] = (int)cov;
            }
        }
        entry["categories"] = newCats;

        // Update per-flag overall coverage (for trend chart filtering)
        var overallPerFlag = newOverall["perFlag"];
        if (overallPerFlag is JsonObject flagObj)
        {
            var perFlag = new JsonObject();
            foreach (var (flagName, flagNode) in flagObj)
            {
                var cov = flagNode?["coverage"]?.GetValue<double>() ?? 0;
                perFlag[flagName] = Math.Round(cov, 2);
            }
            entry["perFlag"] = perFlag;
        }

        Console.WriteLine($"  Updated: {oldCov}% → {newCov}% (covered: {newCovered:N1})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
    }
    finally
    {
        // Cleanup temp directory for this run
        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
        catch { /* ignore cleanup errors */ }
    }

    Console.WriteLine();
}

// ─── Write updated history ─────────────────────────────────────────────────
var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(historyPath, history.ToJsonString(jsonOpts));
Console.WriteLine($"History updated: {historyPath} ({history.Count} entries)");

// ─── Cleanup ────────────────────────────────────────────────────────────────
try { if (Directory.Exists(tempBase)) Directory.Delete(tempBase, recursive: true); }
catch { /* ignore */ }

Console.WriteLine("Done.");

// ─── Helper ─────────────────────────────────────────────────────────────────

static (int ExitCode, string Stdout, string Stderr) RunProcess(string fileName, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var proc = Process.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit(TimeSpan.FromMinutes(5));

    return (proc.ExitCode, stdout, stderr);
}
