// generate-coverage-manifest.cs — Generates per-package coverage manifest files
#pragma warning disable CA1305, CA1310, CA1852
// Scans all .cs files in src/, applies default rules from defaults.json,
// and generates one JSON manifest per package in .github/coverage-manifest/.
//
// Usage:
//   dotnet run --file .github/scripts/generate-coverage-manifest.cs                    append-only (default)
//   dotnet run --file .github/scripts/generate-coverage-manifest.cs -- --dry-run       print the plan, write nothing
//   dotnet run --file .github/scripts/generate-coverage-manifest.cs -- --full          regenerate every manifest
//   dotnet run --file .github/scripts/generate-coverage-manifest.cs -- --self-test
//   Options: --src <dir>  --defaults <file>  --output <dir>  (relative paths resolve against the repository root)
//   --append-only is accepted as an explicit spelling of the default. Any other argument is an error.
//
// Repository root (#1702): the git top-level of this script's own directory; the script only ever
// modifies the checkout it belongs to. When the current directory belongs to a different git
// top-level the run fails naming both paths. The root, the mode and the plan (manifests and entries
// to create or change) are printed before anything is written, and the same counts after.
//
// Ownership (#1542): the generator owns only the package-level keys package, generated,
// totalFiles and files, and inside each file entry only defaultTests, defaultRule and
// reason. When the output manifest already exists it is read as a JSON object and every
// other key (targets, reviewed, per-file override, any future key) is written back
// unchanged and in its original position.
//
// Modes:
//   (default)    append-only: adds an entry for every source file that has none (and creates
//                manifests for packages that have none), computed exactly as the full mode
//                computes it. It never modifies or removes an existing entry. totalFiles
//                becomes the resulting entry count; "generated" changes only when at least
//                one entry was added. The added files are printed.
//   --full       recomputes every file entry from the rules, adds entries for new source
//                files, REMOVES entries whose source file no longer exists (printed), and
//                always stamps "generated" with the current UTC time. It rewrites every
//                manifest, so it only runs when asked for explicitly.
//   --dry-run    with either mode: prints the plan and writes nothing.
//   --self-test  runs the built-in assertions against temporary fixtures; exit code 1 on
//                any failed assertion.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

const string Usage = """
    Usage: dotnet run --file .github/scripts/generate-coverage-manifest.cs -- [options]
      (no mode flag)   append-only: add entries for files missing from a manifest, create missing manifests
      --full           regenerate every manifest (recompute entries, remove stale ones)
      --dry-run        print the plan and write nothing
      --append-only    explicit spelling of the default
      --src <dir>  --defaults <file>  --output <dir>   relative paths resolve against the repository root
      --self-test      run the built-in assertions
    """;

var (opts, parseError) = ParseArgs(args);
if (opts is null)
{
    Console.Error.WriteLine($"ERROR: {parseError}");
    Console.Error.WriteLine(Usage);
    return 2;
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

if (opts.SelfTest) return RunSelfTest();

var scriptPath = AppContext.GetData("EntryPointFilePath") as string;
if (scriptPath is null)
{
    Console.Error.WriteLine("ERROR: cannot determine the script's own path; run it with 'dotnet run --file <path>'.");
    return 1;
}
var scriptRoot = GitTopLevel(Path.GetDirectoryName(Path.GetFullPath(scriptPath))!);
var cwdRoot = GitTopLevel(Directory.GetCurrentDirectory());
var rootError = CheckRoots(scriptRoot, cwdRoot, Directory.GetCurrentDirectory());
if (rootError is not null)
{
    Console.Error.WriteLine($"ERROR: {rootError}");
    return 1;
}
var repoRoot = scriptRoot!;

var srcDir = Path.GetFullPath(opts.Src, repoRoot);
var defaultsFile = Path.GetFullPath(opts.Defaults, repoRoot);
var outputDir = Path.GetFullPath(opts.Output, repoRoot);

if (!File.Exists(defaultsFile))
{
    Console.Error.WriteLine($"ERROR: Defaults file not found: {defaultsFile}");
    return 1;
}

var defaults = LoadDefaults(defaultsFile);
Console.WriteLine($"Loaded {defaults.Rules.Length} rules from {defaultsFile}");

return Generate(srcDir, defaults, outputDir, opts.Full, opts.DryRun, repoRoot, () => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture), Console.Out);

// ─── Arguments and repository root ──────────────────────────────────────────

