// coverage-report.cs — Encina Weighted Coverage Analysis
#pragma warning disable CA1305, CA1310, CA1852 // Globalization/sealed warnings not relevant for standalone scripts
// Reads Cobertura XML reports from multiple test types and generates
// a weighted coverage report where only applicable test types count per package.
//
// Usage: dotnet run .github/scripts/coverage-report.cs -- [--output <dir>] [--input <dir>]
//
// Requires: .NET 10+ (C# 14 file-based app)

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

// ─── Configuration ───────────────────────────────────────────────────────────

var outputDir = "artifacts/coverage";
var inputDir = "artifacts/test-results";
var weightsFile = ".github/scripts/coverage-weights.json";
var manifestDir = ".github/coverage-manifest";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
    if (args[i] == "--input" && i + 1 < args.Length) inputDir = args[++i];
    if (args[i] == "--weights" && i + 1 < args.Length) weightsFile = args[++i];
    if (args[i] == "--manifest" && i + 1 < args.Length) manifestDir = args[++i];
}

// ─── Load package categories from JSON ──────────────────────────────────────

// Try multiple locations for the weights file
if (!File.Exists(weightsFile))
{
    // Try relative to the script file location
    var scriptDir = Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".";
    var altPath = Path.Combine(scriptDir, "..", "..", ".github", "scripts", "coverage-weights.json");
    if (File.Exists(altPath)) weightsFile = altPath;

    // Try relative to git root (walk up from current dir)
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, ".github", "scripts", "coverage-weights.json");
        if (File.Exists(candidate)) { weightsFile = candidate; break; }
        dir = dir.Parent;
    }
}

CategoryDef[] categories;

if (File.Exists(weightsFile))
{
    Console.WriteLine($"Loading weights from: {weightsFile}");
    var weightsJson = File.ReadAllText(weightsFile);
    var jsonOpts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };
    var config = JsonSerializer.Deserialize<WeightsConfig>(weightsJson, jsonOpts)!;
    categories = config.Categories.Select(c =>
    {
        var testType = TestType.None;
        foreach (var t in c.ApplicableTests)
        {
            testType |= t.ToLowerInvariant() switch
            {
                "unit" => TestType.Unit,
                "guard" => TestType.Guard,
                "contract" => TestType.Contract,
                "property" => TestType.Property,
                "integration" => TestType.Integration,
                _ => TestType.None
            };
        }
        return new CategoryDef(c.Name, testType, c.Target, c.Patterns);
    }).ToArray();
    Console.WriteLine($"Loaded {categories.Length} categories from JSON");
}
else
{
    Console.WriteLine($"WARNING: Weights file '{weightsFile}' not found. Using built-in defaults.");
    categories = [
        new("Full", TestType.All, 85.0, ["Encina", "Encina.Messaging", "Encina.DomainModeling",
            "Encina.Caching", "Encina.Security", "Encina.Security.*", "Encina.Compliance.*",
            "Encina.IdGeneration", "Encina.GuardClauses",
            "Encina.Messaging.Encryption", "Encina.Messaging.Encryption.*"]),
        new("Logic", TestType.Unit | TestType.Guard | TestType.Property, 80.0,
            ["Encina.OpenTelemetry", "Encina.Tenancy", "Encina.Tenancy.AspNetCore",
             "Encina.Polly", "Encina.Extensions.Resilience", "Encina.Hangfire", "Encina.Quartz"]),
        new("Provider", TestType.Unit | TestType.Guard | TestType.Integration, 50.0,
            ["Encina.ADO.*", "Encina.Dapper.*", "Encina.EntityFrameworkCore",
             "Encina.MongoDB", "Encina.Marten", "Encina.Marten.GDPR", "Encina.Audit.Marten"]),
        new("Transport", TestType.Unit | TestType.Guard, 75.0,
            ["Encina.Kafka", "Encina.RabbitMQ", "Encina.NATS", "Encina.MQTT",
             "Encina.Redis.PubSub", "Encina.AmazonSQS", "Encina.AzureServiceBus", "Encina.InMemory"]),
        new("Cloud", TestType.Unit | TestType.Guard, 60.0,
            ["Encina.AwsLambda", "Encina.AzureFunctions", "Encina.AspNetCore",
             "Encina.Aspire.Testing", "Encina.SignalR", "Encina.gRPC", "Encina.GraphQL", "Encina.Refit"]),
        new("CDC", TestType.Unit | TestType.Guard | TestType.Integration, 60.0,
            ["Encina.Cdc", "Encina.Cdc.*"]),
        new("Validation", TestType.Unit | TestType.Guard | TestType.Contract, 80.0,
            ["Encina.FluentValidation", "Encina.DataAnnotations", "Encina.MiniValidator"]),
        new("DistributedLock", TestType.Unit | TestType.Guard | TestType.Integration, 70.0,
            ["Encina.DistributedLock", "Encina.DistributedLock.*"]),
        new("Excluded", TestType.None, 0.0,
            ["Encina.Testing", "Encina.Testing.*", "Encina.TestInfrastructure", "Encina.Cli",
             "Encina.Security.ABAC.Analyzers"]),
    ];
}

