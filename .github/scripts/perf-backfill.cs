// perf-backfill.cs — One-shot backfill of perf-raw from GitHub Actions artifacts.
//
// Usage:
//   dotnet run .github/scripts/perf-backfill.cs -- \
//     --workflow "Benchmarks" \
//     --kind benchmarks \
//     --perf-raw .perf-raw \
//     --limit 50 \
//     --dry-run
//
// Design notes (see ADR-025 §1.4):
// - Enumerates all successful runs of the specified workflow via `gh run list`.
// - For each run NOT already archived in perf-raw, attempts to download artifacts.
// - Archives raw data under perf-raw/<kind>/<YYYY-MM-DD>/<runId>/ with metadata.json.
// - Commits each run individually (one commit per run for traceability).
// - Handles expired artifacts gracefully (>90 days) by logging and skipping.
// - Requires `gh` CLI authenticated and perf-raw checked out as a worktree.
//
// Prerequisites:
//   git fetch origin perf-raw
//   git worktree add .perf-raw origin/perf-raw
//   gh auth status
#pragma warning disable CA1305

using System.Diagnostics;
using System.Text.Json.Nodes;

var workflow = "Benchmarks";
var kind = "benchmarks";
var perfRawDir = ".perf-raw";
var limit = 50;
var dryRun = false;
var repo = "dlrivada/Encina";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--workflow" && i + 1 < args.Length) workflow = args[++i];
    if (args[i] == "--kind" && i + 1 < args.Length) kind = args[++i];
    if (args[i] == "--perf-raw" && i + 1 < args.Length) perfRawDir = args[++i];
    if (args[i] == "--limit" && i + 1 < args.Length) _ = int.TryParse(args[++i], out limit);
    if (args[i] == "--dry-run") dryRun = true;
    if (args[i] == "--repo" && i + 1 < args.Length) repo = args[++i];
}

if (!Directory.Exists(perfRawDir))
{
    Console.Error.WriteLine($"perf-raw directory not found: {perfRawDir}");
    Console.Error.WriteLine("Set up the worktree first:");
    Console.Error.WriteLine("  git fetch origin perf-raw");
    Console.Error.WriteLine("  git worktree add .perf-raw origin/perf-raw");
    Environment.Exit(2);
}

// Discover already-archived run IDs by scanning the directory tree
var archivedRunIds = new HashSet<string>(StringComparer.Ordinal);
var kindDir = Path.Combine(perfRawDir, kind);
if (Directory.Exists(kindDir))
{
    foreach (var dateDir in Directory.GetDirectories(kindDir))
    {
        foreach (var runDir in Directory.GetDirectories(dateDir))
            archivedRunIds.Add(Path.GetFileName(runDir));
    }
}
Console.WriteLine($"Already archived: {archivedRunIds.Count} run(s) in {kindDir}");

// List successful runs via gh CLI
Console.WriteLine($"Fetching successful runs for workflow '{workflow}' (limit {limit})...");
var listResult = RunProcess("gh",
    $"run list --workflow \"{workflow}\" --status success --limit {limit} --repo {repo} --json databaseId,createdAt,headSha");

if (listResult.ExitCode != 0)
{
    Console.Error.WriteLine($"Failed to list runs: {listResult.Stderr}");
    Environment.Exit(2);
}

var runs = JsonNode.Parse(listResult.Stdout) as JsonArray ?? [];
Console.WriteLine($"Found {runs.Count} successful run(s)");

int backfilled = 0, skipped = 0, failed = 0, expired = 0;

