// generate-coverage-manifest.cs — Generates per-package coverage manifest files
#pragma warning disable CA1305, CA1310, CA1852
// Scans all .cs files in src/, applies default rules from defaults.json,
// and generates one JSON manifest per package in .github/coverage-manifest/.
//
// Usage:
//   dotnet run .github/scripts/generate-coverage-manifest.cs                 full mode
//   dotnet run .github/scripts/generate-coverage-manifest.cs -- --append-only
//   dotnet run .github/scripts/generate-coverage-manifest.cs -- --self-test
//   Options: --src <dir>  --defaults <file>  --output <dir>
//
// Ownership (#1542): the generator owns only the package-level keys package, generated,
// totalFiles and files, and inside each file entry only defaultTests, defaultRule and
// reason. When the output manifest already exists it is read as a JSON object and every
// other key (targets, reviewed, per-file override, any future key) is written back
// unchanged and in its original position.
//
// Modes:
//   full         (default) recomputes every file entry from the rules, adds entries for
//                new source files, REMOVES entries whose source file no longer exists
//                (printed), and always stamps "generated" with the current UTC time. A
//                manifest with nothing else to change therefore regenerates byte-identical
//                apart from the "generated" line.
//   --append-only adds an entry for every source file that has none, computed exactly as
//                the full mode computes it. It never modifies or removes an existing
//                entry. totalFiles becomes the resulting entry count; "generated" changes
//                only when at least one entry was added. The added files are printed.
//   --self-test  runs the built-in assertions against temporary fixtures; exit code 1 on
//                any failed assertion.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var srcDir = "src";
var defaultsFile = ".github/coverage-manifest/defaults.json";
var outputDir = ".github/coverage-manifest";
bool appendOnly = false, selfTest = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--src" && i + 1 < args.Length) srcDir = args[++i];
    else if (args[i] == "--defaults" && i + 1 < args.Length) defaultsFile = args[++i];
    else if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
    else if (args[i] == "--append-only") appendOnly = true;
    else if (args[i] == "--self-test") selfTest = true;
}

var jsonOpts = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};

var writeOpts = new JsonSerializerOptions
{
    WriteIndented = true,
    NewLine = "\n", // the repository stores manifests with LF (.gitattributes); same output on every OS
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // committed manifests hold literal characters such as an em dash
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};

if (selfTest) return RunSelfTest();

if (!File.Exists(defaultsFile))
{
    Console.Error.WriteLine($"ERROR: Defaults file not found: {defaultsFile}");
    return 1;
}

var defaults = LoadDefaults(defaultsFile);
Console.WriteLine($"Loaded {defaults.Rules.Length} rules from {defaultsFile}");

return Generate(srcDir, defaults, outputDir, appendOnly, () => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture), Console.Out);

// ─── Generation ─────────────────────────────────────────────────────────────

DefaultsConfig LoadDefaults(string file) =>
    JsonSerializer.Deserialize<DefaultsConfig>(File.ReadAllText(file), jsonOpts)!;