// ─── Load per-file manifest ─────────────────────────────────────────────────

// manifest[package][relFilePath] = applicable TestType flags
var manifest = new Dictionary<string, Dictionary<string, TestType>>(StringComparer.OrdinalIgnoreCase);

// Try to find manifest directory
if (!Directory.Exists(manifestDir))
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, ".github", "coverage-manifest");
        if (Directory.Exists(candidate)) { manifestDir = candidate; break; }
        dir = dir.Parent;
    }
}

if (Directory.Exists(manifestDir))
{
    var manifestJsonOpts = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    foreach (var mFile in Directory.GetFiles(manifestDir, "*.json")
        .Where(f => Path.GetFileName(f) != "defaults.json"))
    {
        try
        {
            var mJson = JsonSerializer.Deserialize<ManifestFile>(File.ReadAllText(mFile), manifestJsonOpts);
            if (mJson?.Package is null || mJson.Files is null) continue;

            var pkgFiles = new Dictionary<string, TestType>(StringComparer.OrdinalIgnoreCase);
            foreach (var (filePath, entry) in mJson.Files)
            {
                // Override takes precedence over defaultTests
                var testNames = entry.Override ?? entry.DefaultTests ?? [];
                var testType = TestType.None;
                foreach (var t in testNames)
                {
                    testType |= t.ToLowerInvariant() switch
                    {
                        "unit" => TestType.Unit,
                        "guard" => TestType.Guard,
                        "contract" => TestType.Contract,
                        "property" => TestType.Property,
                        "integration" => TestType.Integration,
                        _ => TestType.None
                    };
                }
                pkgFiles[filePath] = testType;
            }
            manifest[mJson.Package] = pkgFiles;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  WARNING: Failed to parse manifest {Path.GetFileName(mFile)}: {ex.Message}");
        }
    }
    Console.WriteLine($"Loaded {manifest.Count} package manifests with {manifest.Values.Sum(p => p.Count)} file entries");
}
else
{
    Console.WriteLine($"WARNING: Manifest directory '{manifestDir}' not found. Using category-level weights only.");
}

// ─── Per-file test type lookup ──────────────────────────────────────────────

TestType GetFileApplicableTests(string packageName, string fileRelPath)
{
    // 1. Try manifest (per-file granularity)
    if (manifest.TryGetValue(packageName, out var pkgManifest))
    {
        if (pkgManifest.TryGetValue(fileRelPath, out var fileTests))
            return fileTests;

        // Try with just the filename (manifest might use filename only)
        var fileName = Path.GetFileName(fileRelPath);
        if (pkgManifest.TryGetValue(fileName, out var fnTests))
            return fnTests;
    }

    // 2. Fallback to category-level weights
    var (_, applicableTests, _) = GetCategory(packageName);
    return applicableTests;
}

// ─── Directory → TestType mapping ────────────────────────────────────────────

TestType ClassifyDirectory(string dirName)
{
    // Handle both direct names (UnitTests-Messaging) and artifact names (test-results-UnitTests-Messaging)
    if (dirName.Contains("UnitTests", StringComparison.OrdinalIgnoreCase)) return TestType.Unit;
    if (dirName.Contains("GuardTests", StringComparison.OrdinalIgnoreCase)) return TestType.Guard;
    if (dirName.Contains("ContractTests", StringComparison.OrdinalIgnoreCase)) return TestType.Contract;
    if (dirName.Contains("PropertyTests", StringComparison.OrdinalIgnoreCase)) return TestType.Property;
    if (dirName.Contains("IntegrationTests", StringComparison.OrdinalIgnoreCase)) return TestType.Integration;
    if (dirName.Contains("EFCore-", StringComparison.OrdinalIgnoreCase)) return TestType.Integration;
    return TestType.None;
}

// ─── Package → Category matching ─────────────────────────────────────────────

(string CategoryName, TestType ApplicableTests, double Target) GetCategory(string packageName)
{
    foreach (var cat in categories)
    {
        foreach (var pattern in cat.Patterns)
        {
            if (pattern.EndsWith(".*"))
            {
                var prefix = pattern[..^2];
                if (packageName == prefix || packageName.StartsWith(prefix + ".", StringComparison.Ordinal))
                    return (cat.Name, cat.ApplicableTests, cat.Target);
            }
            else if (packageName == pattern)
            {
                return (cat.Name, cat.ApplicableTests, cat.Target);
            }
        }
    }
    return ("Uncategorized", TestType.Unit | TestType.Guard, 70.0);
}

// ─── Cobertura XML parsing ───────────────────────────────────────────────────

// line coverage: file → lineNumber → hits (max across applicable reports)
var coverageByFlag = new Dictionary<TestType, Dictionary<string, Dictionary<int, int>>>();

Console.WriteLine($"Scanning {inputDir} for coverage reports...");

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"WARNING: Input directory '{inputDir}' not found. Generating empty report.");
    Directory.CreateDirectory(outputDir);
    File.WriteAllText(Path.Combine(outputDir, "encina-coverage-report.md"),
        "# Encina Coverage Report\n\nNo coverage data available. Run tests with `--collect \"XPlat Code Coverage\"` first.\n");
    return;
}