foreach (var run in runs)
{
    var runId = run?["databaseId"]?.GetValue<long>() ?? 0;
    var createdAt = run?["createdAt"]?.GetValue<string>() ?? "";
    var sha = run?["headSha"]?.GetValue<string>() ?? "";
    var runIdStr = runId.ToString();

    if (archivedRunIds.Contains(runIdStr))
    {
        skipped++;
        continue;
    }

    // Extract date from createdAt for directory structure
    var date = createdAt.Length >= 10 ? createdAt[..10] : DateTime.UtcNow.ToString("yyyy-MM-dd");
    var targetDir = Path.Combine(perfRawDir, kind, date, runIdStr);

    Console.WriteLine();
    Console.WriteLine($"  Run {runId} ({date}, sha={sha[..Math.Min(7, sha.Length)]})");

    if (dryRun)
    {
        Console.WriteLine($"    [dry-run] Would download to {targetDir}");
        backfilled++;
        continue;
    }

    // Create temp download directory
    var tempDir = Path.Combine(Path.GetTempPath(), $"perf-backfill-{runId}");
    try
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        Directory.CreateDirectory(tempDir);

        // Try downloading all benchmark artifacts
        // Pre-Phase1 runs have artifacts like "benchmarks-Core", "benchmarks-Polly"
        // Phase1+ runs have "benchmarks-Core-MediatorBenchmarks" etc.
        // Phase2+ runs also have "perf-report-benchmarks" and "perf-raw-benchmarks-<runId>"
        // We download everything matching "benchmarks-*" pattern.
        var dlResult = RunProcess("gh",
            $"run download {runId} --pattern \"benchmarks-*\" --dir \"{tempDir}\" --repo {repo}");

        if (dlResult.ExitCode != 0)
        {
            // Check if it's an expiry issue
            if (dlResult.Stderr.Contains("expired", StringComparison.OrdinalIgnoreCase)
                || dlResult.Stderr.Contains("no artifact", StringComparison.OrdinalIgnoreCase)
                || dlResult.Stderr.Contains("no valid", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"    ⏭ Artifacts expired or unavailable — skipping");
                expired++;
                continue;
            }

            // Try downloading without pattern (gets everything)
            dlResult = RunProcess("gh",
                $"run download {runId} --dir \"{tempDir}\" --repo {repo}");

            if (dlResult.ExitCode != 0)
            {
                Console.WriteLine($"    ❌ Download failed: {dlResult.Stderr.Trim()[..Math.Min(200, dlResult.Stderr.Trim().Length)]}");
                failed++;
                continue;
            }
        }

        // Check if we got any files
        var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            Console.WriteLine($"    ⏭ No files in downloaded artifacts — skipping");
            expired++;
            continue;
        }

        Console.WriteLine($"    Downloaded {files.Length} file(s)");

        // Move to perf-raw target
        Directory.CreateDirectory(targetDir);

        // Copy all files preserving structure
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(tempDir, file);
            var destPath = Path.Combine(targetDir, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir is not null) Directory.CreateDirectory(destDir);
            File.Copy(file, destPath, overwrite: true);
        }

        // Write metadata
        var metadata = new JsonObject
        {
            ["runId"] = runId,
            ["sha"] = sha,
            ["timestamp"] = createdAt,
            ["workflow"] = workflow,
            ["kind"] = kind,
            ["backfilled"] = true,
            ["backfilledAt"] = DateTime.UtcNow.ToString("o")
        };
        File.WriteAllText(
            Path.Combine(targetDir, "metadata.json"),
            metadata.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        // Git commit in perf-raw worktree
        var gitAdd = RunProcessInDir(perfRawDir, "git", "add -A");
        if (gitAdd.ExitCode != 0)
        {
            Console.WriteLine($"    ❌ git add failed: {gitAdd.Stderr}");
            failed++;
            continue;
        }

        var gitCommit = RunProcessInDir(perfRawDir, "git",
            $"commit -m \"perf-raw: backfill {kind} run {runId} ({date})\"");
        if (gitCommit.ExitCode != 0)
        {
            if (gitCommit.Stdout.Contains("nothing to commit"))
            {
                Console.WriteLine($"    ⏭ Nothing new to commit — skipping");
                skipped++;
            }
            else
            {
                Console.WriteLine($"    ❌ git commit failed: {gitCommit.Stderr}");
                failed++;
            }
            continue;
        }

        Console.WriteLine($"    ✅ Archived ({files.Length} files)");
        backfilled++;
    }
    finally
    {
        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); }
        catch { /* best-effort cleanup */ }
    }
}

Console.WriteLine();
Console.WriteLine("## Backfill Summary");
Console.WriteLine($"  Total runs found:   {runs.Count}");
Console.WriteLine($"  Already archived:   {skipped}");
Console.WriteLine($"  Backfilled:         {backfilled}");
Console.WriteLine($"  Expired/empty:      {expired}");
Console.WriteLine($"  Failed:             {failed}");

if (backfilled > 0 && !dryRun)
{
    Console.WriteLine();
    Console.WriteLine("Push the backfilled data:");
    Console.WriteLine($"  cd {perfRawDir} && git push origin HEAD:perf-raw");
}


static ProcessResult RunProcess(string fileName, string arguments)
{
    var psi = new ProcessStartInfo(fileName, arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    var proc = Process.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    return new ProcessResult(proc.ExitCode, stdout, stderr);
}

static ProcessResult RunProcessInDir(string workDir, string fileName, string arguments)
{
    var psi = new ProcessStartInfo(fileName, arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        WorkingDirectory = Path.GetFullPath(workDir)
    };
    psi.Environment["GIT_AUTHOR_NAME"] = "github-actions[bot]";
    psi.Environment["GIT_AUTHOR_EMAIL"] = "github-actions[bot]@users.noreply.github.com";
    psi.Environment["GIT_COMMITTER_NAME"] = "github-actions[bot]";
    psi.Environment["GIT_COMMITTER_EMAIL"] = "github-actions[bot]@users.noreply.github.com";
    var proc = Process.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    return new ProcessResult(proc.ExitCode, stdout, stderr);
}

sealed record ProcessResult(int ExitCode, string Stdout, string Stderr);
