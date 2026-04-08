// perf-coverage-scan.cs — Dynamically scan src/ packages and determine benchmark coverage.
//
// Usage:
//   dotnet run .github/scripts/perf-coverage-scan.cs -- \
//     --src-root src \
//     --manifest-dir .github/perf-manifest \
//     --nbomber-root tests/Encina.NBomber \
//     --loadtests-root tests/Encina.LoadTests \
//     --output docs/benchmarks/data/benchmark-coverage.json
//
// Design notes (ADR-025 + issue #947):
// - Scans src/ for all .csproj files → extracts package names
// - Crosses against perf-manifest/*.json → BDN benchmark coverage
// - Crosses against NBomber scenarios + LoadTests → load test coverage
// - Classifies: Covered / Partial / Uncovered / N/A
// - Output is regenerated on EVERY publish run → always reflects current state
// - New packages added to src/ are automatically detected as Uncovered
#pragma warning disable CA1305

using System.Text.Json;
using System.Text.Json.Nodes;

var srcRoot = "src";
var manifestDir = ".github/perf-manifest";
var nbomberRoot = "tests/Encina.NBomber";
var loadTestsRoot = "tests/Encina.LoadTests";
var outputPath = "docs/benchmarks/data/benchmark-coverage.json";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--src-root" && i + 1 < args.Length) srcRoot = args[++i];
    if (args[i] == "--manifest-dir" && i + 1 < args.Length) manifestDir = args[++i];
    if (args[i] == "--nbomber-root" && i + 1 < args.Length) nbomberRoot = args[++i];
    if (args[i] == "--loadtests-root" && i + 1 < args.Length) loadTestsRoot = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputPath = args[++i];
}

// 1. Discover all packages in src/
var packages = new SortedDictionary<string, PackageInfo>(StringComparer.Ordinal);
foreach (var dir in Directory.GetDirectories(srcRoot))
{
    var csprojFiles = Directory.GetFiles(dir, "*.csproj");
    if (csprojFiles.Length == 0) continue;
    var name = Path.GetFileName(dir);
    packages[name] = new PackageInfo { Name = name };
}

Console.WriteLine($"Discovered {packages.Count} packages in {srcRoot}/");

// 2. Check BDN benchmark coverage via manifests
if (Directory.Exists(manifestDir))
{
    foreach (var file in Directory.GetFiles(manifestDir, "*.json"))
    {
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(file));
            var project = json?["project"]?.GetValue<string>() ?? "";
            var totalMethods = json?["totalMethods"]?.GetValue<int>() ?? 0;
            var docRefs = 0;
            var classes = json?["classes"] as JsonArray;
            if (classes is not null)
            {
                foreach (var cls in classes)
                {
                    var methods = cls?["methods"] as JsonArray;
                    if (methods is null) continue;
                    foreach (var m in methods)
                    {
                        var dr = m?["docRef"]?.GetValue<string?>();
                        if (!string.IsNullOrEmpty(dr)) docRefs++;
                    }
                }
            }

            // Map benchmark project → source packages it covers
            // e.g., "Encina.Caching.Benchmarks" covers "Encina.Caching"
            var coveredPkg = project
                .Replace(".Benchmarks", "")
                .Replace("Encina.Benchmarks", "Encina");

            // Special mappings
            if (coveredPkg == "Encina") coveredPkg = "Encina"; // Core benchmarks cover the main package

            if (packages.TryGetValue(coveredPkg, out var pkg))
            {
                pkg.HasBenchmarks = true;
                pkg.BdnMethods = totalMethods;
                pkg.DocRefs = docRefs;
            }
        }
        catch { /* skip malformed */ }
    }
}

// 3. Check NBomber scenario coverage
if (Directory.Exists(nbomberRoot))
{
    var scenarioFiles = Directory.GetFiles(nbomberRoot, "*ScenarioFactory.cs", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(nbomberRoot, "*ScenarioRunner.cs", SearchOption.AllDirectories))
        .ToList();

    // Map scenario areas to packages
    var areaToPackages = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Database"] = ["Encina.ADO.MySQL", "Encina.ADO.PostgreSQL", "Encina.ADO.SqlServer",
                        "Encina.Dapper.MySQL", "Encina.Dapper.PostgreSQL", "Encina.Dapper.SqlServer",
                        "Encina.EntityFrameworkCore", "Encina.MongoDB"],
        ["Caching"] = ["Encina.Caching", "Encina.Caching.Memory", "Encina.Caching.Redis", "Encina.Caching.Hybrid"],
        ["Locking"] = ["Encina.DistributedLock", "Encina.DistributedLock.InMemory",
                       "Encina.DistributedLock.Redis", "Encina.DistributedLock.SqlServer"],
        ["Messaging"] = ["Encina.Messaging"],
        ["Brokers"] = ["Encina.RabbitMQ", "Encina.Kafka", "Encina.NATS", "Encina.MQTT"],
        ["IdGeneration"] = ["Encina.IdGeneration"]
    };

    foreach (var file in scenarioFiles)
    {
        var relDir = Path.GetRelativePath(nbomberRoot, Path.GetDirectoryName(file) ?? "");
        var area = relDir.Split(Path.DirectorySeparatorChar).FirstOrDefault() ?? "";
        // Remove "Scenarios/" prefix if present
        if (area.Equals("Scenarios", StringComparison.OrdinalIgnoreCase))
            area = relDir.Split(Path.DirectorySeparatorChar).ElementAtOrDefault(1) ?? "";

        if (areaToPackages.TryGetValue(area, out var coveredPackages))
        {
            foreach (var pkg in coveredPackages)
            {
                if (packages.TryGetValue(pkg, out var p))
                {
                    p.HasLoadTests = true;
                    p.LoadScenarios++;
                }
            }
        }
    }
}