var xmlFiles = Directory.GetFiles(inputDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
Console.WriteLine($"Found {xmlFiles.Length} coverage report(s)");

foreach (var xmlFile in xmlFiles)
{
    // Determine flag from directory name
    var relPath = Path.GetRelativePath(inputDir, xmlFile);
    var topDir = relPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
    var flag = ClassifyDirectory(topDir);

    if (flag == TestType.None)
    {
        Console.WriteLine($"  SKIP: {relPath} (unknown test type: {topDir})");
        continue;
    }

    Console.WriteLine($"  {flag,-15} ← {relPath}");

    if (!coverageByFlag.ContainsKey(flag))
        coverageByFlag[flag] = new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);

    var flagData = coverageByFlag[flag];

    try
    {
        var doc = XDocument.Load(xmlFile);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        // Check if <source> ends with /src/ — filenames may be relative to it
        var sourceEl = doc.Descendants(ns + "source").FirstOrDefault()?.Value?.Replace('\\', '/') ?? "";
        var sourceEndsSrc = sourceEl.EndsWith("/src/", StringComparison.Ordinal)
                         || sourceEl.EndsWith("/src", StringComparison.Ordinal);

        foreach (var cls in doc.Descendants(ns + "class"))
        {
            var filename = cls.Attribute("filename")?.Value;
            if (filename is null) continue;

            // Normalize path separators
            filename = filename.Replace('\\', '/');

            // If filename is relative (no src/) and source ends with /src/, prepend src/
            if (sourceEndsSrc && !filename.Contains("/src/") && !filename.StartsWith("src/"))
            {
                filename = "src/" + filename;
            }

            // Only count src/ files
            if (!filename.Contains("/src/") && !filename.StartsWith("src/")) continue;

            // Extract relative path from src/
            var srcIdx = filename.IndexOf("/src/", StringComparison.Ordinal);
            if (srcIdx < 0) srcIdx = filename.IndexOf("src/", StringComparison.Ordinal) - 1;
            var relFile = filename[(srcIdx + 1)..]; // "src/Encina.Foo/Bar.cs"

            if (!flagData.ContainsKey(relFile))
                flagData[relFile] = new Dictionary<int, int>();

            var fileLines = flagData[relFile];

            foreach (var line in cls.Descendants(ns + "line"))
            {
                var num = int.Parse(line.Attribute("number")?.Value ?? "0", CultureInfo.InvariantCulture);
                var hits = int.Parse(line.Attribute("hits")?.Value ?? "0", CultureInfo.InvariantCulture);
                if (num <= 0) continue;

                if (fileLines.TryGetValue(num, out var existing))
                    fileLines[num] = Math.Max(existing, hits);
                else
                    fileLines[num] = hits;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR parsing {relPath}: {ex.Message}");
    }
}

Console.WriteLine($"\nLoaded data from {coverageByFlag.Count} test type(s): {string.Join(", ", coverageByFlag.Keys)}");

// ─── Merge coverage per file using only applicable flags ─────────────────────

// Collect all unique files across all flags
var allFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var flagData in coverageByFlag.Values)
    foreach (var file in flagData.Keys)
        allFiles.Add(file);

Console.WriteLine($"Total source files with coverage data: {allFiles.Count}");

// Process each file
var fileResults = new List<FileCoverage>();

foreach (var file in allFiles)
{
    // Extract package name from path: src/Encina.Foo/... → Encina.Foo
    var parts = file.Split('/');
    if (parts.Length < 3 || parts[0] != "src") continue;
    var packageName = parts[1];

    // Get per-file applicable tests from manifest (falls back to category)
    var fileRelPath = string.Join("/", parts[2..]); // path within package
    var applicableTests = GetFileApplicableTests(packageName, fileRelPath);
    var (catName, _, target) = GetCategory(packageName);
    if (applicableTests == TestType.None) continue; // Excluded

    // AND-weighted merge: a line's coverage = (flags covering it) / (total applicable flags)
    var allLineNumbers = new HashSet<int>();
    var lineHitsByFlag = new Dictionary<TestType, Dictionary<int, int>>();
    var perFlag = new Dictionary<TestType, (int Total, int Covered)>();

    foreach (var (flag, flagData) in coverageByFlag)
    {
        if (!applicableTests.HasFlag(flag)) continue; // Skip non-applicable per THIS FILE
        if (!flagData.TryGetValue(file, out var fileLines)) continue;

        int flagTotal = fileLines.Count;
        int flagCovered = fileLines.Values.Count(h => h > 0);
        perFlag[flag] = (flagTotal, flagCovered);
        lineHitsByFlag[flag] = fileLines;

        foreach (var lineNum in fileLines.Keys)
            allLineNumbers.Add(lineNum);
    }

    if (allLineNumbers.Count == 0) continue;

    // Count how many test types the manifest requires for this file
    int applicableFlagCount = CountFlags(applicableTests);

    // Calculate fractional coverage per line: covered_flags / total_applicable_flags
    double totalFractionalCoverage = 0;
    foreach (var lineNum in allLineNumbers)
    {
        int flagsCoveringThisLine = 0;
        foreach (var (flag, lines) in lineHitsByFlag)
        {
            if (lines.TryGetValue(lineNum, out var hits) && hits > 0)
                flagsCoveringThisLine++;
        }
        totalFractionalCoverage += (double)flagsCoveringThisLine / applicableFlagCount;
    }

    int totalLines = allLineNumbers.Count;
    double coveredEquivalent = totalFractionalCoverage;
    double pct = totalLines > 0 ? Math.Round(coveredEquivalent * 100.0 / totalLines, 2) : 0;

    fileResults.Add(new FileCoverage(file, packageName, catName, totalLines, coveredEquivalent, pct, perFlag));
}

// ─── Aggregate by package ────────────────────────────────────────────────────

var packageResults = fileResults
    .GroupBy(f => f.Package)
    .Select(g =>
    {
        var first = g.First();
        var (catName, applicableTests, target) = GetCategory(first.Package);
        int totalLines = g.Sum(f => f.TotalLines);
        double coveredEquivalent = g.Sum(f => f.CoveredEquivalent);
        double pct = totalLines > 0 ? Math.Round(coveredEquivalent * 100.0 / totalLines, 2) : 0;

        // Aggregate per-flag
        var perFlag = new Dictionary<TestType, (int Total, int Covered)>();
        foreach (var file in g)
        {
            foreach (var (flag, (t, c)) in file.PerFlag)
            {
                if (perFlag.TryGetValue(flag, out var existing))
                    perFlag[flag] = (existing.Total + t, existing.Covered + c);
                else
                    perFlag[flag] = (t, c);
            }
        }

        return new PackageCoverage(first.Package, catName, applicableTests, target,
            totalLines, coveredEquivalent, pct, perFlag, g.OrderBy(f => f.Percentage).ToList());
    })
    .OrderBy(p => p.Percentage)
    .ToList();

// ─── Aggregate by category ───────────────────────────────────────────────────

var categoryResults = packageResults
    .GroupBy(p => p.Category)
    .Select(g =>
    {
        var cat = categories.FirstOrDefault(c => c.Name == g.Key);
        int totalLines = g.Sum(p => p.TotalLines);
        double coveredEquivalent = g.Sum(p => p.CoveredEquivalent);
        double pct = totalLines > 0 ? Math.Round(coveredEquivalent * 100.0 / totalLines, 2) : 0;
        return new CategoryCoverage(g.Key, cat?.ApplicableTests ?? TestType.None, cat?.Target ?? 0,
            g.Count(), totalLines, coveredEquivalent, pct);
    })
    .OrderByDescending(c => c.TotalLines)
    .ToList();

// ─── Overall ─────────────────────────────────────────────────────────────────

int overallTotal = packageResults.Sum(p => p.TotalLines);
double overallCovered = packageResults.Sum(p => p.CoveredEquivalent);
double overallPct = overallTotal > 0 ? Math.Round(overallCovered * 100.0 / overallTotal, 2) : 0;

// Aggregate per-flag overall coverage
var overallPerFlag = new Dictionary<TestType, (int Total, int Covered)>();
foreach (var pkg in packageResults)
{
    foreach (var (flag, (t, c)) in pkg.PerFlag)
    {
        if (overallPerFlag.TryGetValue(flag, out var existing))
            overallPerFlag[flag] = (existing.Total + t, existing.Covered + c);
        else
            overallPerFlag[flag] = (t, c);
    }
}

// ─── Console summary ─────────────────────────────────────────────────────────

Console.WriteLine($"\n{'═',0}══════════════════════════════════════════════════════════════");
Console.WriteLine($"  ENCINA WEIGHTED COVERAGE: {overallPct}% ({overallCovered:N1} / {overallTotal:N0} weighted lines)");
Console.WriteLine($"{'═',0}══════════════════════════════════════════════════════════════\n");

foreach (var cat in categoryResults)
{
    var testsStr = FormatTestTypes(cat.ApplicableTests);
    var status = cat.Percentage >= cat.Target ? "✅" : cat.Percentage >= cat.Target * 0.8 ? "🟡" : "🔴";
    Console.WriteLine($"  {status} {cat.Name,-20} {cat.Percentage,6:F1}% / {cat.Target,4:F0}%  ({cat.PackageCount} pkgs, {testsStr})");
}

Console.WriteLine($"\n  Packages: {packageResults.Count} | Files: {fileResults.Count}");

// ─── Generate outputs ────────────────────────────────────────────────────────

Directory.CreateDirectory(outputDir);

// 1. Markdown report
var md = new StringBuilder();
md.AppendLine("# Encina Weighted Coverage Report");
md.AppendLine();
md.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
md.AppendLine();
md.AppendLine($"## Overall: {overallPct}% ({overallCovered:N1} / {overallTotal:N0} weighted lines)");
md.AppendLine();
md.AppendLine("## By Category");
md.AppendLine();
md.AppendLine("| Category | Packages | Tests | Coverage | Target | Status |");
md.AppendLine("|----------|:--------:|:-----:|:--------:|:------:|:------:|");
foreach (var cat in categoryResults)
{
    var testsStr = FormatTestTypes(cat.ApplicableTests);
    var status = cat.Percentage >= cat.Target ? "🟢" : cat.Percentage >= cat.Target * 0.8 ? "🟡" : "🔴";
    md.AppendLine($"| {cat.Name} | {cat.PackageCount} | {testsStr} | {cat.Percentage:F1}% | {cat.Target:F0}% | {status} |");
}
md.AppendLine();
md.AppendLine("## By Package");
md.AppendLine();
md.AppendLine("| Package | Category | Unit | Guard | Contract | Property | Integ | Combined | Target | Gap |");
md.AppendLine("|---------|----------|:----:|:-----:|:--------:|:--------:|:-----:|:--------:|:------:|:---:|");

foreach (var pkg in packageResults)
{
    var gap = pkg.Percentage - pkg.Target;
    var gapStr = gap >= 0 ? $"+{gap:F0}%" : $"{gap:F0}%";
    md.AppendLine($"| {pkg.Name} | {pkg.Category} | {FlagPct(pkg, TestType.Unit)} | {FlagPct(pkg, TestType.Guard)} | {FlagPct(pkg, TestType.Contract)} | {FlagPct(pkg, TestType.Property)} | {FlagPct(pkg, TestType.Integration)} | {pkg.Percentage:F1}% | {pkg.Target:F0}% | {gapStr} |");
}

md.AppendLine();
md.AppendLine("<details><summary>File-level detail</summary>");
md.AppendLine();

foreach (var pkg in packageResults.OrderBy(p => p.Name))
{
    md.AppendLine($"### {pkg.Name} ({pkg.Percentage:F1}%)");
    md.AppendLine();
    md.AppendLine("| File | Lines | Covered | Coverage |");
    md.AppendLine("|------|:-----:|:-------:|:--------:|");
    foreach (var file in pkg.Files)
    {
        var shortName = file.RelativePath.Replace($"src/{pkg.Name}/", "");
        md.AppendLine($"| {shortName} | {file.TotalLines} | {file.CoveredEquivalent:F1} | {file.Percentage:F1}% |");
    }
    md.AppendLine();
}

md.AppendLine("</details>");

File.WriteAllText(Path.Combine(outputDir, "encina-coverage-report.md"), md.ToString());
Console.WriteLine($"\n  Markdown: {Path.Combine(outputDir, "encina-coverage-report.md")}");

// 2. JSON summary
var jsonData = new
{
    timestamp = DateTime.UtcNow.ToString("o"),
    overall = new {
        coverage = overallPct, lines = overallTotal, covered = Math.Round(overallCovered, 2),
        perFlag = overallPerFlag.ToDictionary(
            kv => kv.Key.ToString().ToLowerInvariant(),
            kv => new { total = kv.Value.Total, covered = kv.Value.Covered,
                        coverage = kv.Value.Total > 0 ? Math.Round(kv.Value.Covered * 100.0 / kv.Value.Total, 2) : 0 })
    },
    categories = categoryResults.Select(c => new
    {
        name = c.Name,
        tests = FormatTestTypes(c.ApplicableTests),
        coverage = c.Percentage,
        target = c.Target,
        packages = c.PackageCount,
        lines = c.TotalLines,
        covered = Math.Round(c.CoveredEquivalent, 2)
    }),
    packages = packageResults.Select(p => new
    {
        name = p.Name,
        category = p.Category,
        coverage = p.Percentage,
        target = p.Target,
        lines = p.TotalLines,
        covered = Math.Round(p.CoveredEquivalent, 2),
        perFlag = p.PerFlag.ToDictionary(
            kv => kv.Key.ToString().ToLowerInvariant(),
            kv => new { total = kv.Value.Total, covered = kv.Value.Covered,
                        coverage = kv.Value.Total > 0 ? Math.Round(kv.Value.Covered * 100.0 / kv.Value.Total, 2) : 0 })
    })
};

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};
File.WriteAllText(Path.Combine(outputDir, "encina-coverage-summary.json"),
    JsonSerializer.Serialize(jsonData, jsonOptions));