int Generate(string src, DefaultsConfig cfg, string outDir, bool append, Func<string> now, TextWriter log)
{
    // Scan and classify each source file.
    var allFiles = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        .ToList();

    log.WriteLine($"Found {allFiles.Count} source files");

    var packageFiles = new Dictionary<string, List<FileEntry>>(StringComparer.OrdinalIgnoreCase);

    foreach (var file in allFiles)
    {
        var relPath = Path.GetRelativePath(src, file).Replace('\\', '/');
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

        var (tests, rule, reason) = ApplyRules(fileName, fileRelPath, isInterface, cfg.Rules);

        if (!packageFiles.ContainsKey(packageName))
            packageFiles[packageName] = [];

        packageFiles[packageName].Add(new FileEntry(fileRelPath, tests, rule, reason));
    }

    log.WriteLine($"Classified files across {packageFiles.Count} packages");

    Directory.CreateDirectory(outDir);
    int created = 0, updated = 0, failed = 0;

    foreach (var (pkg, files) in packageFiles.OrderBy(kv => kv.Key))
    {
        var outputFile = Path.Combine(outDir, $"{pkg}.json");
        var sorted = files.OrderBy(f => f.Path).ToList();

        JsonObject? existing = null;
        if (File.Exists(outputFile))
        {
            try
            {
                existing = JsonNode.Parse(File.ReadAllText(outputFile), null,
                    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
                _ = existing?["files"]; // duplicate keys surface lazily as ArgumentException
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException) { existing = null; }

            if (existing is null)
            {
                // Overwriting would lose whatever the file holds: skip it and fail the run.
                Console.Error.WriteLine($"ERROR: {outputFile} exists but is not a valid JSON object; skipped.");
                failed++;
                continue;
            }
        }

        var existingFiles = existing?["files"] as JsonObject;
        JsonObject newFiles;
        var added = new List<string>();
        var removed = new List<string>();
        var changed = new List<string>();

        if (append)
        {
            newFiles = new JsonObject();
            foreach (var (k, v) in existingFiles ?? new JsonObject())
                newFiles.Add(k, v?.DeepClone());

            foreach (var f in sorted)
            {
                if (existingFiles?.ContainsKey(f.Path) == true) continue;
                InsertSorted(newFiles, f.Path, NewEntry(f));
                added.Add(f.Path);
            }
        }
        else
        {
            newFiles = new JsonObject();
            var sourcePaths = new HashSet<string>(sorted.Select(f => f.Path), StringComparer.Ordinal);
            foreach (var f in sorted)
            {
                if (existingFiles?[f.Path] is JsonObject old)
                {
                    var entry = (JsonObject)old.DeepClone();
                    // Owned keys are recomputed: report entries whose hand-edited values the rules replace.
                    if (old["defaultTests"]?.ToJsonString() != TestsArray(f.Tests).ToJsonString()
                        || old["defaultRule"]?.ToString() != f.Rule
                        || old["reason"]?.ToString() != f.Reason)
                        changed.Add(f.Path);
                    // Replacing a value keeps the key's position, so unknown keys stay where they were.
                    entry["defaultTests"] = TestsArray(f.Tests);
                    entry["defaultRule"] = f.Rule;
                    entry["reason"] = f.Reason;
                    newFiles.Add(f.Path, entry);
                }
                else
                {
                    newFiles.Add(f.Path, NewEntry(f));
                    added.Add(f.Path);
                }
            }
            foreach (var (k, _) in existingFiles ?? new JsonObject())
                if (!sourcePaths.Contains(k)) removed.Add(k);
        }

        // Append-only with nothing to add leaves the file alone, byte for byte (hand-formatted manifests included).
        if (append && existing is not null && added.Count == 0)
        {
            updated++;
            continue;
        }

        JsonObject manifest;
        bool stamp = !append || added.Count > 0 || existing is null;
        if (existing is null)
        {
            manifest = new JsonObject
            {
                ["package"] = pkg,
                ["generated"] = now(),
                ["totalFiles"] = newFiles.Count,
                ["files"] = newFiles
            };
        }
        else
        {
            manifest = (JsonObject)existing.DeepClone();
            manifest["package"] = pkg;
            if (stamp) manifest["generated"] = now();
            manifest["totalFiles"] = newFiles.Count;
            manifest["files"] = newFiles;
        }

        // Final newline: committed manifests end with one.
        File.WriteAllText(outputFile, manifest.ToJsonString(writeOpts) + "\n");

        if (existing is not null) updated++; else created++;

        if (existing is null) log.WriteLine($"  {pkg}: new manifest, {added.Count} file(s)");
        else if (added.Count > 0) log.WriteLine($"  {pkg}: added {added.Count}: {string.Join(", ", added)}");
        if (removed.Count > 0) log.WriteLine($"  {pkg}: removed {removed.Count}: {string.Join(", ", removed)}");
        if (changed.Count > 0)
            log.WriteLine($"  {pkg}: recomputed {changed.Count} entr{(changed.Count == 1 ? "y" : "ies")} whose defaultTests/defaultRule/reason differed from the rules (move a deliberate classification into \"override\"): "
                + string.Join(", ", changed.Take(10)) + (changed.Count > 10 ? $", +{changed.Count - 10} more" : ""));
    }

    log.WriteLine($"\n{(append ? "Append-only" : "Generated")}: {created} new + {updated} updated manifests in {outDir}/");

    // Summary
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

    if (allFiles.Count > 0)
    {
        log.WriteLine($"\n  Total files: {allFiles.Count}");
        log.WriteLine($"  Need tests: {totalWithTests} ({Math.Round(totalWithTests * 100.0 / allFiles.Count)}%)");
        log.WriteLine($"  Excluded:   {totalExcluded} ({Math.Round(totalExcluded * 100.0 / allFiles.Count)}%)");
        log.WriteLine($"\n  Files per test type:");
        foreach (var (tt, count) in testTypeCounts.OrderByDescending(kv => kv.Value))
            log.WriteLine($"    {tt,-15} {count,5} files");
    }

    return failed == 0 ? 0 : 1;
}

JsonArray TestsArray(string[] tests)
{
    var arr = new JsonArray();
    foreach (var t in tests) arr.Add(JsonValue.Create(t));
    return arr;
}

JsonObject NewEntry(FileEntry f) => new()
{
    ["defaultTests"] = TestsArray(f.Tests),
    ["defaultRule"] = f.Rule,
    ["reason"] = f.Reason
};

// Inserts before the first existing key that sorts after the new one, so the original order
// of existing keys never changes; appends when none does.
void InsertSorted(JsonObject target, string key, JsonNode value)
{
    var snapshot = target.Select(kv => (kv.Key, kv.Value?.DeepClone())).ToList();
    int at = snapshot.FindIndex(kv => Comparer<string>.Default.Compare(kv.Key, key) > 0);
    if (at < 0) at = snapshot.Count;
    snapshot.Insert(at, (key, value));
    target.Clear();
    foreach (var (k, v) in snapshot) target.Add(k, v);
}

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

// ─── Self-test ──────────────────────────────────────────────────────────────

int RunSelfTest()
{
    int failures = 0, checks = 0;
    void Check(bool ok, string what)
    {
        checks++;
        if (!ok) { failures++; Console.Error.WriteLine($"  FAIL: {what}"); }
    }

    var root = Path.Combine(Path.GetTempPath(), "encina-manifest-selftest-" + Guid.NewGuid().ToString("N"));
    try
    {
        var src = Path.Combine(root, "src", "PkgA");
        var outDir = Path.Combine(root, "out");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(src, "A.cs"), "class A {}");
        File.WriteAllText(Path.Combine(src, "B.cs"), "class B {}");
        var cfg = new DefaultsConfig { Rules = [new Rule { Pattern = "*.cs", Match = "glob", Tests = ["unit"], Reason = "r" }] };
        var manifestPath = Path.Combine(outDir, "PkgA.json");
        var quiet = new StringWriter();

        // Manifest: targets and an unknown package key, A with override + unknown key, Gone has no source.
        var seed = """
            {
              "package": "PkgA",
              "generated": "2000-01-01T00:00:00Z",
              "totalFiles": 2,
              "targets": { "unit": 70 },
              "owner": "team-x",
              "files": {
                "A.cs": {
                  "defaultTests": [ "guard" ],
                  "note": "keep me",
                  "defaultRule": "stale",
                  "reason": "stale",
                  "override": [ "integration" ]
                },
                "Gone.cs": { "defaultTests": [ "unit" ], "defaultRule": "*.cs", "reason": "r" }
              }
            }
            """;

        // 1. Full mode keeps unowned keys, recomputes owned ones, removes Gone.cs, adds B.cs.
        File.WriteAllText(manifestPath, seed);
        Generate(Path.Combine(root, "src"), cfg, outDir, false, () => "T1", quiet);
        var full = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        Check(full["targets"]?["unit"]?.GetValue<int>() == 70, "full: targets kept");
        Check(full["owner"]?.GetValue<string>() == "team-x", "full: unknown package key kept");
        Check(full.Select(kv => kv.Key).SequenceEqual(["package", "generated", "totalFiles", "targets", "owner", "files"]),
            "full: package-level key order kept");
        var a = full["files"]!["A.cs"]!.AsObject();
        Check(a["override"]?[0]?.GetValue<string>() == "integration", "full: per-file override kept");
        Check(a["note"]?.GetValue<string>() == "keep me", "full: unknown per-file key kept");
        Check(a["defaultRule"]?.GetValue<string>() == "*.cs" && a["defaultTests"]![0]!.GetValue<string>() == "unit",
            "full: computed keys recomputed");
        Check(a.Select(kv => kv.Key).SequenceEqual(["defaultTests", "note", "defaultRule", "reason", "override"]),
            "full: per-file key order kept");
        Check(full["files"]!.AsObject().ContainsKey("B.cs"), "full: new file added");
        Check(!full["files"]!.AsObject().ContainsKey("Gone.cs"), "full: deleted file removed");
        Check(full["totalFiles"]?.GetValue<int>() == 2 && full["generated"]?.GetValue<string>() == "T1", "full: totalFiles and generated set");

        Check(quiet.ToString().Contains("recomputed 1 entry") && quiet.ToString().Contains("removed 1: Gone.cs"),
            "full: reports recomputed and removed entries");

        // 2. Full mode on its own output is byte-identical (same clock).
        var bytes1 = File.ReadAllText(manifestPath);
        Generate(Path.Combine(root, "src"), cfg, outDir, false, () => "T1", quiet);
        Check(File.ReadAllText(manifestPath) == bytes1, "full: regenerating is byte-identical");

        // 3. Append-only adds B.cs, touches nothing else, keeps Gone.cs.
        File.WriteAllText(manifestPath, seed);
        Generate(Path.Combine(root, "src"), cfg, outDir, true, () => "T2", quiet);
        var app = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        var seedFiles = JsonNode.Parse(seed)!["files"]!.AsObject();
        Check(app["files"]!.AsObject().ContainsKey("B.cs"), "append-only: missing file added");
        Check(app["files"]!["A.cs"]!.ToJsonString() == seedFiles["A.cs"]!.ToJsonString(), "append-only: existing entry untouched");
        Check(app["files"]!.AsObject().ContainsKey("Gone.cs"), "append-only: deleted file NOT removed");
        Check(app["targets"]?["unit"]?.GetValue<int>() == 70 && app["owner"]?.GetValue<string>() == "team-x", "append-only: package keys kept");
        Check(app["totalFiles"]?.GetValue<int>() == 3, "append-only: totalFiles is the resulting count");
        Check(app["generated"]?.GetValue<string>() == "T2", "append-only: generated set when an entry was added");
        Check(app["files"]!.AsObject().Select(kv => kv.Key).SequenceEqual(["A.cs", "B.cs", "Gone.cs"]),
            "append-only: new entry inserted in sorted position");

        // 4. Append-only with nothing missing leaves the file byte-identical, generated included.
        var bytes2 = File.ReadAllText(manifestPath);
        Generate(Path.Combine(root, "src"), cfg, outDir, true, () => "T3", quiet);
        Check(File.ReadAllText(manifestPath) == bytes2, "append-only: nothing missing leaves the file byte-identical");
    }
    finally
    {
        try { Directory.Delete(root, true); } catch { /* best effort */ }
    }

    Console.WriteLine(failures == 0 ? $"Self-test passed ({checks} checks)." : $"Self-test FAILED: {failures} of {checks} checks.");
    return failures == 0 ? 0 : 1;
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
