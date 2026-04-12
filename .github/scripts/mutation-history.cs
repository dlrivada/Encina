// mutation-history.cs — Converts Stryker mutation-report.json into dashboard data
// Usage: dotnet run .github/scripts/mutation-history.cs -- --report <path> --latest <path> --history <path> [--src-root <path>] [--run-id <id>] [--scope <glob>]
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

var reportPath = "artifacts/mutation/reports/mutation-report.json";
var latestPath = "docs/mutations/data/latest.json";
var historyPath = "docs/mutations/data/history.json";
var srcRoot = "src/Encina";
var runId = 0L;
var scope = "";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--report" && i + 1 < args.Length) reportPath = args[++i];
    if (args[i] == "--latest" && i + 1 < args.Length) latestPath = args[++i];
    if (args[i] == "--history" && i + 1 < args.Length) historyPath = args[++i];
    if (args[i] == "--src-root" && i + 1 < args.Length) srcRoot = args[++i];
    if (args[i] == "--run-id" && i + 1 < args.Length) runId = long.Parse(args[++i]);
    if (args[i] == "--scope" && i + 1 < args.Length) scope = args[++i];
}

if (!File.Exists(reportPath))
{
    Console.Error.WriteLine($"Stryker report not found at {reportPath}");
    return 1;
}

// ── Parse Stryker report ──────────────────────────────────────────────
var reportJson = JsonDocument.Parse(File.ReadAllBytes(reportPath));
var root = reportJson.RootElement;