Console.WriteLine($"  JSON:     {Path.Combine(outputDir, "encina-coverage-summary.json")}");

// 3. Badge JSON (shields.io endpoint format)
var badgeColor = overallPct switch
{
    >= 90 => "brightgreen",
    >= 80 => "green",
    >= 70 => "yellowgreen",
    >= 60 => "yellow",
    >= 50 => "orange",
    _ => "red"
};
var badgeJson = new { schemaVersion = 1, label = "weighted coverage", message = $"{overallPct}%", color = badgeColor };
File.WriteAllText(Path.Combine(outputDir, "badge.json"),
    JsonSerializer.Serialize(badgeJson, jsonOptions));
Console.WriteLine($"  Badge:    {Path.Combine(outputDir, "badge.json")}");

// 4. SVG Badge (self-contained, no external dependencies)
var badgeSvg = GenerateBadgeSvg("weighted coverage", $"{overallPct}%", badgeColor);
File.WriteAllText(Path.Combine(outputDir, "badge.svg"), badgeSvg);
Console.WriteLine($"  SVG:      {Path.Combine(outputDir, "badge.svg")}");

// 5. HTML Dashboard
var htmlDashboard = GenerateHtmlDashboard(overallPct, overallTotal, overallCovered,
    categoryResults, packageResults, fileResults);