(Options? Options, string? Error) ParseArgs(string[] argv)
{
    string src = "src", defs = ".github/coverage-manifest/defaults.json", output = ".github/coverage-manifest";
    bool full = false, appendOnly = false, dryRun = false, self = false;

    for (int i = 0; i < argv.Length; i++)
    {
        switch (argv[i])
        {
            case "--src" or "--defaults" or "--output":
                if (i + 1 >= argv.Length) return (null, $"{argv[i]} needs a value.");
                var value = argv[++i];
                if (argv[i - 1] == "--src") src = value;
                else if (argv[i - 1] == "--defaults") defs = value;
                else output = value;
                break;
            case "--full": full = true; break;
            case "--append-only": appendOnly = true; break;
            case "--dry-run": dryRun = true; break;
            case "--self-test": self = true; break;
            default: return (null, $"unknown argument '{argv[i]}'.");
        }
    }

    if (full && appendOnly) return (null, "--full and --append-only are mutually exclusive.");
    return (new Options(src, defs, output, full, dryRun, self), null);
}

string? GitTopLevel(string dir)
{
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("rev-parse");
        psi.ArgumentList.Add("--show-toplevel");
        using var p = System.Diagnostics.Process.Start(psi)!;
        var text = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return p.ExitCode == 0 && text.Length > 0 ? Path.GetFullPath(text) : null;
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { return null; }
}

// Returns an error message when the script's checkout and the current directory's checkout differ.
string? CheckRoots(string? scriptTop, string? cwdTop, string cwd)
{
    if (scriptTop is null) return "this script is not inside a git checkout.";
    if (cwdTop is null)
        return $"the current directory '{cwd}' is not inside a git checkout; the script belongs to '{scriptTop}'. Run it from inside that checkout.";
    var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    if (!string.Equals(Path.TrimEndingDirectorySeparator(scriptTop), Path.TrimEndingDirectorySeparator(cwdTop), cmp))
        return $"checkout mismatch: the script belongs to '{scriptTop}' but the current directory is in '{cwdTop}'. Run the script of the checkout you want to change from inside that checkout.";
    return null;
}

// ─── Generation ─────────────────────────────────────────────────────────────

DefaultsConfig LoadDefaults(string file) =>
    JsonSerializer.Deserialize<DefaultsConfig>(File.ReadAllText(file), jsonOpts)!;

