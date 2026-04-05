// perf-class-matrix.cs — Fan out a project-level matrix into a class-level matrix
//                        by reading the per-project perf-manifest JSON files.
//
// Usage:
//   dotnet run .github/scripts/perf-class-matrix.cs -- \
//     --projects /tmp/changed.json \
//     --manifest-dir .github/perf-manifest \
//     --output /tmp/class-matrix.json
//
// Input (projects, project-level entries from perf-diff-fingerprints.cs or full-matrix.json):
//   [{"name":"Caching","path":"...csproj","project":"Encina.Caching.Benchmarks","os":"ubuntu-latest"}, ...]
//
// Output (classes, one entry per [Benchmark]-annotated class):
//   [
//     {"name":"Caching","path":"...csproj","project":"Encina.Caching.Benchmarks","os":"ubuntu-latest",
//      "class":"CacheOptimizationBenchmarks","filter":"*CacheOptimizationBenchmarks*"},
//     ...
//   ]
//
// Design notes (see ADR-025 §Phase3 and performance-infrastructure-plan.md):
// - Class enumeration is sourced from the committed .github/perf-manifest/<Project>.json
//   files, which are refreshed by generate-perf-manifest.cs. Developers must run the
//   manifest generator after adding/removing benchmark classes so the matrix picks
//   them up.
// - Projects whose manifest lists zero classes (scaffolded stubs: ADO, Dapper) emit
//   ONE fallback entry with class="_all" so the job still runs. The expected
//   'No files were found' warning is identical to the Phase 2 behavior.
// - Fan-out is bounded by the matrix row count (GitHub Actions limit: 256). Current
//   catalog has ~150 classes across 17 populated projects, well within limits.

#pragma warning disable CA1305

using System.Text.Json;
using System.Text.Json.Nodes;

var projectsPath = "";
var manifestDir = ".github/perf-manifest";
var outputPath = "/tmp/class-matrix.json";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--projects" && i + 1 < args.Length) projectsPath = args[++i];
    if (args[i] == "--manifest-dir" && i + 1 < args.Length) manifestDir = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputPath = args[++i];
}

if (!File.Exists(projectsPath))
{
    Console.Error.WriteLine($"Projects file not found: {projectsPath}");
    Environment.Exit(2);
}

var projects = JsonNode.Parse(File.ReadAllText(projectsPath)) as JsonArray ?? new JsonArray();
var result = new JsonArray();

Console.WriteLine($"Fanning out {projects.Count} project(s) to class-level matrix entries:");
Console.WriteLine();

foreach (var entry in projects)
{
    if (entry is not JsonObject eo) continue;
    var matrixName = eo["name"]?.GetValue<string>() ?? "";
    var projectKey = eo["project"]?.GetValue<string>() ?? "";
    var manifestPath = Path.Combine(manifestDir, $"{projectKey}.json");

    var classInfos = new List<(string SimpleName, string FullName)>();
    if (File.Exists(manifestPath))
    {
        try
        {
            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
            if (manifest?["classes"] is JsonArray classesArray)
            {
                foreach (var c in classesArray)
                {
                    var simpleName = c?["name"]?.GetValue<string>() ?? "";
                    var fullName = c?["fullName"]?.GetValue<string>() ?? simpleName;
                    if (!string.IsNullOrEmpty(simpleName))
                        classInfos.Add((simpleName, fullName));
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  {matrixName}: failed to parse manifest {manifestPath}: {ex.Message}");
        }
    }

    if (classInfos.Count == 0)
    {
        // Scaffolded/empty project — emit one fallback entry so the job still runs.
        var fallback = (JsonObject)eo.DeepClone();
        fallback["class"] = "_all";
        fallback["filter"] = "*";
        result.Add(fallback);
        Console.WriteLine($"  {matrixName,-25} 0 classes → 1 fallback entry (_all)");
    }
    else
    {
        foreach (var (simpleName, fullName) in classInfos)
        {
            var newEntry = (JsonObject)eo.DeepClone();
            // Use the short simple name for the matrix job display name.
            newEntry["class"] = simpleName;
            // Use the fully-qualified name for BDN --filter so classes with
            // the same simple name in different namespaces don't collide.
            // BDN filter matches against FullName (Namespace.Class.Method).
            newEntry["filter"] = $"*{fullName}.*";
            // Sanitize artifact suffix to avoid upload-artifact naming collisions
            // when two classes share the same simple name (e.g. ScatterGatherBenchmarks
            // in root namespace and in Sharding namespace).
            var artifactSuffix = fullName.Replace(".", "-");
            newEntry["artifactSuffix"] = artifactSuffix;
            result.Add(newEntry);
        }
        Console.WriteLine($"  {matrixName,-25} {classInfos.Count,3} classes");
    }
}

File.WriteAllText(outputPath, result.ToJsonString());
Console.WriteLine();
Console.WriteLine($"Expanded to {result.Count} class-level matrix entries → {outputPath}");

// Warn if we're getting close to the GitHub Actions matrix limit
if (result.Count > 200)
{
    Console.WriteLine($"WARNING: Matrix has {result.Count} entries, approaching GitHub Actions limit of 256.");
}
if (result.Count > 256)
{
    Console.Error.WriteLine($"ERROR: Matrix has {result.Count} entries, exceeds GitHub Actions limit of 256.");
    Environment.Exit(2);
}