File.WriteAllText(Path.Combine(outputDir, "index.html"), htmlDashboard);
Console.WriteLine($"  HTML:     {Path.Combine(outputDir, "index.html")}");

Console.WriteLine("\nDone.");

// ─── Helper functions ────────────────────────────────────────────────────────

string FlagPct(PackageCoverage pkg, TestType flag)
{
    if (!pkg.ApplicableTests.HasFlag(flag)) return "-";
    if (pkg.PerFlag.TryGetValue(flag, out var data) && data.Total > 0)
        return $"{Math.Round(data.Covered * 100.0 / data.Total, 0)}%";
    return "0%";
}

static string FormatTestTypes(TestType t)
{
    var parts = new List<string>();
    if (t.HasFlag(TestType.Unit)) parts.Add("U");
    if (t.HasFlag(TestType.Guard)) parts.Add("G");
    if (t.HasFlag(TestType.Contract)) parts.Add("C");
    if (t.HasFlag(TestType.Property)) parts.Add("P");
    if (t.HasFlag(TestType.Integration)) parts.Add("I");
    return string.Join("+", parts);
}

static int CountFlags(TestType flags)
{
    int count = 0;
    if (flags.HasFlag(TestType.Unit)) count++;
    if (flags.HasFlag(TestType.Guard)) count++;
    if (flags.HasFlag(TestType.Contract)) count++;
    if (flags.HasFlag(TestType.Property)) count++;
    if (flags.HasFlag(TestType.Integration)) count++;
    return count;
}