// 4. Check legacy LoadTests
if (Directory.Exists(loadTestsRoot))
{
    var loadTestFiles = Directory.GetFiles(loadTestsRoot, "*LoadTests.cs", SearchOption.AllDirectories);
    foreach (var file in loadTestFiles)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        // Heuristic: "GDPRComplianceLoadTests" → covers Encina.Compliance.GDPR
        // "AuditEncryptionLoadTests" → covers Encina.Audit.Marten
        // "CdcProcessorLoadTests" → covers Encina.Cdc
        if (fileName.Contains("GDPR", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var compPkg in packages.Values.Where(x => x.Name.StartsWith("Encina.Compliance", StringComparison.Ordinal)))
            { compPkg.HasLoadTests = true; compPkg.LoadScenarios++; }
        }
        if (fileName.Contains("Audit", StringComparison.OrdinalIgnoreCase))
        {
            if (packages.TryGetValue("Encina.Audit.Marten", out var auditPkg)) { auditPkg.HasLoadTests = true; auditPkg.LoadScenarios++; }
            if (packages.TryGetValue("Encina.Security.Audit", out var secAuditPkg)) { secAuditPkg.HasLoadTests = true; secAuditPkg.LoadScenarios++; }
        }
        if (fileName.Contains("Cdc", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var cdcPkg in packages.Values.Where(x => x.Name.StartsWith("Encina.Cdc", StringComparison.Ordinal)))
            { cdcPkg.HasLoadTests = true; cdcPkg.LoadScenarios++; }
        }
    }
}

// 5. Classify each package
var naPatterns = new[] { "Encina.Testing", "Encina.Aspire", "Analyzers",
                         "Encina.Compliance.Attestation", "Encina.Compliance.AIAct",
                         "Encina.Secrets", "Encina.Security.Secrets" };

foreach (var pkg in packages.Values)
{
    var isNa = naPatterns.Any(p => pkg.Name.StartsWith(p, StringComparison.Ordinal)
                                 || pkg.Name.EndsWith(p, StringComparison.Ordinal));

    if (isNa)
        pkg.Status = "N/A";
    else if (pkg.HasBenchmarks && pkg.DocRefs > 0)
        pkg.Status = "Covered";
    else if (pkg.HasBenchmarks || pkg.HasLoadTests)
        pkg.Status = "Partial";
    else
        pkg.Status = "Uncovered";
}

// 6. Build output
int covered = 0, partial = 0, uncovered = 0, na = 0;
var packagesArray = new JsonArray();

foreach (var pkg in packages.Values)
{
    switch (pkg.Status)
    {
        case "Covered": covered++; break;
        case "Partial": partial++; break;
        case "Uncovered": uncovered++; break;
        case "N/A": na++; break;
    }

    packagesArray.Add(new JsonObject
    {
        ["name"] = pkg.Name,
        ["status"] = pkg.Status,
        ["hasBenchmarks"] = pkg.HasBenchmarks,
        ["hasLoadTests"] = pkg.HasLoadTests,
        ["bdnMethods"] = pkg.BdnMethods,
        ["docRefs"] = pkg.DocRefs,
        ["loadScenarios"] = pkg.LoadScenarios
    });
}

var output = new JsonObject
{
    ["timestamp"] = DateTime.UtcNow.ToString("o"),
    ["totalPackages"] = packages.Count,
    ["covered"] = covered,
    ["partial"] = partial,
    ["uncovered"] = uncovered,
    ["notApplicable"] = na,
    ["measurable"] = covered + partial + uncovered,
    ["coveragePercent"] = (covered + partial + uncovered) > 0
        ? Math.Round((covered + partial) * 100.0 / (covered + partial + uncovered), 1)
        : 0,
    ["packages"] = packagesArray
};

var dir2 = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(dir2)) Directory.CreateDirectory(dir2);
File.WriteAllText(outputPath, output.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Benchmark coverage: {covered} covered + {partial} partial / {covered + partial + uncovered} measurable ({output["coveragePercent"]}%)");
Console.WriteLine($"  Uncovered: {uncovered} | N/A: {na} | Total: {packages.Count}");
Console.WriteLine($"  Output: {outputPath}");

sealed class PackageInfo
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Uncovered";
    public bool HasBenchmarks { get; set; }
    public bool HasLoadTests { get; set; }
    public int BdnMethods { get; set; }
    public int DocRefs { get; set; }
    public int LoadScenarios { get; set; }
}
