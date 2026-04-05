// generate-perf-manifest.cs — Scans tests/Encina.BenchmarkTests/** and emits
//                             one JSON manifest per project under .github/perf-manifest/.
//
// Usage:
//   dotnet run .github/scripts/generate-perf-manifest.cs -- \
//     --src tests/Encina.BenchmarkTests --output .github/perf-manifest
//
// Design notes (see ADR-025 + performance-measurement-methodology.md):
// - Discovers classes annotated with BenchmarkDotNet `[MemoryDiagnoser]`, `[SimpleJob]`,
//   or containing `[Benchmark]` methods. Extraction is regex-based (no Roslyn) to keep
//   the script dependency-free.
// - Preserves existing overrides and targets if the manifest already exists (mirrors
//   generate-coverage-manifest.cs behavior).
// - DocRef extraction: looks for `[BenchmarkCategory("DocRef:<id>")]` attributes.
#pragma warning disable CA1305 // IFormatProvider not relevant for standalone scripts
#pragma warning disable CA1861

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var srcRoot = "tests/Encina.BenchmarkTests";
var outputDir = ".github/perf-manifest";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--src" && i + 1 < args.Length) srcRoot = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
}

if (!Directory.Exists(srcRoot))
{
    Console.Error.WriteLine($"Source directory '{srcRoot}' not found.");
    Environment.Exit(2);
}

Directory.CreateDirectory(outputDir);

// Each immediate subdirectory of srcRoot that contains a *.csproj is a project.
var projectDirs = Directory.GetDirectories(srcRoot)
    .Where(d => Directory.GetFiles(d, "*.csproj").Length > 0)
    .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine($"Discovered {projectDirs.Count} benchmark projects under {srcRoot}");

var classAttrRegex = new Regex(@"\[(?:MemoryDiagnoser|SimpleJob|ShortRunJob|MediumRunJob)\b", RegexOptions.Compiled);
var classDeclRegex = new Regex(@"public\s+(?:sealed\s+|partial\s+)?class\s+(\w+)", RegexOptions.Compiled);
var benchmarkAttrRegex = new Regex(@"\[Benchmark(?:\([^)]*\))?\]", RegexOptions.Compiled);
var methodRegex = new Regex(@"public\s+(?:async\s+)?(?:\w+(?:<[^>]+>)?\s+)+(\w+)\s*\(", RegexOptions.Compiled);
var docRefRegex = new Regex(@"\[BenchmarkCategory\(\s*""DocRef:([^""]+)""\s*\)\]", RegexOptions.Compiled);