static string GenerateBadgeSvg(string label, string message, string color)
{
    var colorHex = color switch
    {
        "brightgreen" => "#4c1",
        "green" => "#97ca00",
        "yellowgreen" => "#a4a61d",
        "yellow" => "#dfb317",
        "orange" => "#fe7d37",
        "red" => "#e05d44",
        _ => "#9f9f9f"
    };

    var labelWidth = label.Length * 6.5 + 12;
    var messageWidth = message.Length * 7.5 + 12;
    var totalWidth = labelWidth + messageWidth;

    return $"""
    <svg xmlns="http://www.w3.org/2000/svg" width="{totalWidth}" height="20" role="img" aria-label="{label}: {message}">
      <title>{label}: {message}</title>
      <linearGradient id="s" x2="0" y2="100%">
        <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
        <stop offset="1" stop-opacity=".1"/>
      </linearGradient>
      <clipPath id="r"><rect width="{totalWidth}" height="20" rx="3" fill="#fff"/></clipPath>
      <g clip-path="url(#r)">
        <rect width="{labelWidth}" height="20" fill="#555"/>
        <rect x="{labelWidth}" width="{messageWidth}" height="20" fill="{colorHex}"/>
        <rect width="{totalWidth}" height="20" fill="url(#s)"/>
      </g>
      <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="11">
        <text aria-hidden="true" x="{labelWidth / 2}" y="15" fill="#010101" fill-opacity=".3">{label}</text>
        <text x="{labelWidth / 2}" y="14">{label}</text>
        <text aria-hidden="true" x="{labelWidth + messageWidth / 2}" y="15" fill="#010101" fill-opacity=".3">{message}</text>
        <text x="{labelWidth + messageWidth / 2}" y="14">{message}</text>
      </g>
    </svg>
    """;
}