if (!root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
{
    Console.Error.WriteLine("Report has no 'files' object.");
    return 1;
}

// Extract thresholds
double thresholdHigh = 85, thresholdLow = 70;
if (root.TryGetProperty("thresholds", out var th) && th.ValueKind == JsonValueKind.Object)
{
    if (th.TryGetProperty("high", out var h) && h.TryGetDouble(out var hv)) thresholdHigh = hv;
    if (th.TryGetProperty("low", out var l) && l.TryGetDouble(out var lv)) thresholdLow = lv;
}

// ── Aggregate per-package, per-file, per-mutator ──────────────────────
// File keys are absolute: /home/runner/.../src/Encina.Foo/Bar/Baz.cs
// We extract the package name (project folder under src/) and the relative
// file path within it. This matches the coverage dashboard's per-package view.
const string srcPrefix = "src/";

var packageCounts = new Dictionary<string, PackageCounts>(StringComparer.OrdinalIgnoreCase);
var mutatorCounts = new Dictionary<string, MutatorCounts>(StringComparer.OrdinalIgnoreCase);
var overallCounts = new MutationCounts();

foreach (var fileEntry in files.EnumerateObject())
{
    var fullPath = fileEntry.Name.Replace('\\', '/');

    // Find "src/" marker and extract project folder + relative file
    var srcIdx = fullPath.IndexOf(srcPrefix, StringComparison.OrdinalIgnoreCase);
    string packageName, relativePath;
    if (srcIdx >= 0)
    {
        var afterSrc = fullPath[(srcIdx + srcPrefix.Length)..];
        var slashIdx = afterSrc.IndexOf('/');
        if (slashIdx > 0)
        {
            packageName = afterSrc[..slashIdx];         // e.g. "Encina", "Encina.Messaging"
            relativePath = afterSrc[(slashIdx + 1)..];  // e.g. "Sharding/Migrations/Strategies/Foo.cs"
        }
        else
        {
            packageName = "(root)";
            relativePath = afterSrc;
        }
    }
    else
    {
        packageName = "(unknown)";
        relativePath = Path.GetFileName(fullPath);
    }

    if (!packageCounts.TryGetValue(packageName, out var pc))
    {
        pc = new PackageCounts(packageName);
        packageCounts[packageName] = pc;
    }

    var fileCounts = new MutationCounts();

    if (fileEntry.Value.TryGetProperty("mutants", out var mutants) && mutants.ValueKind == JsonValueKind.Array)
    {
        foreach (var mutant in mutants.EnumerateArray())
        {
            var status = mutant.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
            var mutatorName = mutant.TryGetProperty("mutatorName", out var mn) ? mn.GetString() ?? "Unknown" : "Unknown";

            overallCounts.Register(status);
            pc.Counts.Register(status);
            fileCounts.Register(status);

            if (!mutatorCounts.TryGetValue(mutatorName, out var mtc))
            {
                mtc = new MutatorCounts(mutatorName);
                mutatorCounts[mutatorName] = mtc;
            }
            mtc.Register(status);
        }
    }

    if (fileCounts.TotalConsidered > 0 || fileCounts.CompileErrors > 0)
    {
        pc.Files.Add(new FileEntry(relativePath, fileCounts));
    }
}

// ── Gap analysis: find all packages (src/*) with 0 mutants ───────────
// Enumerate every project folder under src/ — these are the NuGet packages
// that the coverage dashboard also tracks.
var allPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var srcParent = Path.GetDirectoryName(srcRoot) ?? "src";
if (Directory.Exists(srcParent))
{
    foreach (var dir in Directory.GetDirectories(srcParent))
    {
        var name = Path.GetFileName(dir);
        // Skip non-project folders
        if (name is "bin" or "obj" or ".backup") continue;
        // Only include folders that contain a .csproj (real packages)
        if (Directory.GetFiles(dir, "*.csproj").Length > 0)
            allPackages.Add(name);
    }
}
// Ensure packages from report are included (even if src/ doesn't exist locally)
foreach (var p in packageCounts.Keys) allPackages.Add(p);

// ── Build latest.json ─────────────────────────────────────────────────
var overallScore = overallCounts.TotalConsidered > 0
    ? Math.Round(100.0 * overallCounts.Detected / overallCounts.TotalConsidered, 2)
    : 0.0;

var packagesArray = new JsonArray();
foreach (var pkgName in allPackages.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
{
    var pc = packageCounts.GetValueOrDefault(pkgName);
    var counts = pc?.Counts ?? new MutationCounts();
    var pkgScore = counts.TotalConsidered > 0
        ? Math.Round(100.0 * counts.Detected / counts.TotalConsidered, 2)
        : (double?)null;

    var filesArray = new JsonArray();
    if (pc is not null)
    {
        foreach (var f in pc.Files.OrderBy(f => f.Path))
        {
            var fScore = f.Counts.TotalConsidered > 0
                ? Math.Round(100.0 * f.Counts.Detected / f.Counts.TotalConsidered, 2)
                : 0.0;
            filesArray.Add(new JsonObject
            {
                ["path"] = f.Path,
                ["score"] = fScore,
                ["total"] = f.Counts.TotalConsidered,
                ["killed"] = f.Counts.Killed,
                ["survived"] = f.Counts.Survived,
                ["noCoverage"] = f.Counts.NoCoverage,
                ["timeouts"] = f.Counts.Timeouts
            });
        }
    }

    var pkgObj = new JsonObject
    {
        ["name"] = pkgName,
        ["score"] = pkgScore.HasValue ? JsonValue.Create(pkgScore.Value) : null,
        ["total"] = counts.TotalConsidered,
        ["detected"] = counts.Detected,
        ["killed"] = counts.Killed,
        ["survived"] = counts.Survived,
        ["noCoverage"] = counts.NoCoverage,
        ["timeouts"] = counts.Timeouts,
        ["compileErrors"] = counts.CompileErrors,
        ["ignored"] = counts.Ignored,
        ["files"] = filesArray
    };
    packagesArray.Add(pkgObj);
}

var mutatorsArray = new JsonArray();
foreach (var mtc in mutatorCounts.Values.OrderByDescending(m => m.SurvivalRate).ThenBy(m => m.Name))
{
    mutatorsArray.Add(new JsonObject
    {
        ["name"] = mtc.Name,
        ["total"] = mtc.Total,
        ["killed"] = mtc.Killed,
        ["survived"] = mtc.Survived
    });
}

var packagesInScope = packageCounts.Count(p => p.Value.Counts.TotalConsidered > 0);
var filesInScope = packageCounts.Values.Sum(p => p.Files.Count);
var filesTotal = allPackages.Sum(pkgName =>
{
    var dir = Path.Combine(srcParent, pkgName);
    return Directory.Exists(dir) ? Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Length : 0;
});

var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

var latest = new JsonObject
{
    ["timestamp"] = timestamp,
    ["scope"] = scope,
    ["thresholds"] = new JsonObject
    {
        ["high"] = thresholdHigh,
        ["low"] = thresholdLow
    },
    ["overall"] = new JsonObject
    {
        ["score"] = overallScore,
        ["total"] = overallCounts.TotalConsidered,
        ["detected"] = overallCounts.Detected,
        ["killed"] = overallCounts.Killed,
        ["survived"] = overallCounts.Survived,
        ["noCoverage"] = overallCounts.NoCoverage,
        ["timeouts"] = overallCounts.Timeouts,
        ["runtimeErrors"] = overallCounts.RuntimeErrors,
        ["compileErrors"] = overallCounts.CompileErrors,
        ["ignored"] = overallCounts.Ignored
    },
    ["packages"] = packagesArray,
    ["mutators"] = mutatorsArray,
    ["gaps"] = new JsonObject
    {
        ["packagesInScope"] = packagesInScope,
        ["packagesTotal"] = allPackages.Count,
        ["filesInScope"] = filesInScope,
        ["filesTotal"] = filesTotal,
        ["coveragePercent"] = filesTotal > 0 ? Math.Round(100.0 * filesInScope / filesTotal, 1) : 0
    }
};

if (runId > 0) latest["runId"] = runId;

// Write latest.json
var latestDir = Path.GetDirectoryName(latestPath);
if (latestDir is not null) Directory.CreateDirectory(latestDir);
var options = new JsonSerializerOptions { WriteIndented = true };
File.WriteAllText(latestPath, latest.ToJsonString(options));
Console.WriteLine($"latest.json written: score={overallScore}%, {overallCounts.TotalConsidered} mutants considered");

// ── Append to history.json ────────────────────────────────────────────
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

var perPackage = new JsonObject();
foreach (var (name, pc) in packageCounts.Where(p => p.Value.Counts.TotalConsidered > 0))
{
    var ps = Math.Round(100.0 * pc.Counts.Detected / pc.Counts.TotalConsidered, 2);
    perPackage[name] = ps;
}

var histEntry = new JsonObject
{
    ["timestamp"] = timestamp,
    ["score"] = overallScore,
    ["total"] = overallCounts.TotalConsidered,
    ["detected"] = overallCounts.Detected,
    ["killed"] = overallCounts.Killed,
    ["survived"] = overallCounts.Survived,
    ["scope"] = scope,
    ["perPackage"] = perPackage
};
if (runId > 0) histEntry["runId"] = runId;

history.Add(histEntry);
while (history.Count > 100) history.RemoveAt(0);

var histDir = Path.GetDirectoryName(historyPath);
if (histDir is not null) Directory.CreateDirectory(histDir);
File.WriteAllText(historyPath, history.ToJsonString(options));
Console.WriteLine($"history.json updated: {history.Count} entries");

return 0;

// ── Supporting types ──────────────────────────────────────────────────
sealed class MutationCounts
{
    private int _killed, _survived, _noCoverage, _runtimeErrors, _compileErrors, _timeouts, _ignored;

    public int Detected => _killed + _timeouts + _runtimeErrors;
    public int Killed => _killed;
    public int Survived => _survived;
    public int NoCoverage => _noCoverage;
    public int RuntimeErrors => _runtimeErrors;
    public int CompileErrors => _compileErrors;
    public int Timeouts => _timeouts;
    public int Ignored => _ignored;
    public int TotalConsidered { get; private set; }

    public void Register(string status)
    {
        switch (status)
        {
            case "Killed": _killed++; TotalConsidered++; break;
            case "Survived": _survived++; TotalConsidered++; break;
            case "NoCoverage": _noCoverage++; TotalConsidered++; break;
            case "RuntimeError": _runtimeErrors++; TotalConsidered++; break;
            case "Timeout" or "TimedOut": _timeouts++; TotalConsidered++; break;
            case "CompileError": _compileErrors++; break;
            case "Ignored" or "Skipped" or "Pending": _ignored++; break;
            default: TotalConsidered++; break;
        }
    }
}

sealed class PackageCounts(string name)
{
    public string Name { get; } = name;
    public MutationCounts Counts { get; } = new();
    public List<FileEntry> Files { get; } = [];
}

sealed record FileEntry(string Path, MutationCounts Counts);

sealed class MutatorCounts(string name)
{
    public string Name { get; } = name;
    public int Total { get; private set; }
    public int Killed { get; private set; }
    public int Survived { get; private set; }
    public double SurvivalRate => Total > 0 ? (double)Survived / Total : 0;

    public void Register(string status)
    {
        switch (status)
        {
            case "Killed": Killed++; Total++; break;
            case "Survived": Survived++; Total++; break;
            case "NoCoverage": Total++; break;
            case "Timeout" or "TimedOut": Total++; break;
            case "RuntimeError": Total++; break;
        }
    }
}
