// generate-coverage-manifest.cs — Generates per-package coverage manifest files
#pragma warning disable CA1305, CA1310, CA1852
// Scans all .cs files in src/, applies default rules from defaults.json,
// and generates one JSON manifest per package in .github/coverage-manifest/.
//
// Usage: dotnet run .github/scripts/generate-coverage-manifest.cs
//
// After running, review the generated manifests and add overrides for
// files that need different test types than what the defaults assigned.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var srcDir = "src";
var defaultsFile = ".github/coverage-manifest/defaults.json";
var outputDir = ".github/coverage-manifest";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--src" && i + 1 < args.Length) srcDir = args[++i];
    if (args[i] == "--defaults" && i + 1 < args.Length) defaultsFile = args[++i];
    if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
}

// ─── Load defaults ──────────────────────────────────────────────────────────

if (!File.Exists(defaultsFile))
{
    Console.Error.WriteLine($"ERROR: Defaults file not found: {defaultsFile}");
    return;
}

var jsonOpts = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};

var defaults = JsonSerializer.Deserialize<DefaultsConfig>(File.ReadAllText(defaultsFile), jsonOpts)!;
Console.WriteLine($"Loaded {defaults.Rules.Length} rules from {defaultsFile}");

// ─── Scan source files ──────────────────────────────────────────────────────

var allFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .ToList();

Console.WriteLine($"Found {allFiles.Count} source files");

// ─── Classify each file ─────────────────────────────────────────────────────

var packageFiles = new Dictionary<string, List<FileEntry>>(StringComparer.OrdinalIgnoreCase);

foreach (var file in allFiles)
{
    var relPath = Path.GetRelativePath(srcDir, file).Replace('\\', '/');
    var parts = relPath.Split('/');
    if (parts.Length < 2) continue;

    var packageName = parts[0];
    var fileRelPath = string.Join("/", parts[1..]);
    var fileName = Path.GetFileName(file);

    // Check if file is an interface (read first 40 lines)
    bool isInterface = false;
    if (fileName.Length > 1 && fileName[0] == 'I' && char.IsUpper(fileName[1]))
    {
        try
        {
            var lines = File.ReadLines(file).Take(40);
            isInterface = lines.Any(l => Regex.IsMatch(l, @"^\s*(public\s+)?interface\s+"));
        }
        catch { /* ignore read errors */ }
    }

    // Apply rules
    var (tests, rule, reason) = ApplyRules(fileName, fileRelPath, isInterface, defaults.Rules);

    if (!packageFiles.ContainsKey(packageName))
        packageFiles[packageName] = [];

    packageFiles[packageName].Add(new FileEntry(fileRelPath, tests, rule, reason));
}

Console.WriteLine($"Classified files across {packageFiles.Count} packages");

// ─── Load existing overrides ────────────────────────────────────────────────

var existingOverrides = new Dictionary<string, Dictionary<string, string[]>>(StringComparer.OrdinalIgnoreCase);
foreach (var manifestFile in Directory.GetFiles(outputDir, "*.json")
    .Where(f => Path.GetFileName(f) != "defaults.json"))
{
    try
    {
        var manifest = JsonSerializer.Deserialize<PackageManifest>(File.ReadAllText(manifestFile), jsonOpts);
        if (manifest?.Overrides is not null && manifest.Package is not null)
        {
            existingOverrides[manifest.Package] = manifest.Overrides;
        }
    }
    catch { /* ignore parse errors on existing files */ }
}

Console.WriteLine($"Found {existingOverrides.Count} existing manifest(s) with overrides");

// ─── Generate manifests ─────────────────────────────────────────────────────

Directory.CreateDirectory(outputDir);
int created = 0, updated = 0;

var writeOpts = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};

foreach (var (pkg, files) in packageFiles.OrderBy(kv => kv.Key))
{
    var outputFile = Path.Combine(outputDir, $"{pkg}.json");

    // Merge existing overrides
    var overrides = existingOverrides.TryGetValue(pkg, out var existing) ? existing : null;

    // Build the files section with defaults applied
    var fileEntries = new Dictionary<string, FileManifestEntry>();
    foreach (var f in files.OrderBy(f => f.Path))
    {
        // Check if there's an override
        string[]? overriddenTests = null;
        if (overrides is not null && overrides.TryGetValue(f.Path, out var ov))
        {
            overriddenTests = ov;
        }

        fileEntries[f.Path] = new FileManifestEntry
        {
            DefaultTests = f.Tests,
            DefaultRule = f.Rule,
            Reason = f.Reason,
            Override = overriddenTests
        };
    }

    var manifest = new GeneratedManifest
    {
        Package = pkg,
        Generated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        TotalFiles = files.Count,
        Files = fileEntries
    };

    File.WriteAllText(outputFile, JsonSerializer.Serialize(manifest, writeOpts));

    if (existingOverrides.ContainsKey(pkg)) updated++;
    else created++;
}