static string GenerateHtmlDashboard(double overallPct, int overallTotal, double overallCovered,
    List<CategoryCoverage> categories, List<PackageCoverage> packages, List<FileCoverage> files)
{
    var jOpts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };
    var packagesJson = JsonSerializer.Serialize(packages.Select(p => new
    {
        name = p.Name, category = p.Category, coverage = p.Percentage, target = p.Target,
        lines = p.TotalLines, covered = Math.Round(p.CoveredEquivalent, 2),
        applicableTests = FormatTestTypes(p.ApplicableTests),
        files = p.Files.Select(f => new
        {
            path = f.RelativePath.Replace($"src/{p.Name}/", ""),
            lines = f.TotalLines, covered = Math.Round(f.CoveredEquivalent, 2), coverage = f.Percentage
        })
    }), jOpts);

    var categoriesJson = JsonSerializer.Serialize(categories.Select(c => new
    {
        name = c.Name, tests = FormatTestTypes(c.ApplicableTests),
        coverage = c.Percentage, target = c.Target,
        packages = c.PackageCount, lines = c.TotalLines, covered = Math.Round(c.CoveredEquivalent, 2)
    }), jOpts);

    return $$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Encina Coverage Report</title>
    <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0d1117; color: #c9d1d9; padding: 24px; }
    h1 { font-size: 24px; margin-bottom: 8px; }
    h2 { font-size: 18px; margin: 24px 0 12px; color: #58a6ff; }
    .overall { font-size: 48px; font-weight: bold; margin: 16px 0; }
    .overall .pct { color: {{(overallPct >= 70 ? "#3fb950" : overallPct >= 50 ? "#d29922" : "#f85149")}}; }
    .overall .detail { font-size: 16px; color: #8b949e; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin: 24px 0; }
    @media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }
    .card { background: #161b22; border: 1px solid #30363d; border-radius: 6px; padding: 16px; }
    table { width: 100%; border-collapse: collapse; font-size: 13px; }
    th { text-align: left; padding: 8px; border-bottom: 2px solid #30363d; color: #8b949e; font-weight: 600; cursor: pointer; }
    th:hover { color: #58a6ff; }
    td { padding: 6px 8px; border-bottom: 1px solid #21262d; }
    tr:hover { background: #1c2128; }
    .bar { height: 8px; border-radius: 4px; background: #21262d; position: relative; overflow: hidden; }
    .bar-fill { height: 100%; border-radius: 4px; transition: width 0.3s; }
    .green { background: #3fb950; } .yellow { background: #d29922; } .red { background: #f85149; }
    .num { text-align: right; font-variant-numeric: tabular-nums; }
    .tag { display: inline-block; padding: 2px 6px; border-radius: 3px; font-size: 11px; background: #21262d; margin-right: 4px; }
    .filter-bar { margin: 12px 0; display: flex; gap: 8px; flex-wrap: wrap; }
    .filter-btn { padding: 4px 12px; border: 1px solid #30363d; border-radius: 16px; background: none; color: #c9d1d9; cursor: pointer; font-size: 12px; }
    .filter-btn.active { background: #1f6feb; border-color: #1f6feb; }
    .filter-btn:hover { border-color: #58a6ff; }
    details { margin: 4px 0; }
    details summary { cursor: pointer; padding: 4px 0; color: #58a6ff; }
    details summary:hover { text-decoration: underline; }
    .status { font-size: 14px; }
    input[type="text"] { background: #0d1117; border: 1px solid #30363d; border-radius: 6px; color: #c9d1d9; padding: 6px 12px; width: 100%; margin-bottom: 12px; }
    </style>
    </head>
    <body>
    <h1>Encina Weighted Coverage Report</h1>
    <p style="color:#8b949e">Generated: {{DateTime.UtcNow:yyyy-MM-dd HH:mm}} UTC</p>

    <div class="overall">
      <span class="pct">{{overallPct}}%</span>
      <div class="detail">{{overallCovered:N1}} / {{overallTotal:N0}} weighted lines covered</div>
    </div>

    <div class="grid">
      <div class="card">
        <h2>By Category</h2>
        <table id="catTable">
          <thead><tr><th>Category</th><th>Tests</th><th class="num">Coverage</th><th class="num">Target</th><th>Bar</th><th>Status</th></tr></thead>
          <tbody id="catBody"></tbody>
        </table>
      </div>
      <div class="card">
        <h2>Coverage Distribution</h2>
        <canvas id="chart" height="250"></canvas>
      </div>
    </div>

    <div class="card">
      <h2>By Package</h2>
      <div class="filter-bar" id="filters"></div>
      <input type="text" id="search" placeholder="Search packages..." />
      <table id="pkgTable">
        <thead><tr>
          <th onclick="sortTable('name')">Package</th>
          <th onclick="sortTable('category')">Category</th>
          <th class="num" onclick="sortTable('coverage')">Coverage</th>
          <th class="num" onclick="sortTable('target')">Target</th>
          <th class="num" onclick="sortTable('gap')">Gap</th>
          <th>Bar</th>
          <th class="num" onclick="sortTable('lines')">Lines</th>
        </tr></thead>
        <tbody id="pkgBody"></tbody>
      </table>
    </div>

    <script>
    const categories = {{categoriesJson}};
    const packages = {{packagesJson}};
    let activeFilter = 'all';
    let sortKey = 'coverage';
    let sortAsc = true;

    function barColor(pct) { return pct >= 70 ? 'green' : pct >= 50 ? 'yellow' : 'red'; }
    function statusIcon(pct, target) { return pct >= target ? '🟢' : pct >= target * 0.8 ? '🟡' : '🔴'; }

    function renderCategories() {
      const body = document.getElementById('catBody');
      body.innerHTML = categories.map(c => `<tr>
        <td>${c.name}</td><td><span class="tag">${c.tests}</span></td>
        <td class="num">${c.coverage.toFixed(1)}%</td><td class="num">${c.target}%</td>
        <td><div class="bar"><div class="bar-fill ${barColor(c.coverage)}" style="width:${Math.min(c.coverage,100)}%"></div></div></td>
        <td class="status">${statusIcon(c.coverage, c.target)}</td>
      </tr>`).join('');
    }

    function renderPackages() {
      const body = document.getElementById('pkgBody');
      const search = document.getElementById('search').value.toLowerCase();
      let filtered = packages.filter(p =>
        (activeFilter === 'all' || p.category === activeFilter) &&
        (!search || p.name.toLowerCase().includes(search))
      );
      filtered.sort((a, b) => {
        let va = a[sortKey], vb = b[sortKey];
        if (sortKey === 'gap') { va = a.coverage - a.target; vb = b.coverage - b.target; }
        if (typeof va === 'string') return sortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
        return sortAsc ? va - vb : vb - va;
      });
      body.innerHTML = filtered.map(p => {
        const gap = (p.coverage - p.target).toFixed(0);
        const gapStr = gap >= 0 ? `+${gap}%` : `${gap}%`;
        const gapColor = gap >= 0 ? '#3fb950' : '#f85149';
        const filesHtml = p.files.map(f =>
          `<tr><td style="padding-left:24px;color:#8b949e">${f.path}</td><td class="num">${f.lines}</td><td class="num">${f.covered}</td><td class="num">${f.coverage.toFixed(1)}%</td></tr>`
        ).join('');
        return `<tr>
          <td><details><summary>${p.name}</summary><table style="margin:8px 0">${filesHtml}</table></details></td>
          <td><span class="tag">${p.category}</span></td>
          <td class="num">${p.coverage.toFixed(1)}%</td><td class="num">${p.target}%</td>
          <td class="num" style="color:${gapColor}">${gapStr}</td>
          <td><div class="bar"><div class="bar-fill ${barColor(p.coverage)}" style="width:${Math.min(p.coverage,100)}%"></div></div></td>
          <td class="num">${p.lines.toLocaleString()}</td>
        </tr>`;
      }).join('');
    }

    function renderFilters() {
      const cats = ['all', ...new Set(packages.map(p => p.category))];
      document.getElementById('filters').innerHTML = cats.map(c =>
        `<button class="filter-btn ${c === activeFilter ? 'active' : ''}" onclick="setFilter('${c}')">${c}</button>`
      ).join('');
    }

    function setFilter(cat) { activeFilter = cat; renderFilters(); renderPackages(); }
    function sortTable(key) {
      if (sortKey === key) sortAsc = !sortAsc; else { sortKey = key; sortAsc = true; }
      renderPackages();
    }
    document.getElementById('search').addEventListener('input', renderPackages);

    // Simple bar chart on canvas
    function renderChart() {
      const canvas = document.getElementById('chart');
      const ctx = canvas.getContext('2d');
      const w = canvas.width = canvas.offsetWidth;
      const h = canvas.height;
      const barW = Math.min(60, (w - 40) / categories.length - 10);
      ctx.clearRect(0, 0, w, h);
      ctx.fillStyle = '#8b949e'; ctx.font = '11px sans-serif'; ctx.textAlign = 'center';
      categories.forEach((c, i) => {
        const x = 30 + i * (barW + 10);
        const barH = (c.coverage / 100) * (h - 50);
        const targetH = (c.target / 100) * (h - 50);
        // Bar
        ctx.fillStyle = c.coverage >= c.target ? '#3fb950' : c.coverage >= c.target * 0.8 ? '#d29922' : '#f85149';
        ctx.fillRect(x, h - 30 - barH, barW, barH);
        // Target line
        ctx.strokeStyle = '#58a6ff'; ctx.lineWidth = 2; ctx.setLineDash([4, 2]);
        ctx.beginPath(); ctx.moveTo(x - 4, h - 30 - targetH); ctx.lineTo(x + barW + 4, h - 30 - targetH); ctx.stroke();
        ctx.setLineDash([]);
        // Label
        ctx.fillStyle = '#8b949e'; ctx.fillText(c.name.substring(0, 8), x + barW/2, h - 14);
        ctx.fillStyle = '#c9d1d9'; ctx.fillText(c.coverage.toFixed(0) + '%', x + barW/2, h - 34 - barH);
      });
    }

    renderCategories(); renderFilters(); renderPackages(); renderChart();
    window.addEventListener('resize', renderChart);
    </script>
    </body>
    </html>
    """;
}

// ─── Type declarations (must be after top-level statements in C# 14) ─────────

[Flags]
enum TestType
{
    None = 0,
    Unit = 1,
    Guard = 2,
    Contract = 4,
    Property = 8,
    Integration = 16,
    All = Unit | Guard | Contract | Property | Integration
}

record CategoryDef(string Name, TestType ApplicableTests, double Target, string[] Patterns);

record WeightsConfig
{
    [JsonPropertyName("categories")]
    public CategoryConfig[] Categories { get; init; } = [];
}

record CategoryConfig
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";
    [JsonPropertyName("applicableTests")]
    public string[] ApplicableTests { get; init; } = [];
    [JsonPropertyName("target")]
    public double Target { get; init; }
    [JsonPropertyName("patterns")]
    public string[] Patterns { get; init; } = [];
}

record FileCoverage(string RelativePath, string Package, string Category, int TotalLines,
    double CoveredEquivalent, double Percentage, Dictionary<TestType, (int Total, int Covered)> PerFlag);

record PackageCoverage(string Name, string Category, TestType ApplicableTests, double Target,
    int TotalLines, double CoveredEquivalent, double Percentage, Dictionary<TestType, (int Total, int Covered)> PerFlag,
    List<FileCoverage> Files);

record CategoryCoverage(string Name, TestType ApplicableTests, double Target,
    int PackageCount, int TotalLines, double CoveredEquivalent, double Percentage);

record ManifestFile
{
    [JsonPropertyName("package")]
    public string? Package { get; init; }
    [JsonPropertyName("files")]
    public Dictionary<string, ManifestFileEntry>? Files { get; init; }
}

record ManifestFileEntry
{
    [JsonPropertyName("defaultTests")]
    public string[]? DefaultTests { get; init; }
    [JsonPropertyName("override")]
    public string[]? Override { get; init; }
}