int Generate(string src, DefaultsConfig cfg, string outDir, bool full, bool dryRun, string root, Func<string> now, TextWriter log)
{
    var append = !full;
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

    int failed = 0;
    var pending = new List<PendingWrite>();

    foreach (var (pkg, files) in packageFiles.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        var outputFile = Path.Combine(outDir, $"{pkg}.json");
        var sorted = files.OrderBy(f => f.Path, StringComparer.Ordinal).ToList();

        JsonObject? existing = null;
        if (File.Exists(outputFile))
        {
            try
            {
                existing = JsonNode.Parse(File.ReadAllText(outputFile), null,
                    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
                Materialize(existing); // JsonObject is lazy: walking the whole tree surfaces a duplicate key anywhere as ArgumentException
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException) { existing = null; }

            if (existing is null)
            {
                // Overwriting would lose whatever the file holds: skip it and fail the run.
                Console.Error.WriteLine($"ERROR: {outputFile} exists but is invalid JSON, not a JSON object, or has a duplicate key; skipped.");
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
            continue;

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
        pending.Add(new PendingWrite(pkg, outputFile, manifest.ToJsonString(writeOpts) + "\n", existing is null, added, removed, changed));
    }

    // Plan, printed before anything is written.
    var mode = full ? "full (rewrites every manifest it touches)" : "append-only (never rewrites an existing entry)";
    var (nNew, nChanged, nAdded, nRemoved, nRecomputed) = Totals(pending);
    log.WriteLine($"\nRepository root: {root}");
    log.WriteLine($"Mode: {mode}{(dryRun ? ", dry run" : "")}");
    log.WriteLine($"Plan: {nNew} manifest(s) to create, {nChanged} to change; entries: {nAdded} added, {nRemoved} removed, {nRecomputed} recomputed");
    foreach (var w in pending)
    {
        if (w.IsNew) log.WriteLine($"  {w.Package}: new manifest, {w.Added.Count} file(s)");
        else if (w.Added.Count > 0) log.WriteLine($"  {w.Package}: added {w.Added.Count}: {string.Join(", ", w.Added)}");
        if (w.Removed.Count > 0) log.WriteLine($"  {w.Package}: removed {w.Removed.Count}: {string.Join(", ", w.Removed)}");
        if (w.Changed.Count > 0)
            log.WriteLine($"  {w.Package}: recomputed {w.Changed.Count} entr{(w.Changed.Count == 1 ? "y" : "ies")} whose defaultTests/defaultRule/reason differed from the rules (move a deliberate classification into \"override\"): "
                + string.Join(", ", w.Changed.Take(10)) + (w.Changed.Count > 10 ? $", +{w.Changed.Count - 10} more" : ""));
    }

    if (dryRun)
        log.WriteLine("Dry run: nothing written.");
    else
    {
        Directory.CreateDirectory(outDir);
        foreach (var w in pending) File.WriteAllText(w.File, w.Text);
        log.WriteLine($"Written: {nNew} manifest(s) created, {nChanged} changed; entries: {nAdded} added, {nRemoved} removed, {nRecomputed} recomputed in {outDir}");
    }

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

(int New, int Changed, int Added, int Removed, int Recomputed) Totals(List<PendingWrite> writes) =>
    (writes.Count(w => w.IsNew), writes.Count(w => !w.IsNew),
     writes.Sum(w => w.Added.Count), writes.Sum(w => w.Removed.Count), writes.Sum(w => w.Changed.Count));

void Materialize(JsonNode? node)
{
    if (node is JsonObject obj)
        foreach (var (_, child) in obj) Materialize(child);
    else if (node is JsonArray arr)
        foreach (var child in arr) Materialize(child);
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
    int at = snapshot.FindIndex(kv => string.CompareOrdinal(kv.Key, key) > 0);
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
        Generate(Path.Combine(root, "src"), cfg, outDir, true, false, root, () => "T1", quiet);
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
        Generate(Path.Combine(root, "src"), cfg, outDir, true, false, root, () => "T1", quiet);
        Check(File.ReadAllText(manifestPath) == bytes1, "full: regenerating is byte-identical");

        // 3. Append-only adds B.cs, touches nothing else, keeps Gone.cs.
        File.WriteAllText(manifestPath, seed);
        Generate(Path.Combine(root, "src"), cfg, outDir, false, false, root, () => "T2", quiet);
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
        Generate(Path.Combine(root, "src"), cfg, outDir, false, false, root, () => "T3", quiet);
        Check(File.ReadAllText(manifestPath) == bytes2, "append-only: nothing missing leaves the file byte-identical");

        // 5. A duplicate key anywhere in an existing manifest skips that package (exit 1, file untouched); others still run.
        var src3 = Path.Combine(root, "src3");
        var out3 = Path.Combine(root, "out3");
        foreach (var p in new[] { "PkgX", "PkgY", "PkgZ" })
        {
            Directory.CreateDirectory(Path.Combine(src3, p));
            File.WriteAllText(Path.Combine(src3, p, "A.cs"), "class A {}");
        }
        Directory.CreateDirectory(out3);
        var dupEntry = """{"package":"PkgX","files":{"A.cs":{"defaultTests":["unit"],"defaultTests":["guard"],"reason":"x"}}}""";
        var dupTargets = """{"package":"PkgY","targets":{"unit":1,"unit":2},"files":{}}""";
        File.WriteAllText(Path.Combine(out3, "PkgX.json"), dupEntry);
        File.WriteAllText(Path.Combine(out3, "PkgY.json"), dupTargets);
        var dupLog = Console.Error;
        int rc;
        try { Console.SetError(new StringWriter()); rc = Generate(src3, cfg, out3, true, false, root, () => "T4", quiet); }
        finally { Console.SetError(dupLog); }
        Check(rc == 1, "duplicate keys: exit code 1");
        Check(File.ReadAllText(Path.Combine(out3, "PkgX.json")) == dupEntry, "duplicate key inside an entry: file untouched");
        Check(File.ReadAllText(Path.Combine(out3, "PkgY.json")) == dupTargets, "duplicate key inside targets: file untouched");
        Check(File.Exists(Path.Combine(out3, "PkgZ.json")), "duplicate keys: other packages still generated");

        // 6. Ordering is ordinal, not culture-sensitive ('B' sorts before 'a' ordinally; the reverse under most cultures).
        var src4 = Path.Combine(root, "src4");
        var out4 = Path.Combine(root, "out4");
        Directory.CreateDirectory(Path.Combine(src4, "PkgO"));
        Directory.CreateDirectory(out4);
        foreach (var n in new[] { "a.cs", "B.cs", "c.cs" })
            File.WriteAllText(Path.Combine(src4, "PkgO", n), "class C {}");
        Generate(src4, cfg, out4, true, false, root, () => "T5", quiet);
        var ordKeys = JsonNode.Parse(File.ReadAllText(Path.Combine(out4, "PkgO.json")))!["files"]!.AsObject().Select(kv => kv.Key);
        Check(ordKeys.SequenceEqual(["B.cs", "a.cs", "c.cs"]), "full: entries ordered ordinally");
        File.WriteAllText(Path.Combine(out4, "PkgO.json"),
            """{"package":"PkgO","generated":"G","totalFiles":2,"files":{"B.cs":{"defaultTests":["unit"]},"c.cs":{"defaultTests":["unit"]}}}""");
        Generate(src4, cfg, out4, false, false, root, () => "T6", quiet);
        var ordKeys2 = JsonNode.Parse(File.ReadAllText(Path.Combine(out4, "PkgO.json")))!["files"]!.AsObject().Select(kv => kv.Key);
        Check(ordKeys2.SequenceEqual(["B.cs", "a.cs", "c.cs"]), "append-only: new entry inserted at its ordinal position");

        // 7. The default mode never rewrites: a drifted entry survives, and only --full recomputes it.
        var drift = """{"package":"PkgO","generated":"G","totalFiles":3,"files":{"B.cs":{"defaultTests":["guard"],"defaultRule":"x","reason":"x"},"a.cs":{"defaultTests":["guard"],"defaultRule":"x","reason":"x"},"c.cs":{"defaultTests":["guard"],"defaultRule":"x","reason":"x"}}}""";
        var pkgOPath = Path.Combine(out4, "PkgO.json");
        File.WriteAllText(pkgOPath, drift);
        Generate(src4, cfg, out4, false, false, root, () => "T7", quiet);
        Check(File.ReadAllText(pkgOPath) == drift, "default: drifted entries are left untouched");
        Generate(src4, cfg, out4, true, true, root, () => "T7", quiet);
        Check(File.ReadAllText(pkgOPath) == drift, "full + dry-run: writes nothing");
        var dryText = new StringWriter();
        Generate(src4, cfg, out4, true, true, root, () => "T7", dryText);
        Check(dryText.ToString().Contains("Repository root: " + root) && dryText.ToString().Contains("0 manifest(s) to create, 1 to change"),
            "dry-run: prints the root and the plan counts");
        Generate(src4, cfg, out4, true, false, root, () => "T7", quiet);
        Check(File.ReadAllText(pkgOPath) != drift, "full: rewrites the drifted entries");

        // 8. Arguments: unknown flags and conflicts are errors; the default is append-only.
        Check(ParseArgs(["--ful"]).Options is null, "args: unknown flag fails");
        Check(ParseArgs(["--append-onyl"]).Options is null, "args: misspelled flag fails");
        Check(ParseArgs(["--src"]).Options is null, "args: option without value fails");
        Check(ParseArgs(["--full", "--append-only"]).Options is null, "args: --full with --append-only fails");
        Check(ParseArgs([]).Options is { Full: false, DryRun: false }, "args: no flag means append-only, not dry run");
        Check(ParseArgs(["--append-only"]).Options is { Full: false }, "args: --append-only accepted");
        Check(ParseArgs(["--full", "--dry-run"]).Options is { Full: true, DryRun: true }, "args: --full and --dry-run parsed");

        // 9. Checkout mismatch fails naming both paths; equal roots pass.
        var mismatch = CheckRoots(Path.Combine(root, "one"), Path.Combine(root, "two"), root);
        Check(mismatch is not null && mismatch.Contains(Path.Combine(root, "one")) && mismatch.Contains(Path.Combine(root, "two")),
            "root: mismatch fails naming both paths");
        Check(CheckRoots(Path.Combine(root, "one"), null, root) is not null, "root: current directory outside git fails");
        Check(CheckRoots(null, Path.Combine(root, "one"), root) is not null, "root: script outside git fails");
        Check(CheckRoots(Path.Combine(root, "one"), Path.Combine(root, "one") + Path.DirectorySeparatorChar, root) is null, "root: same checkout passes");
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

record Options(string Src, string Defaults, string Output, bool Full, bool DryRun, bool SelfTest);

record PendingWrite(string Package, string File, string Text, bool IsNew, List<string> Added, List<string> Removed, List<string> Changed);

record FileEntry(string Path, string[] Tests, string Rule, string Reason);
