// perf-fingerprint.cs — Compute project-graph source fingerprints for benchmark projects.
//
// Usage:
//   dotnet run .github/scripts/perf-fingerprint.cs -- \
//     --projects-dir tests/Encina.BenchmarkTests \
//     --repo-root . \
//     --output artifacts/perf-output/fingerprints.json
//
// Design notes (see ADR-025 §7 + performance-measurement-methodology.md):
// - For each benchmark .csproj, enumerate the transitive ProjectReference closure
//   by parsing <ProjectReference Include="..."/> directly (no MSBuild needed).
// - Hash all .cs files reachable through the closure (excluding obj/, bin/),
//   sorted lexicographically by relative path.
// - Include Directory.Packages.props as a secondary input so NuGet version
//   changes also invalidate the fingerprint.
// - Output JSON: { "<project-name>": "<sha256-hex>", ... }
//
// Comparison semantics: two fingerprints match iff the same source files
// exist with the same contents AND Directory.Packages.props is identical.
#pragma warning disable CA1305

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var projectsDir = "";
var repoRoot = ".";
var outputPath = "fingerprints.json";
var explicitProjects = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--projects-dir" && i + 1 < args.Length) projectsDir = args[++i];
    if (args[i] == "--repo-root" && i + 1 < args.Length) repoRoot = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputPath = args[++i];
    if (args[i] == "--project" && i + 1 < args.Length) explicitProjects.Add(args[++i]);
}

repoRoot = Path.GetFullPath(repoRoot);

var csprojPaths = new List<string>();
if (explicitProjects.Count > 0)
{
    csprojPaths.AddRange(explicitProjects.Select(Path.GetFullPath));
}
else if (!string.IsNullOrEmpty(projectsDir))
{
    csprojPaths.AddRange(Directory.GetDirectories(Path.GetFullPath(projectsDir))
        .SelectMany(d => Directory.GetFiles(d, "*.csproj"))
        .OrderBy(p => p, StringComparer.Ordinal));
}
else
{
    Console.Error.WriteLine("Specify --projects-dir or one or more --project paths.");
    Environment.Exit(2);
}

if (csprojPaths.Count == 0)
{
    Console.Error.WriteLine("No .csproj files discovered.");
    Environment.Exit(2);
}

// Hash Directory.Packages.props (central package management file) once — shared across all projects.
var dppPath = Path.Combine(repoRoot, "Directory.Packages.props");
var dppHash = File.Exists(dppPath) ? HashFileHex(dppPath) : "";
Console.WriteLine($"Directory.Packages.props hash: {(string.IsNullOrEmpty(dppHash) ? "(missing)" : dppHash[..12] + "...")}");

var result = new SortedDictionary<string, string>(StringComparer.Ordinal);

foreach (var csproj in csprojPaths)
{
    var projectName = Path.GetFileNameWithoutExtension(csproj);
    try
    {
        var hash = ComputeProjectFingerprint(csproj, repoRoot, dppHash);
        result[projectName] = hash;
        Console.WriteLine($"  {projectName,-50} {hash[..12]}...");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  {projectName}: FAILED — {ex.Message}");
        result[projectName] = ""; // empty hash signals "rerun this project"
    }
}

var dir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

var outputJson = new JsonObject();
foreach (var (key, value) in result) outputJson[key] = value;
File.WriteAllText(outputPath, outputJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Fingerprints written to {outputPath} ({result.Count} entries)");


static string ComputeProjectFingerprint(string csprojPath, string repoRoot, string dppHash)
{
    var closure = ResolveTransitiveProjects(csprojPath);

    // Collect all .cs files across the closure, sorted by repo-relative path.
    var files = new List<string>();
    foreach (var proj in closure)
    {
        var projDir = Path.GetDirectoryName(proj);
        if (projDir is null || !Directory.Exists(projDir)) continue;

        files.AddRange(Directory.GetFiles(projDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsInObjOrBin(f)));
    }
    files.Sort(StringComparer.Ordinal);

    using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    // Domain separator + DPP hash first so it participates in the hash.
    hasher.AppendData(Encoding.UTF8.GetBytes("ENCINA_PERF_FINGERPRINT_V1\n"));
    hasher.AppendData(Encoding.UTF8.GetBytes($"dpp={dppHash}\n"));

    foreach (var file in files)
    {
        var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
        hasher.AppendData(Encoding.UTF8.GetBytes($"\nfile:{rel}\n"));
        hasher.AppendData(File.ReadAllBytes(file));
    }

    return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
}

static HashSet<string> ResolveTransitiveProjects(string rootCsproj)
{
    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var stack = new Stack<string>();
    stack.Push(Path.GetFullPath(rootCsproj));

    var refRegex = new Regex(
        @"<ProjectReference\s+[^>]*Include\s*=\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    while (stack.Count > 0)
    {
        var current = stack.Pop();
        if (!visited.Add(current)) continue;
        if (!File.Exists(current)) continue;

        string content;
        try { content = File.ReadAllText(current); }
        catch { continue; }

        foreach (Match m in refRegex.Matches(content))
        {
            var relRef = m.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
            var baseDir = Path.GetDirectoryName(current) ?? "";
            var absRef = Path.GetFullPath(Path.Combine(baseDir, relRef));
            if (!visited.Contains(absRef)) stack.Push(absRef);
        }
    }

    return visited;
}

static bool IsInObjOrBin(string path)
{
    var norm = path.Replace('\\', '/');
    return norm.Contains("/obj/", StringComparison.Ordinal)
        || norm.Contains("/bin/", StringComparison.Ordinal);
}

static string HashFileHex(string path)
{
    using var sha = SHA256.Create();
    using var fs = File.OpenRead(path);
    return Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
}