foreach (var projDir in projectDirs)
{
    var projName = Path.GetFileName(projDir);
    var manifestPath = Path.Combine(outputDir, $"{projName}.json");

    // Load existing manifest to preserve overrides + targets
    JsonObject? existing = null;
    if (File.Exists(manifestPath))
    {
        try
        {
            existing = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject;
        }
        catch { /* regenerate from scratch */ }
    }

    var benchmarkClasses = new List<BenchmarkClassInfo>();
    var csFiles = Directory.GetFiles(projDir, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        .Where(f => !Path.GetFileName(f).Equals("Program.cs", StringComparison.OrdinalIgnoreCase))
        .Where(f => !Path.GetFileName(f).Equals("GlobalSuppressions.cs", StringComparison.OrdinalIgnoreCase));

    var nsRegex = new Regex(@"^\s*namespace\s+([\w.]+)\s*[;{]", RegexOptions.Multiline);

    foreach (var file in csFiles)
    {
        try
        {
            var content = File.ReadAllText(file);
            if (!benchmarkAttrRegex.IsMatch(content)) continue;

            var classMatch = classDeclRegex.Match(content);
            if (!classMatch.Success) continue;
            var className = classMatch.Groups[1].Value;

            // Extract namespace for FQN-based filters (Phase 3.1 class-level fan-out).
            var nsMatch = nsRegex.Match(content);
            var ns = nsMatch.Success ? nsMatch.Groups[1].Value : "";

            var hasMemoryDiagnoser = content.Contains("[MemoryDiagnoser]", StringComparison.Ordinal);

            // Extract benchmark methods with their DocRefs (regex-based; good enough for catalog)
            var methods = new List<BenchmarkMethodInfo>();
            var lines = content.Split('\n');
            var currentDocRef = (string?)null;
            var expectingMethod = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                var docRefMatch = docRefRegex.Match(line);
                if (docRefMatch.Success) currentDocRef = docRefMatch.Groups[1].Value;

                if (benchmarkAttrRegex.IsMatch(line))
                {
                    expectingMethod = true;
                    continue;
                }

                if (expectingMethod)
                {
                    var m = methodRegex.Match(line);
                    if (m.Success)
                    {
                        methods.Add(new BenchmarkMethodInfo(m.Groups[1].Value, currentDocRef));
                        currentDocRef = null;
                        expectingMethod = false;
                    }
                }
            }

            if (methods.Count > 0)
            {
                var relativePath = Path.GetRelativePath(projDir, file).Replace('\\', '/');
                benchmarkClasses.Add(new BenchmarkClassInfo
                {
                    Name = className,
                    Namespace = ns,
                    FullName = string.IsNullOrEmpty(ns) ? className : $"{ns}.{className}",
                    File = relativePath,
                    HasMemoryDiagnoser = hasMemoryDiagnoser,
                    Methods = methods
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to parse '{file}': {ex.Message}");
        }
    }

    // Build manifest JSON
    var manifest = new JsonObject
    {
        ["project"] = projName,
        ["generated"] = DateTime.UtcNow.ToString("o"),
        ["totalClasses"] = benchmarkClasses.Count,
        ["totalMethods"] = benchmarkClasses.Sum(c => c.Methods.Count)
    };

    // Preserve targets if they exist, otherwise emit an empty stub for manual editing
    if (existing is { } ex0 && ex0["targets"] is JsonObject prevTargets)
    {
        manifest["targets"] = prevTargets.DeepClone();
    }
    else
    {
        manifest["targets"] = new JsonObject
        {
            ["$comment"] = "Populate with per-method target values once Phase 1 produces baseline data. Example: { \"SendCommand.mean_ns_max\": 2000, \"SendCommand.allocated_bytes_max\": 4096 }"
        };
    }

    // Preserve stability overrides (e.g. marking known-unstable benchmarks)
    if (existing is { } ex1 && ex1["stabilityOverrides"] is JsonObject prevStab)
    {
        manifest["stabilityOverrides"] = prevStab.DeepClone();
    }
    else
    {
        manifest["stabilityOverrides"] = new JsonObject();
    }

    var classesArray = new JsonArray();
    foreach (var c in benchmarkClasses.OrderBy(c => c.Name, StringComparer.Ordinal))
    {
        var methodsArray = new JsonArray();
        foreach (var m in c.Methods.OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            methodsArray.Add(new JsonObject
            {
                ["name"] = m.Name,
                ["docRef"] = m.DocRef
            });
        }
        classesArray.Add(new JsonObject
        {
            ["name"] = c.Name,
            ["namespace"] = c.Namespace,
            ["fullName"] = c.FullName,
            ["file"] = c.File,
            ["hasMemoryDiagnoser"] = c.HasMemoryDiagnoser,
            ["methods"] = methodsArray
        });
    }
    manifest["classes"] = classesArray;

    File.WriteAllText(manifestPath, manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"  {projName}: {benchmarkClasses.Count} classes, {benchmarkClasses.Sum(c => c.Methods.Count)} methods");
}

Console.WriteLine($"Manifests written to {outputDir}");

sealed class BenchmarkClassInfo
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string FullName { get; set; } = "";
    public string File { get; set; } = "";
    public bool HasMemoryDiagnoser { get; set; }
    public List<BenchmarkMethodInfo> Methods { get; set; } = new();
}

sealed record BenchmarkMethodInfo(string Name, string? DocRef);