Console.WriteLine($"\nGenerated {created} new + {updated} updated manifests in {outputDir}/");

// ─── Summary ────────────────────────────────────────────────────────────────

var testTypeCounts = new Dictionary<string, int>();
int totalWithTests = 0, totalExcluded = 0;

foreach (var (_, files) in packageFiles)
{
    foreach (var f in files)
    {
        if (f.Tests.Length == 0) { totalExcluded++; continue; }
        totalWithTests++;
        foreach (var t in f.Tests)
        {
            if (!testTypeCounts.ContainsKey(t)) testTypeCounts[t] = 0;
            testTypeCounts[t]++;
        }
    }
}

Console.WriteLine($"\n  Total files: {allFiles.Count}");
Console.WriteLine($"  Need tests: {totalWithTests} ({Math.Round(totalWithTests * 100.0 / allFiles.Count)}%)");
Console.WriteLine($"  Excluded:   {totalExcluded} ({Math.Round(totalExcluded * 100.0 / allFiles.Count)}%)");
Console.WriteLine($"\n  Files per test type:");
foreach (var (tt, count) in testTypeCounts.OrderByDescending(kv => kv.Value))
    Console.WriteLine($"    {tt,-15} {count,5} files");

// ─── Rule matching logic ────────────────────────────────────────────────────

(string[] Tests, string Rule, string Reason) ApplyRules(string fileName, string relPath, bool isInterface, Rule[] rules)
{
    foreach (var rule in rules)
    {
        if (rule.Pattern is null) continue; // $comment entries

        bool matched = rule.Match switch
        {
            "exact" => fileName == rule.Pattern,
            "glob" => GlobMatch(fileName, rule.Pattern),
            "regex" => Regex.IsMatch(fileName, rule.Pattern),
            "path-glob" => GlobMatch(relPath, rule.Pattern),
            _ => false
        };

        // Additional condition check
        if (matched && rule.Condition == "contains_interface" && !isInterface)
            matched = false;

        if (matched)
            return (rule.Tests ?? [], rule.Pattern, rule.Reason ?? "");
    }

    return (["unit", "guard"], "*.cs (fallback)", "No specific rule matched");
}

bool GlobMatch(string input, string pattern)
{
    // Simple glob: * matches any chars, ? matches one char
    var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
}

// ─── Types ──────────────────────────────────────────────────────────────────

record DefaultsConfig
{
    [JsonPropertyName("rules")]
    public Rule[] Rules { get; init; } = [];
}

record Rule
{
    [JsonPropertyName("pattern")]
    public string? Pattern { get; init; }
    [JsonPropertyName("match")]
    public string Match { get; init; } = "glob";
    [JsonPropertyName("tests")]
    public string[]? Tests { get; init; }
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
    [JsonPropertyName("condition")]
    public string? Condition { get; init; }
    [JsonPropertyName("$comment")]
    public string? Comment { get; init; }
}

record FileEntry(string Path, string[] Tests, string Rule, string Reason);

record PackageManifest
{
    [JsonPropertyName("package")]
    public string? Package { get; init; }
    [JsonPropertyName("overrides")]
    public Dictionary<string, string[]>? Overrides { get; init; }
}

record FileManifestEntry
{
    [JsonPropertyName("defaultTests")]
    public string[] DefaultTests { get; init; } = [];
    [JsonPropertyName("defaultRule")]
    public string? DefaultRule { get; init; }
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
    [JsonPropertyName("override")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Override { get; init; }
}

record GeneratedManifest
{
    [JsonPropertyName("package")]
    public string Package { get; init; } = "";
    [JsonPropertyName("generated")]
    public string Generated { get; init; } = "";
    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; init; }
    [JsonPropertyName("files")]
    public Dictionary<string, FileManifestEntry> Files { get; init; } = [];
}
