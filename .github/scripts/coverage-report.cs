// coverage-report.cs — Encina Weighted Coverage Analysis
#:package Microsoft.CodeAnalysis.CSharp
#pragma warning disable CA1305, CA1310, CA1852 // Globalization/sealed warnings not relevant for standalone scripts
// Reads Cobertura XML reports from multiple test types and generates
// a weighted coverage report where only applicable test types count per package.
//
// Usage: dotnet run .github/scripts/coverage-report.cs -- [--output <dir>] [--input <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-stale-manifest [--manifest <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-missing-manifest [--manifest <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-justifications [--manifest <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-unmeasured [--input <dir>] [--output <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-flag-assignment [--min-lines <n>] [--input <dir>] [--output <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --files-from <list> [--input <dir>] [--output <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-targets [--files-from <list>] [--enforce] [--require-all-flags] [--input <dir>] [--output <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --check-zeros [--enforce] [--manifest <dir>] [--output <dir>]
//        dotnet run .github/scripts/coverage-report.cs -- --classify-zero <source-file> <unit|guard|contract>
//        dotnet run .github/scripts/coverage-report.cs -- --self-test
//
// --check-zeros (#2890) lists every per-file target of 0 on unit, guard or contract whose source file the closed
// list of constructs (ZeroClassifier below, decided on the issue 2026-10-26) does not exempt, with the construct it
// found, and writes <output>/check-zeros.json. The classification is a Roslyn SYNTAX pass (no compilation, no
// coverage data). Report-only (exit 0) unless --enforce is given, which exits 1 when any zero is rejected or
// unreadable. integration and property zeros are outside the rule. Not part of --check-justifications yet: the
// dry run on main still lists rejected zeros (see docs/testing/coverage-measurement-methodology.md).
// --classify-zero prints "exempt: <construct>" (exit 0) or "not exempt: <reason>" (exit 1) for one file and flag;
// a bad argument exits 2. It is the shared code path of the R-ZERO ratchet (#2898).
//
// --check-targets (#1651) judges the per-file targets of the manifest against the measured Cobertura data and
// writes <output>/check-targets.json. It is report-only (prints the findings, exit 0) unless --enforce is given,
// which exits 1 when there is any finding: (a) a file is below the target of a flag; (b) a target has no data
// where it is required (the flag ran but has no data for the file; with --require-all-flags, every flag
// is required, as in the nightly run, otherwise only flags that have a Cobertura file in the run are); (c) a
// Cobertura file could not be parsed (recorded in every mode); (d) with --files-from, a touched src/ file that
// still exists has no per-file target in its package manifest, or a touched src/ C# path has characters the
// check cannot read (dropped-path). --enforce and --require-all-flags need --check-targets.
//
// --files-from (#3289) reads a list of repository paths, one per line (the files a pull request touches, from
// the GitHub API), and also writes <output>/pr-coverage-comment.md: the "Per-file targets" rows of those files
// only. Names that are not plain src/ C# paths are ignored; with no src/ file in the list nothing is written.
//
// --check-flag-assignment (#2932) lists, per package and flag, the source files whose lines that flag's tests
// execute although the manifest does not assign the flag to the file (override ?? defaultTests; a per-file
// target does not assign, it is only noted on the row), with the executed line count. It writes <output>/flag-assignment.json and exits 1 when
// any such file executes at least --min-lines lines (default 5); wiring it into CI is #2935.
//
// A package with a manifest but no measured lines is listed in the report and the summary JSON
// as "not measured" (measured: false, unmeasuredPackages), never as 0%. The report mode prints a
// warning for each; --check-unmeasured also writes the report and then exits 1 when any exists
// (#2827; wiring it into CI is #2896).
//
// Per-file obligations (#1762): a manifest file entry may carry "targets" ({ "<flag>": 0-100 }) and
// "justifications" ({ "<flag>": "<one sentence>" }); --check-justifications exits 1 when a per-file
// target has no justification or a justification has no target. The default report lists, per file
// with targets, the measured coverage against them (report only, no exit code).
//
// Requires: .NET 10+ (C# 14 file-based app)

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// ─── Configuration ───────────────────────────────────────────────────────────

var outputDir = "artifacts/coverage";
var inputDir = "artifacts/test-results";
var manifestDir = ".github/coverage-manifest";
var manifestDirExplicit = false;
var checkStaleManifest = false;
var checkMissingManifest = false;
var checkJustifications = false;
var checkUnmeasured = false;
var checkFlagAssignment = false;
var minLines = 5;
var selfTest = false;
var checkTargets = false;
var enforce = false;
var requireAllFlags = false;
string? filesFrom = null;
var checkZeros = false;
string? classifyFile = null;
string? classifyFlag = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--check-zeros") checkZeros = true;
    if (args[i] == "--classify-zero")
    {
        // Both values are required: a missing one must be an argument error, never a silent no-op (#2890).
        if (i + 2 >= args.Length)
        {
            Console.WriteLine("\nERROR: --classify-zero needs a source file and a flag (unit, guard or contract).");
            Environment.Exit(2);
        }
        classifyFile = args[++i];
        classifyFlag = args[++i];
        continue;
    }
    if (args[i] == "--files-from")
    {
        // A missing value must never silently disable the touched-file check of --enforce (#3860).
        if (i + 1 >= args.Length)
        {
            Console.WriteLine("\nERROR: --files-from needs a file path.");
            Environment.Exit(1);
        }
        filesFrom = args[++i];
    }
    if (args[i] == "--check-flag-assignment") checkFlagAssignment = true;
    if (args[i] == "--min-lines" && (i + 1 >= args.Length || !int.TryParse(args[++i], NumberStyles.None, CultureInfo.InvariantCulture, out minLines) || minLines < 1))
    {
        Console.WriteLine("\nERROR: --min-lines needs a whole number of at least 1.");
        Environment.Exit(1);
    }
    if (args[i] == "--check-justifications") checkJustifications = true;
    if (args[i] == "--self-test") selfTest = true;
    // A value-taking flag with no value is an argument error, never a silent fall back to the default (#2890).
    if (args[i] is "--output" or "--input" or "--manifest" && i + 1 >= args.Length)
    {
        Console.WriteLine($"\nERROR: {args[i]} needs a value.");
        Environment.Exit(1);
    }
    if (args[i] == "--output" && i + 1 < args.Length) outputDir = args[++i];
    if (args[i] == "--input" && i + 1 < args.Length) inputDir = args[++i];
    if (args[i] == "--manifest" && i + 1 < args.Length) { manifestDir = args[++i]; manifestDirExplicit = true; }
    if (args[i] == "--check-stale-manifest") checkStaleManifest = true;
    if (args[i] == "--check-missing-manifest") checkMissingManifest = true;
    if (args[i] == "--check-unmeasured") checkUnmeasured = true;
    if (args[i] == "--check-targets") checkTargets = true;
    if (args[i] == "--enforce") enforce = true;
    if (args[i] == "--require-all-flags") requireAllFlags = true;
}

if (selfTest)
    Environment.Exit(RunSelfTest());

// --classify-zero (#2890) reads one source file and nothing else, so it runs before any manifest is loaded.
if (classifyFile is not null)
{
    if (checkZeros || checkTargets || checkJustifications || checkStaleManifest || checkMissingManifest || checkUnmeasured || checkFlagAssignment)
    {
        Console.WriteLine("\nERROR: --classify-zero cannot be combined with another mode; run it as its own step.");
        Environment.Exit(2);
    }
    Environment.Exit(RunClassifyZero(classifyFile, classifyFlag!));
}

// Categories removed — all configuration comes from per-package manifests

// ─── Load per-file manifest ─────────────────────────────────────────────────

// manifest[package][relFilePath] = applicable TestType flags
var manifest = new Dictionary<string, Dictionary<string, TestType>>(StringComparer.OrdinalIgnoreCase);
// Per-package per-flag targets from manifest (e.g., { "unit": 85, "guard": 70 })
var manifestTargets = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
// Manifest files that failed to parse: the default report mode only warns and moves on (a
// package simply loses its manifest-driven weighting), but --check-stale-manifest cannot
// afford that — a JSON error would silently drop that file's keys from the check and let a
// genuinely stale entry pass undetected.
var manifestParseFailures = new List<string>();
// Per-file obligations (#1762): fileObligations[package][manifestKey] = (targets, justifications),
// only for entries that carry at least one of the two.
var fileObligations = new Dictionary<string, Dictionary<string, ManifestFileEntry>>(StringComparer.OrdinalIgnoreCase);
// Per package: flag (lower case) -> number of files expecting it (#2876).
var expectedFlags = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

// Try to find manifest directory. An explicitly passed --manifest that does not exist is a
// caller error, not a hint to go looking elsewhere: silently falling back to the ancestor
// search would let a misspelled or wrong --manifest path resolve to a different, unintended
// manifest directory (or the real repo one) and check that instead, defeating the "cannot pass
// vacuously" guarantee every check mode below documents. The ancestor search only runs when
// --manifest was omitted (the default relative path not existing because cwd isn't the repo
// root). This applies to every mode (default report, --check-stale-manifest,
// --check-missing-manifest) — the caller made the same mistake regardless of mode.
if (manifestDirExplicit && !Directory.Exists(manifestDir))
{
    Console.WriteLine($"\nERROR: --manifest directory '{manifestDir}' does not exist.");
    Environment.Exit(1);
}

if (!manifestDirExplicit && !Directory.Exists(manifestDir))
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
            var pkgExpected = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
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
                CountExpectedFlags(entry, pkgExpected, mJson.Package);

                if (entry.Targets is { Count: > 0 } || entry.Justifications is { Count: > 0 })
                {
                    if (!fileObligations.TryGetValue(mJson.Package, out var pkgObligations))
                        fileObligations[mJson.Package] = pkgObligations = new Dictionary<string, ManifestFileEntry>(StringComparer.OrdinalIgnoreCase);
                    pkgObligations[filePath] = entry;
                }
            }
            manifest[mJson.Package] = pkgFiles;
            // Merged only after the whole file parsed, so a parse failure adds no spurious "no target" lines.
            foreach (var (pkg, counts) in pkgExpected) expectedFlags[pkg] = counts;

            // Store per-flag targets if present
            if (mJson.Targets is { Count: > 0 })
                manifestTargets[mJson.Package] = mJson.Targets;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  WARNING: Failed to parse manifest {Path.GetFileName(mFile)}: {ex.Message}");
            manifestParseFailures.Add(Path.GetFileName(mFile));
        }
    }
    Console.WriteLine($"Loaded {manifest.Count} package manifests with {manifest.Values.Sum(p => p.Count)} file entries");
}
else
{
    Console.WriteLine($"WARNING: Manifest directory '{manifestDir}' not found. Using category-level weights only.");
}

// ─── Check for stale manifest keys (#1497) and/or missing manifest entries (#1536) ──────────
// A manifest key is stale when its source file was deleted but the entry was not removed;
// a source file is missing from the manifest when it has no key at all. Both checks share the
// same key/path resolution as BuildDocRefIndex below (src/<Package>/<relPath>), except the base
// directory: BuildDocRefIndex runs only from the default report mode, which assumes the current
// directory is the repo root, but these modes are meant to run standalone and early, so they
// resolve src/ against the repo root derived from the located manifest directory (its parent's
// parent) instead of the current directory — the same directory independent of the process's
// cwd. Neither needs coverage data or a build. The two flags can be passed together (CI does):
// both run, and the process exits 1 if either found a problem, not just whichever ran last.
// --check-unmeasured needs coverage data, the others do not and return early: combining them would
// silently skip the unmeasured check, so it is rejected (#2827).
if (checkUnmeasured && (checkStaleManifest || checkMissingManifest || checkJustifications))
{
    Console.WriteLine("\nERROR: --check-unmeasured cannot be combined with --check-stale-manifest, --check-missing-manifest or --check-justifications; run it as its own step.");
    Environment.Exit(1);
}

// --check-flag-assignment (#2932) also needs coverage data and has its own exit code, so it runs alone.
if (checkFlagAssignment && (checkUnmeasured || checkStaleManifest || checkMissingManifest || checkJustifications))
{
    Console.WriteLine("\nERROR: --check-flag-assignment cannot be combined with another --check-* mode; run it as its own step.");
    Environment.Exit(1);
}

// --check-targets (#1651) also needs coverage data and has its own exit code, so it runs alone; its two
// modifiers mean nothing without it, and a silent no-op --enforce would read as a passing gate.
if (checkTargets && (checkUnmeasured || checkFlagAssignment || checkStaleManifest || checkMissingManifest || checkJustifications))
{
    Console.WriteLine("\nERROR: --check-targets cannot be combined with another --check-* mode; run it as its own step.");
    Environment.Exit(1);
}

// --check-zeros (#2890) reads the manifests and the source files only, has its own exit code and runs alone.
if (checkZeros && (checkTargets || checkUnmeasured || checkFlagAssignment || checkStaleManifest || checkMissingManifest || checkJustifications))
{
    Console.WriteLine("\nERROR: --check-zeros cannot be combined with another --check-* mode; run it as its own step.");
    Environment.Exit(1);
}

if (requireAllFlags && !checkTargets)
{
    Console.WriteLine("\nERROR: --require-all-flags only applies to --check-targets.");
    Environment.Exit(1);
}

if (enforce && !checkTargets && !checkZeros)
{
    Console.WriteLine("\nERROR: --enforce only applies to --check-targets and --check-zeros.");
    Environment.Exit(1);
}

if (checkZeros)
    Environment.Exit(RunCheckZeros(fileObligations, manifest.Count, manifestParseFailures,
        Path.GetFullPath(Path.Combine(manifestDir, "..", "..")), enforce, outputDir));

if (checkStaleManifest || checkMissingManifest || checkJustifications)
{
    // A gate that fails CI when a manifest entry is stale or missing must not pass vacuously
    // when it never actually read a manifest (an empty --manifest directory, or the default
    // path/ancestor search finding nothing): that would hide the real check behind a silent
    // green run. Fail loudly and distinctly from "found manifests, nothing wrong." An
    // explicitly wrong --manifest path is caught earlier and separately (see manifestDirExplicit
    // above) so it cannot silently fall back to a different directory; this guard only covers
    // "a manifest directory was located but had nothing usable in it." A manifest directory
    // whose files all failed to parse is reported separately below, with the files named, not
    // folded into this case.
    if (manifest.Count == 0 && manifestParseFailures.Count == 0)
    {
        Console.WriteLine($"\nERROR: no coverage manifest was loaded from '{manifestDir}' (or any ancestor directory). Nothing was checked.");
        Environment.Exit(1);
    }

    var manifestDirFull = Path.GetFullPath(manifestDir);
    var repoRoot = Directory.GetParent(manifestDirFull)?.Parent?.FullName ?? Directory.GetCurrentDirectory();

    var failed = false;
    if (checkStaleManifest)
        failed |= RunCheckStaleManifest(manifest, manifestParseFailures, repoRoot);
    if (checkMissingManifest)
        failed |= RunCheckMissingManifest(manifest, manifestParseFailures, repoRoot, manifestDirFull);
    if (checkJustifications)
        failed |= RunCheckJustifications(fileObligations, manifestParseFailures, expectedFlags, manifestTargets);

    if (failed)
        Environment.Exit(1);

    return;
}

// Per-file obligations (#1762): every per-file target needs a one-sentence justification, every
// justification needs its target, flag names are the five known ones and targets are whole numbers
// 0-100. Returns true (failed) when there is anything to report.
bool RunCheckJustifications(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations,
    List<string> manifestParseFailures,
    Dictionary<string, Dictionary<string, int>> expectedFlags,
    Dictionary<string, Dictionary<string, double>> packageTargets)
{
    var (problems, checkedTargets) = FindJustificationProblems(fileObligations);
    problems.AddRange(FindMissingPackageTargets(expectedFlags, packageTargets));

    if (manifestParseFailures.Count > 0)
    {
        Console.WriteLine($"\nMANIFEST PARSE FAILURES ({manifestParseFailures.Count}): these files could not be parsed, so their per-file targets cannot be vouched for");
        foreach (var failedFile in manifestParseFailures)
            Console.WriteLine($"  - {failedFile}");
    }

    if (problems.Count > 0)
    {
        Console.WriteLine($"\nPER-FILE TARGET PROBLEMS ({problems.Count}): every per-file target needs a one-sentence justification and every justification a target");
        foreach (var problem in problems)
            Console.WriteLine($"  - {problem}");
    }

    if (manifestParseFailures.Count > 0 || problems.Count > 0)
        return true;

    Console.WriteLine($"\nAll {checkedTargets} per-file targets are justified.");
    return false;
}

static string[] ValidFlagNames() => ["unit", "guard", "contract", "property", "integration"];

static TestType ParseFlag(string name) => name.ToLowerInvariant() switch
{
    "unit" => TestType.Unit,
    "guard" => TestType.Guard,
    "contract" => TestType.Contract,
    "property" => TestType.Property,
    "integration" => TestType.Integration,
    _ => TestType.None
};

// Lower-cases the flag keys of a per-file map so "unit" and "Unit" are one flag everywhere (the check
// and the report share this map). Keys that collide after case-folding are listed in duplicates; the
// first one is kept, and --check-justifications rejects the manifest anyway.
static Dictionary<string, T> NormaliseFlagKeys<T>(Dictionary<string, T>? source, List<string> duplicates)
{
    var result = new Dictionary<string, T>(StringComparer.Ordinal);
    foreach (var (k, v) in source ?? [])
    {
        var folded = k.ToLowerInvariant();
        if (!result.TryAdd(folded, v)) duplicates.Add(folded);
    }
    return result;
}

// Adds one to the package's count of every flag this file expects: override ?? defaultTests, plus every
// per-file target key (a target is an obligation of its own). Flags fold to lower case; a flag counts once per file.
static void CountExpectedFlags(
    ManifestFileEntry entry, Dictionary<string, Dictionary<string, int>> expectedFlags, string package)
{
    var flags = (entry.Override ?? entry.DefaultTests ?? [])
        .Concat(entry.Targets?.Keys ?? Enumerable.Empty<string>())
        .Select(f => f.ToLowerInvariant())
        .Distinct();
    if (!expectedFlags.TryGetValue(package, out var counts))
        expectedFlags[package] = counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var flag in flags)
        counts[flag] = counts.GetValueOrDefault(flag) + 1;
}

// The closed-world check (#2876): a package with a file expecting a flag must have a package target for
// it. A package target for a flag no file expects is fine. Manifest data only.
static List<string> FindMissingPackageTargets(
    Dictionary<string, Dictionary<string, int>> expectedFlags,
    Dictionary<string, Dictionary<string, double>> packageTargets)
{
    var problems = new List<string>();
    foreach (var (package, counts) in expectedFlags.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        var keys = (packageTargets.TryGetValue(package, out var t) ? t.Keys : Enumerable.Empty<string>())
            .Select(k => k.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        foreach (var (flag, count) in counts.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            if (!keys.Contains(flag))
                problems.Add($"{package}: flag '{flag}' is expected by {count} file(s) and the package has no target for it");
    }
    return problems;
}

// Every per-file target needs a justification and the reverse; flags are the five known ones (compared
// case-insensitively, a case-duplicate key is a problem); a target is a whole number 0-100.
static (List<string> Problems, int CheckedTargets) FindJustificationProblems(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations)
{
    var validFlags = ValidFlagNames();
    var problems = new List<string>();
    var checkedTargets = 0;

    foreach (var (package, files) in fileObligations.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        foreach (var (key, entry) in files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var where = $"{package}: {key.Replace('\\', '/')}";
            var duplicateTargets = new List<string>();
            var duplicateJustifications = new List<string>();
            var targets = NormaliseFlagKeys(entry.Targets, duplicateTargets);
            var justifications = NormaliseFlagKeys(entry.Justifications, duplicateJustifications);

            foreach (var flag in duplicateTargets.Distinct().Order(StringComparer.Ordinal))
                problems.Add($"{where}: targets name flag '{flag}' more than once (keys that differ only by case)");
            foreach (var flag in duplicateJustifications.Distinct().Order(StringComparer.Ordinal))
                problems.Add($"{where}: justifications name flag '{flag}' more than once (keys that differ only by case)");

            foreach (var (flag, value) in targets.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                checkedTargets++;
                if (!validFlags.Contains(flag))
                    problems.Add($"{where}: target for unknown flag '{flag}' (valid: {string.Join(", ", validFlags)})");
                if (value < 0 || value > 100 || value != Math.Floor(value))
                    problems.Add($"{where}: target '{flag}' = {value.ToString(CultureInfo.InvariantCulture)} is not a whole number from 0 to 100");
                if (!justifications.TryGetValue(flag, out var text) || string.IsNullOrWhiteSpace(text))
                    problems.Add($"{where}: target '{flag}' has no justification");
            }

            foreach (var flag in justifications.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (!targets.ContainsKey(flag))
                    problems.Add($"{where}: justification for '{flag}' has no target");
            }
        }
    }

    return (problems, checkedTargets);
}

// Maps a Cobertura class filename to "src/<Package>/<path>", or null for a file outside src/. Handles
// the three shapes the collectors emit: relative to the repo ("src/Pkg/A.cs"), relative to a <source>
// that ends in /src/ ("Pkg/A.cs") and absolute ("C:/x/src/Pkg/A.cs").
static string? ToSrcRelativePath(string filename, string source)
{
    filename = filename.Replace('\\', '/');
    var src = source.Replace('\\', '/');
    var sourceEndsSrc = src.EndsWith("/src/", StringComparison.Ordinal) || src.EndsWith("/src", StringComparison.Ordinal);

    if (sourceEndsSrc && !filename.Contains("/src/") && !filename.StartsWith("src/"))
        filename = "src/" + filename;

    if (!filename.Contains("/src/") && !filename.StartsWith("src/")) return null;

    var srcIdx = filename.IndexOf("/src/", StringComparison.Ordinal);
    if (srcIdx < 0) srcIdx = filename.IndexOf("src/", StringComparison.Ordinal) - 1;
    return filename[(srcIdx + 1)..];
}

// Instrumented and covered line counts of one file under one flag; null when that flag has no data for it.
static (int Total, int Covered)? CountFileFlagLines(
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag, TestType flag, string sourcePath)
{
    if (flag != TestType.None && coverageByFlag.TryGetValue(flag, out var flagData) &&
        flagData.TryGetValue(sourcePath, out var lines) && lines.Count > 0)
        return (lines.Count, lines.Values.Count(h => h > 0));
    return null;
}

// Unrounded line coverage (0-100) of one file under one flag; null when that flag has no data for it.
static double? MeasureFileFlag(
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag, TestType flag, string sourcePath)
    => CountFileFlagLines(coverageByFlag, flag, sourcePath) is { } c ? c.Covered * 100.0 / c.Total : null;

// Largest line-set size of a file over all flags, or null when no flag has the file (#2542, D3). It lends
// a pair with no data its line count for weighting; the covered lines of another flag are never lent.
static int? FileLineCount(
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag, string sourcePath)
{
    int? best = null;
    foreach (var flagData in coverageByFlag.Values)
        if (flagData.TryGetValue(sourcePath, out var lines) && lines.Count > 0 && (best is null || lines.Count > best))
            best = lines.Count;
    return best;
}

// One row per (file, flag) with a per-file target. Measured comes straight from the Cobertura data of
// that flag, whatever the file's defaultTests say: a target is an obligation of its own.
static List<FileTargetRow> BuildFileTargetRows(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations,
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag)
{
    var rows = new List<FileTargetRow>();
    foreach (var (package, files) in fileObligations.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        foreach (var (key, entry) in files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var relPath = key.Replace('\\', '/');
            var sourcePath = $"src/{package}/{relPath}";
            var targets = NormaliseFlagKeys(entry.Targets, []);
            foreach (var (flagName, target) in targets.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                rows.Add(BuildFileTargetRow(package, relPath, flagName, target, coverageByFlag, sourcePath));
        }
    }
    return rows;
}

// A pair with data carries its own counts; a pair without data is 0% measured and borrows only the line
// count of the file's other flags (null when no flag has the file), never their covered lines.
static FileTargetRow BuildFileTargetRow(
    string package, string relPath, string flagName, double target,
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag, string sourcePath)
{
    if (CountFileFlagLines(coverageByFlag, ParseFlag(flagName), sourcePath) is { } c)
        return new FileTargetRow(package, relPath, flagName, target, c.Covered * 100.0 / c.Total, c.Total, c.Covered);
    var borrowed = FileLineCount(coverageByFlag, sourcePath);
    return new FileTargetRow(package, relPath, flagName, target, null, borrowed, borrowed is null ? null : 0);
}

// A target of 0 is always met; no data counts as below any target above 0. The measured value is
// compared unrounded, so 89.96% does not pass a target of 90.
static bool IsBelowTarget(FileTargetRow row)
    => row.Target > 0 && (row.Measured is null || row.Measured < row.Target);

// The status of one pair in the per-file table, latest.json and the dashboard (#2870): a target of 0 is a
// declared waiver, not a met obligation, so it has its own status; the aggregates still leave it out.
static string TargetStatus(FileTargetRow row)
    => row.Target == 0 ? "zero-target" : IsBelowTarget(row) ? "below" : "met";

// ─── Target check (#1651): --check-targets ───────────────────────────────────────────────────────────

// The finding of one (file, flag) pair with a target, or null when it is met. A pair below its target is
// always a finding. A pair with no data is one only where the flag is required: it ran in this run, or
// requireAllFlags says every flag is required (the nightly); otherwise the flag simply did not run here.
static TargetFinding? CheckTargetRow(FileTargetRow row, IReadOnlyCollection<TestType> flagsInRun, bool requireAllFlags)
{
    if (!IsBelowTarget(row)) return null;
    var subject = $"{row.Package}/{row.File} {row.Flag}";
    if (row.Measured is { } measured)
        return new TargetFinding("below-target", subject, $"{FormatMeasured(measured)} is below the target of {row.Target:0}%");
    if (!requireAllFlags && !flagsInRun.Contains(ParseFlag(row.Flag))) return null;
    return new TargetFinding("no-data", subject, $"no coverage data for a target of {row.Target:0}%");
}

// All findings of the check, in a stable order. A touched file counts only while it still exists (a deleted
// file has nothing to cover); touched is null without --files-from.
static List<TargetFinding> FindTargetProblems(
    IReadOnlyList<FileTargetRow> rows, IReadOnlyCollection<TestType> flagsInRun, bool requireAllFlags,
    IReadOnlyList<string> coverageParseFailures, IReadOnlySet<string>? touched, Func<string, bool> sourceExists,
    int droppedSrcPaths = 0)
{
    var findings = coverageParseFailures.Order(StringComparer.Ordinal)
        .Select(f => new TargetFinding("parse-error", f, "the Cobertura report could not be parsed, so its flag cannot be vouched for"))
        .ToList();

    // No flag has a Cobertura report at all (an empty download, only unclassified directories): nothing can be
    // judged, which must never read as a pass.
    if (flagsInRun.Count == 0 && rows.Any(r => r.Target > 0))
        findings.Add(new TargetFinding("no-coverage", "input", "no Cobertura report of a known flag was found, so no target could be checked"));

    findings.AddRange(rows.Select(r => CheckTargetRow(r, flagsInRun, requireAllFlags)).OfType<TargetFinding>()
        .OrderBy(f => f.Subject, StringComparer.Ordinal));

    if (touched is not null)
    {
        var targeted = rows.Select(SourcePathOf).ToHashSet(StringComparer.Ordinal);
        findings.AddRange(touched.Order(StringComparer.Ordinal)
            .Where(t => !targeted.Contains(t) && sourceExists(t))
            .Select(t => new TargetFinding("no-targets", t, "a touched src/ file has no per-file target in its package manifest")));
    }

    // The path itself is not echoed: it is the very text the character filter refused.
    if (droppedSrcPaths > 0)
        findings.Add(new TargetFinding("dropped-path", "touched-files",
            $"{droppedSrcPaths} touched src/ C# path(s) have characters the check cannot read (only ASCII letters, digits and . / - _ + are accepted), so their targets were not checked"));

    return findings;
}

// Prints the findings, writes <outputDir>/check-targets.json and returns true (failed) only when enforcing
// with at least one finding.
static bool RunCheckTargets(
    IReadOnlyList<TargetFinding> findings, IReadOnlyList<FileTargetRow> rows, IReadOnlyCollection<TestType> flagsInRun,
    bool enforce, bool requireAllFlags, string outputDir)
{
    var flagNames = flagsInRun.Select(f => f.ToString().ToLowerInvariant()).Order(StringComparer.Ordinal).ToList();
    Console.WriteLine($"\n--check-targets: {rows.Count(r => r.Target > 0)} target(s) checked; flags with a Cobertura report: {(flagNames.Count > 0 ? string.Join(", ", flagNames) : "none")}{(requireAllFlags ? " (every flag required)" : "")}");
    foreach (var finding in findings)
        Console.WriteLine($"  {finding.Kind.ToUpperInvariant()}: {finding.Subject}: {finding.Message}");

    var payload = new
    {
        enforce,
        requireAllFlags,
        flagsInRun = flagNames,
        checkedTargets = rows.Count(r => r.Target > 0),
        passed = findings.Count == 0,
        findings
    };
    Directory.CreateDirectory(outputDir);
    File.WriteAllText(Path.Combine(outputDir, "check-targets.json"), JsonSerializer.Serialize(payload, SummaryJsonOptions()));

    if (findings.Count == 0)
    {
        Console.WriteLine("  No findings.");
        return false;
    }

    Console.WriteLine(enforce
        ? $"  FAIL: {findings.Count} finding(s)."
        : $"  {findings.Count} finding(s); report-only, pass --enforce to fail the run.");
    return enforce;
}

// ─── Zero targets (#2890): --check-zeros and --classify-zero ─────────────────────────────────────────

// Prints the verdict of one source file and flag; 0 exempt, 1 not exempt, 2 bad argument (for #2898).
static int RunClassifyZero(string file, string flag)
{
    if (!File.Exists(file))
    {
        Console.WriteLine("ERROR: the source file does not exist.");
        return 2;
    }
    if (!ZeroClassifier.CheckedFlags.Contains(flag.ToLowerInvariant()))
    {
        Console.WriteLine("ERROR: the flag must be unit, guard or contract.");
        return 2;
    }
    var verdict = ZeroClassifier.Classify(file, File.ReadAllText(file), flag);
    Console.WriteLine($"{(verdict.Exempt ? "exempt" : "not exempt")}: {verdict.Construct}");
    return verdict.Exempt ? 0 : 1;
}

// The per-file targets of 0 on unit, guard or contract in the manifests (flags folded to lower case),
// in a stable order: (package, manifest key, flag).
static List<(string Package, string Key, string Flag)> ZeroTargets(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations)
{
    var zeros = new List<(string, string, string)>();
    foreach (var (package, files) in fileObligations.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        foreach (var (key, entry) in files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            foreach (var (flag, value) in NormaliseFlagKeys(entry.Targets, []).OrderBy(kv => kv.Key, StringComparer.Ordinal))
                if (value == 0 && ZeroClassifier.CheckedFlags.Contains(flag))
                    zeros.Add((package, key, flag));
    return zeros;
}

// One finding per zero the closed list does not exempt (kind rejected-zero) or whose source file cannot be
// read from under src/ (kind no-source). The subject is "<package>: <key> <flag>" with control characters
// replaced, because it comes from a manifest key.
static (List<TargetFinding> Findings, int Checked) FindRejectedZeros(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations, string repoRoot)
{
    var zeros = ZeroTargets(fileObligations);
    var findings = new List<TargetFinding>();
    foreach (var (package, key, flag) in zeros)
    {
        var subject = ZeroClassifier.Safe($"{package}: {key.Replace('\\', '/')} {flag}");
        var text = ZeroClassifier.ReadSource(repoRoot, package, key);
        if (text is null)
        {
            findings.Add(new TargetFinding("no-source", subject, "the source file was not found under src/ or cannot be read"));
            continue;
        }
        var verdict = ZeroClassifier.Classify(key, text, flag);
        if (!verdict.Exempt)
            findings.Add(new TargetFinding("rejected-zero", subject, verdict.Construct));
    }
    return (findings, zeros.Count);
}

// Prints the findings by package, writes <outputDir>/check-zeros.json (deterministic: no clock, stable order)
// and returns the exit code: 1 only when enforcing with at least one finding, a manifest that did not parse,
// or no manifest at all (never a vacuous pass).
static int RunCheckZeros(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations, int manifestCount,
    List<string> manifestParseFailures, string repoRoot, bool enforce, string outputDir)
{
    var (findings, checkedZeros) = FindRejectedZeros(fileObligations, repoRoot);
    if (manifestCount == 0 || manifestParseFailures.Count > 0)
        findings.Add(new TargetFinding("no-manifest", "manifest",
            manifestParseFailures.Count > 0
                ? $"{manifestParseFailures.Count} coverage manifest(s) could not be parsed, so their zeros were not checked"
                : "no coverage manifest was loaded; nothing was checked"));

    var byPackage = findings.Where(f => f.Kind != "no-manifest")
        .GroupBy(f => f.Subject.Split(':')[0], StringComparer.Ordinal)
        .OrderBy(g => g.Key, StringComparer.Ordinal)
        .ToDictionary(g => g.Key, g => g.Count());

    Console.WriteLine($"\n--check-zeros: {checkedZeros} zero target(s) on unit, guard or contract; {findings.Count} finding(s)");
    foreach (var finding in findings)
        Console.WriteLine($"  {finding.Kind.ToUpperInvariant()}: {finding.Subject}: {finding.Message}");
    foreach (var (package, count) in byPackage)
        Console.WriteLine($"  {package}: {count}");

    var payload = new
    {
        enforce,
        checkedZeros,
        rejectedZeros = findings.Count(f => f.Kind != "no-manifest"),
        passed = findings.Count == 0,
        byPackage,
        findings
    };
    Directory.CreateDirectory(outputDir);
    File.WriteAllText(Path.Combine(outputDir, "check-zeros.json"), JsonSerializer.Serialize(payload, SummaryJsonOptions()));

    if (findings.Count == 0)
    {
        Console.WriteLine("  No findings.");
        return 0;
    }
    Console.WriteLine(enforce ? $"  FAIL: {findings.Count} finding(s)." : $"  {findings.Count} finding(s); report-only, pass --enforce to fail the run.");
    return enforce ? 1 : 0;
}

// ─── Target attainment (#2542): how close a (file, flag) pair is to its own target ───────────────────

// Percentage points still missing to the target; no data counts as 0% measured.
static double GapPoints(FileTargetRow row) => Math.Max(0, row.Target - (row.Measured ?? 0));

// Share of the target reached, capped at 1; a target of 0 is fully attained.
static double Attainment(FileTargetRow row)
    => row.Target <= 0 ? 1 : Math.Min((row.Measured ?? 0) / row.Target, 1);

// Lines still to cover to reach the target: max(0, ceil(target x total / 100) - covered). The target is
// multiplied by the line count before dividing, so a whole-number target gives an exact result.
static int LinesToCover(FileTargetRow row)
    => row.TotalLines is not { } total
        ? 0
        : Math.Max(0, (int)Math.Ceiling(row.Target * total / 100.0) - (row.CoveredLines ?? 0));

// Weight of a pair in the line-weighted value (target lines) and the credit it earns (covered lines, capped
// at the weight, so surplus in one file never offsets a deficit in another).
static (double Weight, double Credit) LineWeight(FileTargetRow row)
{
    var weight = row.Target * (row.TotalLines ?? 0) / 100.0;
    return (weight, Math.Min(row.CoveredLines ?? 0, weight));
}

// The aggregates over a set of pairs. Pairs with a target of 0 are left out of every aggregate and only
// counted (D4); a pair with no data stays in as 0% measured (D3) and is counted in NoData; a pair whose file
// has no line count under any flag is counted in the pair aggregates and listed in Unweighted.
// Ratio and MeanGap are null when there are no pairs: NaN cannot be written as JSON.
static AttainmentSummary SummariseAttainment(IReadOnlyList<FileTargetRow> rows)
{
    var demanding = rows.Where(r => r.Target > 0).ToList();
    var weighted = demanding.Where(r => r.TotalLines is > 0).Select(LineWeight).ToList();
    var denominator = weighted.Sum(w => w.Weight);
    var met = demanding.Count(r => !IsBelowTarget(r));
    return new AttainmentSummary(
        Pairs: demanding.Count,
        Met: met,
        Ratio: demanding.Count > 0 ? (double)met / demanding.Count : null,
        LineWeighted: denominator > 0 ? weighted.Sum(w => w.Credit) / denominator : null,
        MeanGap: demanding.Count > 0 ? demanding.Average(GapPoints) : null,
        LinesToCover: demanding.Sum(LinesToCover),
        NoData: demanding.Count(r => r.Measured is null),
        Unweighted: demanding.Count(r => r.TotalLines is not > 0),
        ZeroTarget: rows.Count - demanding.Count);
}

// The rows of the "Per-file targets" table and of latest.json "fileTargets", furthest from target first.
static List<FileTargetRow> OrderByGap(IEnumerable<FileTargetRow> rows)
    => rows.OrderByDescending(GapPoints)
        .ThenBy(r => r.Package, StringComparer.Ordinal)
        .ThenBy(r => r.File, StringComparer.Ordinal)
        .ThenBy(r => r.Flag, StringComparer.Ordinal)
        .ToList();

// The latest.json row of one pair (Design Choice 7); display values are rounded, status is the exact verdict.
static FileTargetEntry ToFileTargetEntry(FileTargetRow row) => new(
    row.Package, row.File, row.Flag, row.Target,
    row.Measured is { } m ? Math.Round(m, 2) : null,
    row.CoveredLines, row.TotalLines,
    Math.Round(GapPoints(row), 2), IsBelowTarget(row) ? Math.Floor(Attainment(row) * 10000 + 1e-9) / 10000 : Math.Round(Attainment(row), 4), LinesToCover(row),
    TargetStatus(row));

// The aggregates of one package's pairs, or null when the package has none.
static AttainmentSummary? PackageAttainment(IReadOnlyList<FileTargetRow> rows, string package)
{
    var own = rows.Where(r => r.Package == package).ToList();
    return own.Count > 0 ? SummariseAttainment(own) : null;
}

// Options of encina-coverage-summary.json: camelCase, nulls omitted.
static JsonSerializerOptions SummaryJsonOptions() => new()
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
};

static string FormatRatio(double? value) => value is { } v ? (v * 100).ToString("F1", CultureInfo.InvariantCulture) + "%" : "n/a";

static string FormatAttainmentSummary(AttainmentSummary s)
    => $"targets met {s.Met}/{s.Pairs} ({FormatRatio(s.Ratio)}) | line-weighted {FormatRatio(s.LineWeighted)} | " +
       $"mean gap {(s.MeanGap is { } g ? g.ToString("F1", CultureInfo.InvariantCulture) + " points" : "n/a")} | " +
       $"lines to cover {s.LinesToCover} | no data {s.NoData} | unweighted {s.Unweighted} | of which zero-target {s.ZeroTarget}";

// Display only: two decimals so a value just under the target never prints as the target.
static string FormatMeasured(double? measured)
    => measured is { } m ? m.ToString("F2", CultureInfo.InvariantCulture) + "%" : "no data";

// Display only: floored, never rounded, so a pair below its target cannot show 100.0% or a gap of 0.00.
static string FormatAttainment(double attainment)
    => (Math.Floor(attainment * 1000 + 1e-9) / 10).ToString("F1", CultureInfo.InvariantCulture) + "%";

static string FormatGap(double gap)
    => (Math.Floor(gap * 100 + 1e-9) / 100).ToString("F2", CultureInfo.InvariantCulture);

// ─── Pull request comment (#3289): the per-file targets of the files a PR touches ───────────────────

// The touched-file list is data from the GitHub API, but a file name is still attacker-chosen text that ends
// up in a comment, so only plain src/ C# paths are kept; anything else (markup, backticks, "..") is dropped.
static HashSet<string> ParseTouchedFiles(IEnumerable<string> lines)
{
    var touched = new HashSet<string>(StringComparer.Ordinal);
    foreach (var raw in lines)
    {
        var path = NormalizeTouchedLine(raw);
        if (IsSrcCSharpPath(path) && IsPlainPath(path))
            touched.Add(path);
    }
    return touched;
}

// How many lines name a src/ C# file that ParseTouchedFiles drops (a space, "$", "(", a non-ASCII letter, ".."
// ...). Under --check-targets each one is a finding: a file the check cannot read must never escape it (#3860).
static int CountDroppedSrcPaths(IEnumerable<string> lines)
    => lines.Select(NormalizeTouchedLine).Count(path => IsSrcCSharpPath(path) && !IsPlainPath(path));

static string NormalizeTouchedLine(string raw) => raw.Trim().Replace('\\', '/');

static bool IsSrcCSharpPath(string path)
    => path.StartsWith("src/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal);

static bool IsPlainPath(string path)
    => !path.Contains("..", StringComparison.Ordinal) &&
       path.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '/' or '-' or '_' or '+');

static string SourcePathOf(FileTargetRow row) => $"src/{row.Package}/{row.File}";

// The comment body: the "Per-file targets" rows of the touched files, furthest from target first, then the
// touched files that carry no per-file target. flagsWithData names the flags that have Cobertura data in the
// run, so a "no data" cell can be told apart from a flag that PR CI did not run.
static string RenderPullRequestComment(
    IReadOnlyList<FileTargetRow> allRows, HashSet<string> touched, IReadOnlyCollection<string> flagsWithData, int maxRows = 60)
{
    var rows = allRows.Where(r => touched.Contains(SourcePathOf(r))).ToList();
    var withTargets = rows.Select(SourcePathOf).ToHashSet(StringComparer.Ordinal);
    var untargeted = touched.Where(t => !withTargets.Contains(t)).Order(StringComparer.Ordinal).ToList();

    var sb = new StringBuilder();
    sb.AppendLine("### Coverage of the files this pull request touches");
    sb.AppendLine();
    sb.AppendLine("Measured by the pull request CI against the per-file targets of `.github/coverage-manifest`. " +
        "There is no project-wide percentage: each flag is judged against its own target.");
    sb.AppendLine();
    sb.AppendLine(flagsWithData.Count == 0
        ? "No flag produced coverage data in this run."
        : $"Flags with coverage data in this run: {string.Join(", ", flagsWithData.Order(StringComparer.Ordinal))}. " +
          "A pull request CI runs only the test projects whose inputs the change touches, and does not run everything CI Full runs " +
          "(the Observability integration shard and the SQLite EF Core job), so \"no data\" can mean the flag did not run here.");
    sb.AppendLine();

    if (rows.Count == 0)
        sb.AppendLine("None of the touched files has a per-file target.");
    else
    {
        var summary = SummariseAttainment(rows);
        sb.AppendLine($"Targets met: {summary.Met}/{summary.Pairs} ({FormatRatio(summary.Ratio)}); no data: {summary.NoData}; lines to cover: {summary.LinesToCover}.");
        sb.AppendLine();
        sb.AppendLine("| File | Flag | Measured | Target | Gap | Lines to cover | Status |");
        sb.AppendLine("|------|:----:|:--------:|:------:|:---:|:--------------:|:------:|");
        var ordered = OrderByGap(rows);
        foreach (var row in ordered.Take(maxRows))
            sb.AppendLine($"| `{row.Package}/{row.File}` | {row.Flag} | {FormatMeasured(row.Measured)} | {row.Target:0}% | {FormatGap(GapPoints(row))} | {LinesToCover(row)} | {(row.Measured is null && row.Target > 0 ? "no data" : TargetStatus(row))} |");
        if (ordered.Count > maxRows)
            sb.AppendLine($"\n{ordered.Count - maxRows} more rows are in the CI run's coverage report.");
    }

    if (untargeted.Count > 0)
    {
        sb.AppendLine();
        sb.AppendLine($"Touched files without a per-file target ({untargeted.Count}): " +
            string.Join(", ", untargeted.Take(20).Select(t => $"`{t}`")) + (untargeted.Count > 20 ? ", ..." : "") +
            ". AGENTS.md section 9 asks for targets in the package manifest for every file a pull request touches.");
    }

    return sb.ToString();
}

// ─── Flag assignment (#2932): files a flag executes but the manifest does not assign to it ──────────

// Flags that count as assigned to a file: its manifest entry (override ?? defaultTests, a file missing from
// the manifest gets the report's unit + guard fallback), the same resolution as the package row. A per-file
// target does NOT assign: it adds an obligation in the "Per-file targets" table, but the package row ignores
// the flag until it is in override or defaultTests (the Consent case, #2920). None means the manifest
// excludes the file on purpose (an empty override), as the default report does.
static TestType AssignedFlags(
    Dictionary<string, Dictionary<string, TestType>> manifest, string package, string relPath)
{
    var assigned = TestType.Unit | TestType.Guard;
    if (manifest.TryGetValue(package, out var files))
    {
        if (files.TryGetValue(relPath, out var byPath)) assigned = byPath;
        else if (files.TryGetValue(Path.GetFileName(relPath), out var byName)) assigned = byName;
    }
    return assigned;
}

// True when the file's entry has a per-file target for the flag (used only to annotate a reported row).
static bool HasFileTarget(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations,
    string package, string relPath, TestType flag)
{
    if (!fileObligations.TryGetValue(package, out var obligations) ||
        !(obligations.TryGetValue(relPath, out var entry) || obligations.TryGetValue(Path.GetFileName(relPath), out entry)))
        return false;
    return NormaliseFlagKeys(entry.Targets, []).Keys.Any(name => ParseFlag(name) == flag);
}

// Per (package, flag, file): the source files whose lines that flag's tests execute (hits > 0) although the
// manifest's override/defaultTests do not list the flag for the file, so the package row ignores them. A row
// notes when the file has a per-file target for the flag. Packages without a manifest, generated files and
// files the manifest excludes are skipped. Ordered by executed lines, largest first.
static List<UnassignedFlagFile> FindUnassignedFlagFiles(
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag,
    Dictionary<string, Dictionary<string, TestType>> manifest,
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations)
{
    var rows = new List<UnassignedFlagFile>();
    foreach (var (flag, flagData) in coverageByFlag)
    {
        if (flag == TestType.None) continue;
        foreach (var (path, lines) in flagData)
        {
            var parts = path.Split('/');
            if (parts.Length < 3 || parts[0] != "src") continue;
            if (path.Contains("/obj/", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) continue;
            if (!manifest.ContainsKey(parts[1])) continue;

            var executed = lines.Values.Count(h => h > 0);
            if (executed == 0) continue;

            var relPath = string.Join("/", parts[2..]);
            var assigned = AssignedFlags(manifest, parts[1], relPath);
            if (assigned == TestType.None || assigned.HasFlag(flag)) continue;

            rows.Add(new UnassignedFlagFile(parts[1], relPath, flag.ToString().ToLowerInvariant(), executed, lines.Count,
                HasFileTarget(fileObligations, parts[1], relPath, flag)));
        }
    }
    return rows.OrderByDescending(r => r.ExecutedLines)
        .ThenBy(r => r.Package, StringComparer.Ordinal)
        .ThenBy(r => r.Flag, StringComparer.Ordinal)
        .ThenBy(r => r.File, StringComparer.Ordinal)
        .ToList();
}

// Prints the table and writes flag-assignment.json to outputDir; returns true (failed) when any file executes
// at least minLines lines under a flag it is not assigned to.
static bool RunCheckFlagAssignment(List<UnassignedFlagFile> rows, int minLines, string outputDir)
{
    var failing = rows.Where(r => r.ExecutedLines >= minLines).ToList();
    Console.WriteLine($"\nFLAG ASSIGNMENT: {rows.Count} file/flag pair(s) execute lines under a flag the manifest does not assign; {failing.Count} at or above --min-lines {minLines}");
    if (rows.Count > 0)
    {
        Console.WriteLine($"  {"Package",-36} {"Flag",-12} {"Executed",8} {"Total",6}  File");
        foreach (var r in rows)
            Console.WriteLine($"  {r.Package,-36} {r.Flag,-12} {r.ExecutedLines,8} {r.TotalLines,6}  {r.File}{(r.HasFileTarget ? "  (has a per-file target, not in the package row)" : "")}{(r.ExecutedLines >= minLines ? "" : "  (below --min-lines)")}");
    }

    Directory.CreateDirectory(outputDir);
    var json = JsonSerializer.Serialize(new
    {
        minLines,
        pairs = rows.Count,
        failing = failing.Count,
        files = rows.Select(r => new { r.Package, r.Flag, r.File, r.ExecutedLines, r.TotalLines, r.HasFileTarget, failing = r.ExecutedLines >= minLines })
    }, SummaryJsonOptions());
    File.WriteAllText(Path.Combine(outputDir, "flag-assignment.json"), json);
    Console.WriteLine($"  JSON:     {Path.Combine(outputDir, "flag-assignment.json")}");

    if (failing.Count > 0)
    {
        Console.WriteLine("  Fix: add the flag to the file's override or defaultTests in the manifest when the tests are real (keep or add the per-file target and justification), or replace reflection-only tests.");
        return true;
    }
    Console.WriteLine("  No file reaches the threshold.");
    return false;
}

// Self-test (#1762): exercises the per-file obligation logic without touching the repository.
int RunSelfTest()
{
    var passed = 0;
    var failures = new List<string>();
    void Check(bool condition, string name)
    {
        if (condition) passed++;
        else failures.Add(name);
    }

    static Dictionary<string, Dictionary<string, ManifestFileEntry>> One(ManifestFileEntry entry)
        => new() { ["Pkg"] = new() { ["A.cs"] = entry } };
    static bool Reports(ManifestFileEntry entry, string fragment)
        => FindJustificationProblems(One(entry)).Problems.Any(p => p.Contains(fragment, StringComparison.Ordinal));

    // --- the justification check ---
    var good = new ManifestFileEntry
    {
        Targets = new() { ["unit"] = 90, ["guard"] = 0 },
        Justifications = new() { ["unit"] = "Pure logic.", ["guard"] = "No guard tests yet (#1825)." }
    };
    var goodResult = FindJustificationProblems(One(good));
    Check(goodResult.Problems.Count == 0 && goodResult.CheckedTargets == 2, "a justified manifest has no problems");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = 90 } }, "'unit' has no justification"), "a target without justifications is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = 90 }, Justifications = new() { ["guard"] = "x" } }, "'unit' has no justification"), "a target missing its own justification is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = 90 }, Justifications = new() { ["unit"] = "  " } }, "'unit' has no justification"), "a blank justification is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = 90 }, Justifications = new() { ["unit"] = "x", ["guard"] = "y" } }, "justification for 'guard' has no target"), "an orphan justification is reported");
    Check(Reports(new ManifestFileEntry { Justifications = new() { ["guard"] = "y" } }, "justification for 'guard' has no target"), "a justification with no targets at all is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = 150 }, Justifications = new() { ["unit"] = "x" } }, "not a whole number from 0 to 100"), "a target above 100 is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = -1 }, Justifications = new() { ["unit"] = "x" } }, "not a whole number from 0 to 100"), "a negative target is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["unit"] = 55.5 }, Justifications = new() { ["unit"] = "x" } }, "not a whole number from 0 to 100"), "a non-integer target is reported");
    Check(Reports(new ManifestFileEntry { Targets = new() { ["mutation"] = 50 }, Justifications = new() { ["mutation"] = "x" } }, "unknown flag 'mutation'"), "an unknown flag is reported");
    Check(FindJustificationProblems(One(new ManifestFileEntry { Targets = new() { ["Unit"] = 90 }, Justifications = new() { ["UNIT"] = "x" } })).Problems.Count == 0,"flag names match case-insensitively");

    var caseDuplicates = new ManifestFileEntry
    {
        Targets = new() { ["unit"] = 90, ["Unit"] = 80 },
        Justifications = new() { ["unit"] = "x", ["Unit"] = "y" }
    };
    Check(Reports(caseDuplicates, "targets name flag 'unit' more than once"), "case-duplicate target keys are reported");
    Check(Reports(caseDuplicates, "justifications name flag 'unit' more than once"), "case-duplicate justification keys are reported");
    var duplicateRows = BuildFileTargetRows(One(caseDuplicates), new());
    Check(duplicateRows.Count == 1, "case-duplicate keys give one report row, not two");

    // --- closed-world package targets (#2876) ---
    static List<string> Missing(ManifestFileEntry[] files, Dictionary<string, double>? pkgTargets)
    {
        var expected = new Dictionary<string, Dictionary<string, int>>();
        foreach (var f in files) CountExpectedFlags(f, expected, "Pkg");
        var targetsByPkg = new Dictionary<string, Dictionary<string, double>>();
        if (pkgTargets is not null) targetsByPkg["Pkg"] = pkgTargets;
        return FindMissingPackageTargets(expected, targetsByPkg);
    }
    var missingIntegration = Missing([new() { DefaultTests = ["unit", "integration"] }, new() { Override = ["Integration"], DefaultTests = ["guard"] }], new() { ["unit"] = 80 });
    Check(missingIntegration.Count == 1 && missingIntegration[0] == "Pkg: flag 'integration' is expected by 2 file(s) and the package has no target for it",
        "a flag expected by files with no package target is reported with its file count (override wins over defaultTests)");
    Check(Missing([new() { DefaultTests = ["unit", "integration"] }], new() { ["UNIT"] = 80, ["Integration"] = 70 }).Count == 0, "package keys match flags case-insensitively");
    Check(Missing([new() { DefaultTests = ["unit"], Targets = new() { ["Property"] = 50 } }], new() { ["unit"] = 80 }).SingleOrDefault()?.Contains("flag 'property' is expected by 1 file(s)") == true, "a per-file target key alone makes the flag expected");
    Check(Missing([new() { DefaultTests = ["unit"] }], new() { ["unit"] = 80, ["contract"] = 60 }).Count == 0, "a package key for a flag no file expects is not an error");
    Check(Missing([new() { DefaultTests = ["unit"] }], null).Count == 1, "a package with no targets at all is reported");

    // --- measured against target ---
    var lines = new Dictionary<int, int>();
    for (var n = 1; n <= 10_000; n++) lines[n] = n <= 8_996 ? 1 : 0; // 89.96% covered
    var data = new Dictionary<TestType, Dictionary<string, Dictionary<int, int>>>
    {
        [TestType.Unit] = new(StringComparer.OrdinalIgnoreCase) { ["src/Pkg/A.cs"] = lines }
    };
    var measured = MeasureFileFlag(data, TestType.Unit, "src/Pkg/A.cs");
    Check(measured is { } m89 && Math.Abs(m89 - 89.96) < 1e-9, "the measured value is not rounded");
    Check(IsBelowTarget(new FileTargetRow("Pkg", "A.cs", "unit", 90, measured)), "89.96% does not meet a target of 90");
    Check(!IsBelowTarget(new FileTargetRow("Pkg", "A.cs", "unit", 89, measured)), "89.96% meets a target of 89");
    Check(FormatMeasured(measured) == "89.96%" || FormatMeasured(measured) == "89,96%", "the display keeps two decimals");
    Check(IsBelowTarget(new FileTargetRow("Pkg", "A.cs", "guard", 1, null)), "no data is below a target above 0");
    Check(!IsBelowTarget(new FileTargetRow("Pkg", "A.cs", "guard", 0, null)), "a target of 0 is always met");
    Check(MeasureFileFlag(data, TestType.Guard, "src/Pkg/A.cs") is null, "a flag with no data measures null");
    Check(MeasureFileFlag(data, TestType.Unit, "src/Pkg/Other.cs") is null, "a file absent from the flag measures null");

    // --- the three Cobertura filename shapes land on the same key ---
    Check(ToSrcRelativePath("src\\Pkg\\A.cs", "C:\\repo\\") == "src/Pkg/A.cs", "shape 1: src-relative filename");
    Check(ToSrcRelativePath("Pkg\\A.cs", "C:\\repo\\src\\") == "src/Pkg/A.cs", "shape 2: filename relative to a <source> ending in src");
    Check(ToSrcRelativePath("C:\\repo\\src\\Pkg\\A.cs", "C:\\repo\\") == "src/Pkg/A.cs", "shape 3: absolute filename");
    Check(ToSrcRelativePath("/home/r/src/Pkg/A.cs", "/home/r") == "src/Pkg/A.cs", "shape 3: absolute POSIX filename");
    Check(ToSrcRelativePath("tests\\X\\T.cs", "C:\\repo\\") is null, "a file outside src/ is skipped");
    var rowFromShape = BuildFileTargetRows(One(good), new()
    {
        [TestType.Unit] = new(StringComparer.OrdinalIgnoreCase) { [ToSrcRelativePath("Pkg\\A.cs", "C:\\repo\\src\\")!] = lines }
    });
    Check(rowFromShape.Any(r => r.Flag == "unit" && r.Measured is not null), "a per-file target finds data reported in the shape 2 filename");

    // --- target attainment (#2542) ---
    static bool Near(double? actual, double expected) => actual is { } a && Math.Abs(a - expected) < 1e-9;
    static FileTargetRow Pair(string file, string flag, double target, int total, int covered)
        => new("Pkg", file, flag, target, total == 0 ? null : covered * 100.0 / total, total, covered);

    Check(Near(GapPoints(new FileTargetRow("Pkg", "A.cs", "unit", 90, measured)), 90 - 89.96), "gap of 89.96% against 90 is 0.04 points");
    Check(GapPoints(new FileTargetRow("Pkg", "A.cs", "unit", 89, measured)) == 0, "89.96% against 89 has no gap");
    var justBelow = new FileTargetRow("Pkg", "A.cs", "unit", 90, 89.96);
    Check(IsBelowTarget(justBelow), "89.96% against 90 is below");
    Check(FormatAttainment(Attainment(justBelow)) != "100.0%" && FormatAttainment(Attainment(justBelow)) == "99.9%", "a pair just below target never displays 100.0% attainment");
    Check(FormatGap(GapPoints(justBelow)) != "0.00", "a pair just below target never displays a gap of 0.00");
    Check(ToFileTargetEntry(justBelow).Attainment < 1, "the JSON attainment of a below pair is not rounded up to 1");
    Check(GapPoints(new FileTargetRow("Pkg", "A.cs", "guard", 0, null)) == 0, "a target of 0 has no gap");
    Check(GapPoints(new FileTargetRow("Pkg", "A.cs", "guard", 40, null)) == 40, "no data is a gap of the whole target");
    Check(Near(Attainment(new FileTargetRow("Pkg", "A.cs", "unit", 90, measured)), 89.96 / 90), "attainment of 89.96% against 90 is measured / target");
    Check(Attainment(new FileTargetRow("Pkg", "A.cs", "unit", 89, measured)) == 1, "attainment is capped at 1");
    Check(Attainment(new FileTargetRow("Pkg", "A.cs", "guard", 0, null)) == 1, "a target of 0 is fully attained");
    Check(Attainment(new FileTargetRow("Pkg", "A.cs", "guard", 40, null)) == 0, "no data has attainment 0");
    Check(LinesToCover(new FileTargetRow("Pkg", "A.cs", "unit", 7, 0, 100, 0)) == 7, "target 7 of 100 lines needs 7 lines (integer arithmetic)");
    Check(LinesToCover(new FileTargetRow("Pkg", "A.cs", "unit", 90, 50, 3, 1)) == 2, "lines to cover rounds the target up");
    Check(LinesToCover(new FileTargetRow("Pkg", "A.cs", "unit", 90, null)) == 0, "an unknown line count needs no lines");
    Check(LinesToCover(new FileTargetRow("Pkg", "A.cs", "unit", 10, 100, 10, 10)) == 0, "a met pair needs no lines");

    // Two-pair known answer, worked by hand (plan Phase 6): A target 50, 30/100 covered: gap 20, weight 50, credit 30;
    // B target 80, 190/200 covered: gap 0, weight 160, credit min(190 covered, 160) = 160.
    var two = SummariseAttainment([Pair("A.cs", "unit", 50, 100, 30), Pair("B.cs", "unit", 80, 200, 190)]);
    Check(two.Pairs == 2 && two.Met == 1, "two pairs, one met");
    Check(Near(two.Ratio, 0.5), "two-pair ratio is 0.5");
    Check(Near(two.LineWeighted, 190.0 / 210.0), "two-pair line-weighted attainment is 190/210");
    Check(Near(two.MeanGap, 10), "two-pair mean gap is 10 points");
    Check(two.LinesToCover == 20, "two-pair lines to cover is 20");
    Check(two.NoData == 0 && two.Unweighted == 0 && two.ZeroTarget == 0, "two-pair counts of excluded pairs are 0");

    // A zero-target pair (D4) is excluded from every aggregate and counted.
    var withZero = SummariseAttainment([Pair("A.cs", "unit", 50, 100, 30), Pair("B.cs", "unit", 80, 200, 190), Pair("C.cs", "guard", 0, 50, 0)]);
    Check(withZero.Pairs == 2 && withZero.ZeroTarget == 1 && Near(withZero.Ratio, 0.5) && Near(withZero.MeanGap, 10) && Near(withZero.LineWeighted, 190.0 / 210.0),
        "a zero-target pair leaves the aggregates unchanged and is counted");
    Check(TargetStatus(Pair("C.cs", "guard", 0, 50, 0)) == "zero-target" && TargetStatus(new FileTargetRow("Pkg", "C.cs", "guard", 0, null)) == "zero-target",
        "a pair with a target of 0 has status zero-target, with or without data");
    Check(TargetStatus(Pair("A.cs", "unit", 50, 100, 30)) == "below" && TargetStatus(Pair("B.cs", "unit", 80, 200, 190)) == "met",
        "a pair with a target above 0 is below or met");
    Check(ToFileTargetEntry(Pair("C.cs", "guard", 0, 50, 0)).Status == "zero-target", "a zero-target pair is written to latest.json as zero-target");

    // A pair with no data (D3) is 0% measured: counted, with the borrowed line count and no borrowed coverage.
    var other = new Dictionary<int, int>();
    for (var n = 1; n <= 20; n++) other[n] = 1; // every line covered under the unit flag
    var noDataRows = BuildFileTargetRows(One(new ManifestFileEntry { Targets = new() { ["unit"] = 50, ["guard"] = 50 } }), new()
    {
        [TestType.Unit] = new(StringComparer.OrdinalIgnoreCase) { ["src/Pkg/A.cs"] = other }
    });
    var guardRow = noDataRows.Single(r => r.Flag == "guard");
    Check(guardRow.Measured is null && guardRow.TotalLines == 20 && guardRow.CoveredLines == 0, "no data borrows the line count only: covered stays 0");
    var noData = SummariseAttainment(noDataRows);
    Check(noData.Pairs == 2 && noData.Met == 1 && noData.NoData == 1 && noData.Unweighted == 0, "a no-data pair counts as unmet and in NoData");
    Check(Near(noData.MeanGap, 25) && noData.LinesToCover == 10, "a no-data pair has a gap of its whole target");
    Check(Near(noData.LineWeighted, 10.0 / 20.0), "no-data pair weighs its target lines and earns no credit");

    // A file with no data under any flag: counted in the pair aggregates, unweighted.
    var noLines = SummariseAttainment([new FileTargetRow("Pkg", "Z.cs", "unit", 60, null)]);
    Check(noLines.Pairs == 1 && noLines.Met == 0 && noLines.NoData == 1 && noLines.Unweighted == 1, "a pair with no line count is counted and unweighted");
    Check(noLines.LineWeighted is null && Near(noLines.Ratio, 0) && Near(noLines.MeanGap, 60), "an unweighted-only set has no line-weighted value");

    // No pairs: Ratio and MeanGap are null, not NaN, and the summary still serialises.
    var empty = SummariseAttainment([]);
    Check(empty.Pairs == 0 && empty.Ratio is null && empty.MeanGap is null && empty.LineWeighted is null, "no pairs gives null ratio and mean gap");
    var zeroOnly = SummariseAttainment([Pair("A.cs", "guard", 0, 10, 0)]);
    Check(zeroOnly.Pairs == 0 && zeroOnly.ZeroTarget == 1 && zeroOnly.Ratio is null, "only zero-target pairs give null ratio");
    Check(JsonSerializer.Serialize(empty, SummaryJsonOptions()).Contains("\"pairs\": 0", StringComparison.Ordinal), "the empty summary serialises");

    // Case-duplicate flag keys give one pair in the aggregates.
    Check(SummariseAttainment(duplicateRows).Pairs == 1, "case-duplicate keys give one attainment pair");

    // The JSON written to latest.json has exactly the documented fields.
    static string Fields(object value)
        => string.Join(",", JsonSerializer.SerializeToNode(value, SummaryJsonOptions())!.AsObject().Select(kv => kv.Key).Order(StringComparer.Ordinal));
    Check(Fields(two) == "lineWeighted,linesToCover,meanGap,met,noData,pairs,ratio,unweighted,zeroTarget", "the attainment object has exactly the documented fields");
    var entry = ToFileTargetEntry(Pair("A.cs", "unit", 50, 100, 30));
    Check(Fields(entry) == "attainment,covered,file,flag,gap,linesToCover,measured,package,status,target,total", "a fileTargets row has exactly the documented fields");
    Check(entry.Status == "below" && entry.Gap == 20 && entry.LinesToCover == 20 && entry.Covered == 30 && entry.Total == 100, "a fileTargets row carries the pair's values");
    Check(!JsonSerializer.Serialize(ToFileTargetEntry(new FileTargetRow("Pkg", "Z.cs", "unit", 60, null)), SummaryJsonOptions()).Contains("measured", StringComparison.Ordinal),
        "a pair without data omits measured instead of writing 0");
    Check(OrderByGap([Pair("B.cs", "unit", 80, 200, 190), Pair("A.cs", "unit", 50, 100, 30)])[0].File == "A.cs", "rows are ordered by gap, largest first");

    // --- pull request comment (#3289) ---
    var touchedList = ParseTouchedFiles([
        "src/Pkg/A.cs", " src\\Pkg\\C.cs ", "src/Pkg/B.cs", "docs/x.md", "src/Pkg/../../etc.cs",
        "src/Pkg/`x`.cs", "src/Pkg/<img src=x>.cs", "tests/Encina.UnitTests/T.cs", "", "src/Pkg/Readme.md"]);
    Check(touchedList.SetEquals(["src/Pkg/A.cs", "src/Pkg/B.cs", "src/Pkg/C.cs"]),
        "the touched list keeps plain src/ C# paths only (markup, backticks, .. and other folders are dropped)");
    var commentRows = new List<FileTargetRow> { Pair("A.cs", "unit", 80, 100, 90), Pair("A.cs", "guard", 50, 100, 10), Pair("Other.cs", "unit", 80, 100, 10) };
    var comment = RenderPullRequestComment(commentRows, touchedList, ["unit"]);
    Check(comment.Contains("`Pkg/A.cs` | guard", StringComparison.Ordinal) && comment.Contains("`Pkg/A.cs` | unit", StringComparison.Ordinal),
        "the comment lists every target of a touched file");
    Check(RenderPullRequestComment([new FileTargetRow("Pkg", "A.cs", "guard", 50, null)], touchedList, ["unit"]).Contains("| no data |", StringComparison.Ordinal),
        "a pair without data has the status no data, not below");
    Check(!comment.Contains("Other.cs", StringComparison.Ordinal), "the comment leaves out a file the pull request does not touch");
    Check(comment.Contains("Targets met: 1/2", StringComparison.Ordinal), "the comment summarises only the touched rows");
    Check(comment.Contains("`src/Pkg/B.cs`", StringComparison.Ordinal) && comment.Contains("`src/Pkg/C.cs`", StringComparison.Ordinal) && !comment.Contains("`src/Pkg/A.cs`", StringComparison.Ordinal),
        "the comment names the touched files that have no per-file target");
    Check(comment.IndexOf("`Pkg/A.cs` | guard", StringComparison.Ordinal) < comment.IndexOf("`Pkg/A.cs` | unit", StringComparison.Ordinal),
        "the comment orders rows by gap, largest first");
    Check(RenderPullRequestComment(commentRows, touchedList, []).Contains("No flag produced coverage data", StringComparison.Ordinal),
        "the comment says when no flag has data");
    Check(RenderPullRequestComment(commentRows, new HashSet<string> { "src/Pkg/B.cs" }, ["unit"]).Contains("None of the touched files has a per-file target", StringComparison.Ordinal),
        "the comment says when no touched file has a target");
    Check(RenderPullRequestComment(commentRows, touchedList, ["unit"], maxRows: 1).Contains("1 more rows", StringComparison.Ordinal),
        "the comment caps its table and says how many rows are left out");

    // A manifested package with no measured lines is a not-measured row, not an absent one (#2827).
    var seedFiles = new List<FileCoverage>
    {
        new("src/Encina.A/X.cs", "Encina.A", 10, 8, 80, new() { [TestType.Unit] = (10, 8) }, []),
        new("src/Encina.A/Y.cs", "Encina.A", 10, 2, 20, new() { [TestType.Unit] = (10, 2) }, []),
    };
    var seedTargets = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Encina.A"] = new() { ["unit"] = 80 },
        ["Encina.Gone"] = new() { ["unit"] = 70 },
    };
    var seeded = SeedPackages(["Encina.A", "Encina.Gone", "Encina.Silent", "encina.a"], seedFiles, seedTargets);
    var seededA = seeded.Single(p => p.Name == "Encina.A");
    var seededGone = seeded.Single(p => p.Name == "Encina.Gone");
    Check(!seededGone.Measured && seededGone.TotalLines == 0 && seededGone.PerFlag.Count == 0 && seededGone.Files.Count == 0,
        "a manifest-only package is present and flagged not measured, with no lines or flags");
    Check(seeded.Count == 3, "the package count equals the manifest count (case-duplicate keys count once)");
    Check(string.Join(",", seeded.Where(p => !p.Measured).Select(p => p.Name).Order(StringComparer.Ordinal)) == "Encina.Gone,Encina.Silent",
        "the not-measured set is exactly the manifest packages without data");
    Check(seededA.Measured && seededA.TotalLines == 20 && Near(seededA.Percentage, 50) && seededA.PerFlag[TestType.Unit] == (20, 10)
            && seededA.Files.Count == 2 && seededA.PerFlagTarget?["unit"] == 80,
        "a package with data keeps its measured aggregates and targets");
    Check(seededGone.PerFlagTarget?["unit"] == 70 && seeded.Single(p => p.Name == "Encina.Silent").PerFlagTarget is null,
        "a not-measured package keeps its manifest targets, or none");
    Check(seeded.Sum(p => p.PerFlag.Values.Sum(v => v.Total)) == 20, "a not-measured package adds no obligations to the overall ratios");
    Check(SeedPackages([], seedFiles, seedTargets).Count == 1 && SeedPackages(["Encina.A"], [], seedTargets).Single().Measured == false,
        "no manifest keys give only the measured packages; no data gives only not-measured rows");

    // --- target check (#1651) ---
    TestType[] unitRan = [TestType.Unit];
    static string Kinds(List<TargetFinding> found) => string.Join(",", found.Select(f => f.Kind));
    static List<TargetFinding> Find(IReadOnlyList<FileTargetRow> rows, TestType[] ran, bool all = false, string[]? parseFailures = null,
        HashSet<string>? touched = null, Func<string, bool>? exists = null)
        => FindTargetProblems(rows, ran, all, parseFailures ?? [], touched, exists ?? (_ => true));

    Check(Kinds(Find([Pair("A.cs", "unit", 80, 100, 79)], unitRan)) == "below-target", "a pair below its target is a finding");
    Check(Find([Pair("A.cs", "unit", 80, 100, 80)], unitRan).Count == 0, "a pair exactly on its target is not a finding");
    Check(Kinds(Find([new FileTargetRow("Pkg", "A.cs", "unit", 80, null)], unitRan)) == "no-data", "a flag that ran with no data for the file is a finding");
    Check(Find([new FileTargetRow("Pkg", "A.cs", "guard", 50, null)], unitRan).Count == 0, "a flag that did not run is not required on a pull request");
    Check(Kinds(Find([new FileTargetRow("Pkg", "A.cs", "guard", 50, null)], unitRan, all: true)) == "no-data", "--require-all-flags requires a flag that did not run");
    Check(Find([new FileTargetRow("Pkg", "A.cs", "guard", 0, null)], unitRan, all: true).Count == 0, "a target of 0 never needs data");
    Check(Kinds(Find([Pair("A.cs", "guard", 50, 100, 10)], unitRan)) == "below-target", "a pair below its target counts even when its flag is not in the run list");
    Check(Kinds(Find([], unitRan, parseFailures: ["UnitTests/coverage.cobertura.xml"])) == "parse-error", "a Cobertura parse failure is a finding");
    Check(Kinds(Find([new FileTargetRow("Pkg", "A.cs", "unit", 80, null)], [])) == "no-coverage", "a run with no flag at all is a finding, not a pass");
    Check(Find([new FileTargetRow("Pkg", "A.cs", "unit", 0, null)], []).Count == 0, "no flag in the run is fine when no target needs data");

    var touchedSet = new HashSet<string> { "src/Pkg/A.cs", "src/Pkg/New.cs", "src/Pkg/Gone.cs" };
    var targetedRows = new List<FileTargetRow> { Pair("A.cs", "unit", 80, 100, 90) };
    var touchedFindings = Find(targetedRows, unitRan, touched: touchedSet, exists: p => p != "src/Pkg/Gone.cs");
    Check(Kinds(touchedFindings) == "no-targets" && touchedFindings[0].Subject == "src/Pkg/New.cs",
        "a touched file with no per-file target is a finding; a targeted or deleted file is not");
    Check(Find(targetedRows, unitRan, touched: null).Count == 0, "without --files-from no touched file is checked");

    // A src/ C# path the character filter drops is a finding, never a silent escape (#3860).
    string[] oddLines = ["src/Pkg/My File.cs", "src/Pkg/A$.cs", "src/Pkg/../x.cs", "src/Pkg/Ok.cs", "docs/My File.md", "tests/My File.cs", "src/Pkg/Readme.md", ""];
    Check(CountDroppedSrcPaths(oddLines) == 3, "only src/ C# paths the filter refuses are counted as dropped");
    Check(CountDroppedSrcPaths(["src/Pkg/A.cs"]) == 0, "a plain path is not counted as dropped");
    var droppedFindings = FindTargetProblems(targetedRows, unitRan, false, [], touchedSet, _ => true, droppedSrcPaths: 2);
    Check(droppedFindings.Any(f => f.Kind == "dropped-path" && !f.Message.Contains("My File", StringComparison.Ordinal)),
        "dropped src paths are a dropped-path finding that does not echo the path");
    Check(FindTargetProblems(targetedRows, unitRan, false, [], touchedSet, _ => true).All(f => f.Kind != "dropped-path"),
        "no dropped path, no dropped-path finding");

    var reported = new List<TargetFinding> { new("below-target", "Pkg/A.cs unit", "x") };
    var reportDir = Path.Combine(Path.GetTempPath(), "coverage-report-selftest-targets-" + Guid.NewGuid().ToString("N"));
    try
    {
        Check(!RunCheckTargets(reported, targetedRows, unitRan, enforce: false, requireAllFlags: false, reportDir), "report-only mode never fails the run");
        Check(RunCheckTargets(reported, targetedRows, unitRan, enforce: true, requireAllFlags: false, reportDir), "enforce mode fails the run on a finding");
        Check(!RunCheckTargets([], targetedRows, unitRan, enforce: true, requireAllFlags: false, reportDir), "enforce mode passes with no finding");
        var written = JsonNode.Parse(File.ReadAllText(Path.Combine(reportDir, "check-targets.json")))!.AsObject();
        Check(string.Join(",", written.Select(kv => kv.Key).Order(StringComparer.Ordinal)) == "checkedTargets,enforce,findings,flagsInRun,passed,requireAllFlags",
            "check-targets.json has exactly the documented fields");
    }
    finally
    {
        if (Directory.Exists(reportDir)) Directory.Delete(reportDir, true);
    }

    // --- flag assignment (#2932) ---
    static Dictionary<int, int> Executed(int executed, int unexecuted)
    {
        var d = new Dictionary<int, int>();
        for (var n = 1; n <= executed + unexecuted; n++) d[n] = n <= executed ? 1 : 0;
        return d;
    }
    var assignManifest = new Dictionary<string, Dictionary<string, TestType>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Pkg"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Unit.cs"] = TestType.Unit,
            ["Override.cs"] = TestType.Unit | TestType.Contract,
            ["Targeted.cs"] = TestType.Unit,
            ["Excluded.cs"] = TestType.None,
            ["Gen.g.cs"] = TestType.Unit
        }
    };
    var assignObligations = new Dictionary<string, Dictionary<string, ManifestFileEntry>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Pkg"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Targeted.cs"] = new ManifestFileEntry { Targets = new() { ["Contract"] = 0 }, Justifications = new() { ["contract"] = "x" } }
        }
    };
    var contractData = new Dictionary<TestType, Dictionary<string, Dictionary<int, int>>>
    {
        [TestType.Contract] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["src/Pkg/Unit.cs"] = Executed(12, 3),        // unassigned: reported
            ["src/Pkg/Override.cs"] = Executed(12, 0),    // assigned through the override
            ["src/Pkg/Targeted.cs"] = Executed(12, 0),    // has a contract target but is not in the package row: reported with a note
            ["src/Pkg/Excluded.cs"] = Executed(12, 0),    // excluded on purpose
            ["src/Pkg/Gen.g.cs"] = Executed(12, 0),       // generated
            ["src/Pkg/Small.cs"] = Executed(2, 8),        // unlisted (unit + guard fallback), below the threshold
            ["src/Pkg/Idle.cs"] = Executed(0, 9),         // nothing executed
            ["src/Other/X.cs"] = Executed(12, 0)          // package without a manifest
        },
        [TestType.Unit] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["src/Pkg/Unit.cs"] = Executed(5, 0),
            ["src/Pkg/ExclTarget.cs"] = Executed(9, 0)    // excluded by an empty override although it has a target
        }
    };
    assignManifest["Pkg"]["ExclTarget.cs"] = TestType.None;
    assignObligations["Pkg"]["ExclTarget.cs"] = new ManifestFileEntry { Targets = new() { ["contract"] = 10 }, Justifications = new() { ["contract"] = "x" } };
    var unassigned = FindUnassignedFlagFiles(contractData, assignManifest, assignObligations);
    Check(unassigned.Count == 3 && unassigned.Any(r => r is { Package: "Pkg", File: "Unit.cs", Flag: "contract", ExecutedLines: 12, TotalLines: 15, HasFileTarget: false }),
        "a flag that executes a file the manifest does not assign to it is reported with its executed lines");
    Check(unassigned.All(r => r.File != "Override.cs"), "a file assigned through the override is not reported");
    Check(unassigned.Any(r => r is { File: "Targeted.cs", Flag: "contract", HasFileTarget: true }),
        "a file with a per-file target but no override/defaultTests assignment is still reported, flagged as having a target");
    Check(unassigned.All(r => r.File is not ("Excluded.cs" or "Gen.g.cs" or "Idle.cs" or "X.cs" or "ExclTarget.cs")), "excluded (even with a target), generated, idle and unmanifested files are not reported");
    Check(unassigned.All(r => r.Flag == "contract"), "a flag assigned to the file (unit on Unit.cs) is not reported");
    Check(unassigned.Any(r => r.File == "Small.cs" && r.ExecutedLines == 2), "an unlisted file gets the unit + guard fallback, so contract is unassigned");
    var assignOut = Path.Combine(Path.GetTempPath(), "flag-assignment-selftest-" + Guid.NewGuid().ToString("N"));
    try
    {
        Check(RunCheckFlagAssignment(unassigned, 5, assignOut), "a file at or above --min-lines fails the check");
        Check(File.Exists(Path.Combine(assignOut, "flag-assignment.json")), "the check writes flag-assignment.json");
        Check(!RunCheckFlagAssignment(unassigned.Where(r => r.File == "Small.cs").ToList(), 5, assignOut), "files below --min-lines do not fail the check");
        Check(RunCheckFlagAssignment(unassigned.Where(r => r.File == "Small.cs").ToList(), 2, assignOut), "--min-lines is inclusive");
        Check(!RunCheckFlagAssignment(FindUnassignedFlagFiles(new(), assignManifest, assignObligations), 5, assignOut), "empty input finds nothing and passes");
    }
    finally
    {
        if (Directory.Exists(assignOut)) Directory.Delete(assignOut, true);
    }

    // --- zero-target classifier (#2890) ---
    failures.AddRange(ZeroSelfTest.Run(out var zeroChecks));
    passed += zeroChecks;
    failures.AddRange(ZeroSelfTest.CheckFiles(FindRejectedZeros, RunCheckZeros, out var zeroFileChecks));
    passed += zeroFileChecks;

    foreach (var failure in failures) Console.WriteLine($"  FAIL: {failure}");
    if (failures.Count > 0)
    {
        Console.WriteLine($"Self-test FAILED ({failures.Count} of {passed + failures.Count} checks)");
        return 1;
    }
    Console.WriteLine($"Self-test passed ({passed} checks)");
    return 0;
}

// Checks every manifest key against src/<Package>/<relPath>; a key naming a file that no
// longer exists is stale. Returns true (failed) when there is anything to report.
bool RunCheckStaleManifest(
    Dictionary<string, Dictionary<string, TestType>> manifest,
    List<string> manifestParseFailures,
    string repoRoot)
{
    var staleKeys = new List<string>();
    foreach (var (package, files) in manifest.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        foreach (var key in files.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var relPath = key.Replace('\\', '/');
            var sourcePath = Path.Combine(repoRoot, "src", package, relPath);
            if (!File.Exists(sourcePath))
                staleKeys.Add($"{package}: {relPath}");
        }
    }

    var hasParseFailures = manifestParseFailures.Count > 0;
    var hasStaleKeys = staleKeys.Count > 0;

    if (hasParseFailures)
    {
        Console.WriteLine($"\nMANIFEST PARSE FAILURES ({manifestParseFailures.Count}): these files could not be parsed, so their keys were skipped and cannot be vouched for as non-stale");
        foreach (var failedFile in manifestParseFailures)
            Console.WriteLine($"  - {failedFile}");
    }

    if (hasStaleKeys)
    {
        Console.WriteLine($"\nSTALE MANIFEST KEYS ({staleKeys.Count}): the key names a file that does not exist under src/<Package>/");
        foreach (var staleKey in staleKeys)
            Console.WriteLine($"  - {staleKey}");
    }

    if (hasParseFailures || hasStaleKeys)
        return true;

    Console.WriteLine("\nNo stale manifest keys found.");
    return false;
}

// Checks every src/<Package>/**/*.cs file (excluding obj/, bin/, generated *.g.cs) against the
// package's manifest, in both directions: a file with no key in an existing manifest, and (#1536
// review) a src/Encina.* package directory with source files but no <Package>.json manifest at
// all, and a manifest whose "package" field names a src/ directory that does not exist (a rename
// or a case mismatch — case-sensitive on Linux CI). Returns true (failed) when there is anything
// to report.
bool RunCheckMissingManifest(
    Dictionary<string, Dictionary<string, TestType>> manifest,
    List<string> manifestParseFailures,
    string repoRoot,
    string manifestDirFull)
{
    var missingFiles = new List<string>();

    foreach (var package in manifest.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
        var pkgSrcDir = Path.Combine(repoRoot, "src", package);
        if (!Directory.Exists(pkgSrcDir))
        {
            missingFiles.Add($"{package}: manifest has no src/{package}/ directory (check the name and its case)");
            continue;
        }

        var manifestKeys = manifest[package].Keys
            .Select(k => k.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var srcFiles = Directory.GetFiles(pkgSrcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                     && !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase));

        foreach (var srcFile in srcFiles.OrderBy(f => f, StringComparer.Ordinal))
        {
            var relPath = Path.GetRelativePath(pkgSrcDir, srcFile).Replace('\\', '/');
            if (!manifestKeys.Contains(relPath))
                missingFiles.Add($"{package}: {relPath}");
        }
    }

    // A src/Encina.* directory with source files but no <Package>.json next to it is invisible
    // to the loop above, which only iterates packages that already have a loaded manifest.
    var srcRootDir = Path.Combine(repoRoot, "src");
    if (Directory.Exists(srcRootDir))
    {
        var allPackageDirNames = Directory.GetDirectories(srcRootDir)
            .Select(Path.GetFileName)
            .Where(name => name is not null && name.StartsWith("Encina.", StringComparison.Ordinal))
            .Select(name => name!)
            .OrderBy(n => n, StringComparer.Ordinal);

        foreach (var pkgDirName in allPackageDirNames)
        {
            var pkgSrcDir = Path.Combine(srcRootDir, pkgDirName);
            var hasCsFiles = Directory.GetFiles(pkgSrcDir, "*.cs", SearchOption.AllDirectories)
                .Any(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                       && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
            if (!hasCsFiles) continue;

            var manifestFilePath = Path.Combine(manifestDirFull, $"{pkgDirName}.json");
            if (!File.Exists(manifestFilePath))
                missingFiles.Add($"{pkgDirName}: no coverage manifest");
        }
    }

    var hasParseFailures = manifestParseFailures.Count > 0;
    var hasMissingFiles = missingFiles.Count > 0;

    if (hasParseFailures)
    {
        Console.WriteLine($"\nMANIFEST PARSE FAILURES ({manifestParseFailures.Count}): these files could not be parsed, so their package's source files cannot be vouched for as covered by the manifest");
        foreach (var failedFile in manifestParseFailures)
            Console.WriteLine($"  - {failedFile}");
    }

    if (hasMissingFiles)
    {
        Console.WriteLine($"\nMISSING MANIFEST ENTRIES ({missingFiles.Count}): the file exists under src/<Package>/ but has no key in the package manifest, the package has no manifest at all, or the manifest names a src/ directory that does not exist");
        foreach (var missing in missingFiles.OrderBy(m => m, StringComparer.Ordinal))
            Console.WriteLine($"  - {missing}");
        Console.WriteLine("\n  To fix a missing file entry or a missing manifest run: dotnet run --file .github/scripts/generate-coverage-manifest.cs (append-only by default; add --dry-run to preview)");
        Console.WriteLine("  Do not run the generator with --full on an existing manifest until #1597 resolves the hand-edited entries.");
    }

    if (hasParseFailures || hasMissingFiles)
        return true;

    Console.WriteLine("\nNo missing manifest entries found.");
    return false;
}

// ─── Detect packages in src/ without a coverage manifest ────────────────────
var srcDir = Path.Combine(Directory.GetCurrentDirectory(), "src");
var untrackedPackages = new List<string>();
if (Directory.Exists(srcDir))
{
    var srcPackages = Directory.GetDirectories(srcDir)
        .Select(d => Path.GetFileName(d))
        .Where(name => name.StartsWith("Encina.", StringComparison.Ordinal))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var manifestedPackages = manifest.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    untrackedPackages = srcPackages.Except(manifestedPackages, StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x).ToList();

    if (untrackedPackages.Count > 0)
    {
        Console.WriteLine($"\n  ⚠ WARNING: {untrackedPackages.Count} package(s) in src/ have no coverage manifest:");
        foreach (var pkg in untrackedPackages)
            Console.WriteLine($"    - {pkg}");
        Console.WriteLine("  Run: dotnet run --file .github/scripts/generate-coverage-manifest.cs (append-only by default; add --dry-run to preview)\n");
    }
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

    // 2. Fallback: files not explicitly listed in manifest get only unit + guard.
    //    Contract, property, and integration are opt-in per file — they should NOT be
    //    inherited by unlisted files, as that inflates applicable lines and makes targets
    //    unreachable (e.g., 950 contract lines when only 2 files are actually contract-testable).
    return TestType.Unit | TestType.Guard;
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

// GetCategory removed — all configuration from manifests

// ─── Cobertura XML parsing ───────────────────────────────────────────────────

// line coverage: file → lineNumber → hits (max across applicable reports)
var coverageByFlag = new Dictionary<TestType, Dictionary<string, Dictionary<int, int>>>();

// CRAP (Change Risk Anti-Patterns) raw method data: flag → methodKey → MethodRaw.
// methodKey = "relFile|className|name+signature". Populated alongside coverageByFlag
// from the same <method> elements Cobertura already emits (complexity attribute + <lines>).
var methodsByFlag = new Dictionary<TestType, Dictionary<string, MethodRaw>>();

Console.WriteLine($"Scanning {inputDir} for coverage reports...");

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"WARNING: Input directory '{inputDir}' not found. Generating empty report.");
    Directory.CreateDirectory(outputDir);
    File.WriteAllText(Path.Combine(outputDir, "encina-coverage-report.md"),
        "# Encina Coverage Report\n\nNo coverage data available. Run tests with `--collect \"XPlat Code Coverage\"` first.\n");
    // A missing input directory means every manifested package is unmeasured: the check must not pass vacuously.
    if (checkUnmeasured || checkFlagAssignment) Environment.Exit(1);
    if (checkTargets)
    {
        // Always (over)write check-targets.json so a stale file of an earlier run is never read as current (#3860).
        var missingInput = new List<TargetFinding> { new("no-coverage", "input", $"the input directory '{inputDir}' was not found, so no target could be checked") };
        if (RunCheckTargets(missingInput, [], [], enforce, requireAllFlags, outputDir)) Environment.Exit(1);
    }
    return;
}

var coverageParseFailures = 0;
// Relative paths of the Cobertura files that failed to parse (#1651), for --check-targets.
var coverageParseFailureFiles = new List<string>();
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

        // <source> may end with /src/ — filenames may be relative to it
        var sourceEl = doc.Descendants(ns + "source").FirstOrDefault()?.Value ?? "";

        foreach (var cls in doc.Descendants(ns + "class"))
        {
            var filename = cls.Attribute("filename")?.Value;
            if (filename is null) continue;

            // Normalize separators, resolve a filename relative to <source>, keep only src/ files
            var relFile = ToSrcRelativePath(filename, sourceEl); // "src/Encina.Foo/Bar.cs"
            if (relFile is null) continue;

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

            // Per-method data for CRAP: Cobertura nests one <methods><method> per direct
            // method of this class (compiler-generated state machines/lambdas are their
            // own <class>, so this does not double-count nested types). Build-generated
            // sources under obj/ (source generators: regex, JSON context, etc.) are excluded —
            // nobody edits them and their complexity is not a signal about hand-written risk.
            // This only narrows the new CRAP data; the pre-existing line-coverage aggregation
            // above is untouched, so the base coverage percentages stay byte-for-byte the same.
            var className = cls.Attribute("name")?.Value ?? "";
            var methodsEl = cls.Element(ns + "methods");
            if (methodsEl is not null && !relFile.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            {
                if (!methodsByFlag.TryGetValue(flag, out var flagMethods))
                    methodsByFlag[flag] = flagMethods = new Dictionary<string, MethodRaw>(StringComparer.Ordinal);

                foreach (var m in methodsEl.Elements(ns + "method"))
                {
                    var mName = m.Attribute("name")?.Value ?? "";
                    var mSignature = m.Attribute("signature")?.Value ?? "";
                    var complexity = int.Parse(m.Attribute("complexity")?.Value ?? "1", CultureInfo.InvariantCulture);
                    var methodKey = $"{relFile}|{className}|{mName}{mSignature}";

                    if (!flagMethods.TryGetValue(methodKey, out var raw))
                    {
                        raw = new MethodRaw(relFile, className, mName, mSignature, complexity);
                        flagMethods[methodKey] = raw;
                    }

                    foreach (var mLine in m.Descendants(ns + "line"))
                    {
                        var num = int.Parse(mLine.Attribute("number")?.Value ?? "0", CultureInfo.InvariantCulture);
                        var hits = int.Parse(mLine.Attribute("hits")?.Value ?? "0", CultureInfo.InvariantCulture);
                        if (num <= 0) continue;

                        if (raw.Lines.TryGetValue(num, out var existingHits))
                            raw.Lines[num] = Math.Max(existingHits, hits);
                        else
                            raw.Lines[num] = hits;
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR parsing {relPath}: {ex.Message}");
        coverageParseFailures++;
        coverageParseFailureFiles.Add(relPath.Replace('\\', '/'));
    }
}

Console.WriteLine($"\nLoaded data from {coverageByFlag.Count} test type(s): {string.Join(", ", coverageByFlag.Keys)}");

// --check-flag-assignment (#2932): needs only the coverage data and the manifest, so it runs here and stops.
// It cannot pass vacuously: no manifest, an unparsed manifest or no coverage data is an error.
if (checkFlagAssignment)
{
    var noSourceLines = !coverageByFlag.Values.Any(d => d.Values.Any(l => l.Count > 0));
    if (manifest.Count == 0 || manifestParseFailures.Count > 0 || coverageParseFailures > 0 || noSourceLines)
    {
        Console.WriteLine(manifestParseFailures.Count > 0
            ? $"\nERROR: --check-flag-assignment could not parse {manifestParseFailures.Count} coverage manifest(s): {string.Join(", ", manifestParseFailures)}"
            : coverageParseFailures > 0
                ? $"\nERROR: --check-flag-assignment could not parse {coverageParseFailures} Cobertura report(s); the check would be incomplete."
                : "\nERROR: --check-flag-assignment found no coverage manifest or no executable source lines in the coverage data; nothing was checked.");
        Environment.Exit(1);
    }

    var unassignedFiles = FindUnassignedFlagFiles(coverageByFlag, manifest, fileObligations);
    Environment.Exit(RunCheckFlagAssignment(unassignedFiles, minLines, outputDir) ? 1 : 0);
}

// ─── CRAP per method (combined coverage across ALL flags) ────────────────────
//
// CRAP(m) = comp(m)^2 * (1 - cov(m))^3 + comp(m), comp = Cobertura's <method complexity="">,
// cov = the method's line coverage in [0,1].
//
// Coverage here is deliberately the COMBINED coverage of every flag (unit, guard, contract,
// property, integration) — the union of covered lines across all reports — not filtered by
// the per-file manifest applicability used for the obligations model above. CRAP measures the
// risk of shipping untested complex code; a method proven safe by an integration test is not
// risky merely because integration tests aren't its assigned obligation. See
// docs/testing/coverage-measurement-methodology.md (CRAP section) for the rationale.
var combinedMethods = new Dictionary<string, MethodCrapInfo>(StringComparer.Ordinal);
foreach (var flagMethods in methodsByFlag.Values)
{
    foreach (var (methodKey, raw) in flagMethods)
    {
        if (!combinedMethods.TryGetValue(methodKey, out var info))
        {
            info = new MethodCrapInfo(raw.RelFile, raw.ClassName, raw.Name, raw.Signature, raw.Complexity);
            combinedMethods[methodKey] = info;
        }

        foreach (var (num, hits) in raw.Lines)
        {
            if (info.Lines.TryGetValue(num, out var existingHits))
                info.Lines[num] = Math.Max(existingHits, hits);
            else
                info.Lines[num] = hits;
        }
    }
}

foreach (var info in combinedMethods.Values)
{
    var total = info.Lines.Count;
    var covered = info.Lines.Values.Count(h => h > 0);
    info.Coverage = total > 0 ? covered / (double)total : 1.0;
    info.Crap = (info.Complexity * (double)info.Complexity * Math.Pow(1 - info.Coverage, 3)) + info.Complexity;
}

var methodsByRelFile = combinedMethods.Values
    .GroupBy(m => m.RelFile)
    .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.Crap).ToList());

Console.WriteLine($"CRAP: computed for {combinedMethods.Count} method(s) across {methodsByRelFile.Count} file(s)");

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

    // Get per-file applicable tests from manifest
    var fileRelPath = string.Join("/", parts[2..]); // path within package
    var applicableTests = GetFileApplicableTests(packageName, fileRelPath);
    if (applicableTests == TestType.None) continue; // Excluded

    // Obligations model: each flag×line is an obligation. Combined = obligations met / total obligations.
    // A file with 100 lines and 3 applicable flags has 300 obligations.
    var perFlag = new Dictionary<TestType, (int Total, int Covered)>();
    int totalObligations = 0;
    int metObligations = 0;

    foreach (var (flag, flagData) in coverageByFlag)
    {
        if (!applicableTests.HasFlag(flag)) continue; // Skip non-applicable per THIS FILE
        if (!flagData.TryGetValue(file, out var fileLines)) continue;

        int flagTotal = fileLines.Count;
        int flagCovered = fileLines.Values.Count(h => h > 0);
        perFlag[flag] = (flagTotal, flagCovered);
        totalObligations += flagTotal;
        metObligations += flagCovered;
    }

    // Also count obligations for applicable flags that have NO coverage data (0 covered of N lines)
    // These flags are required by manifest but have no XML data at all — they contribute 0 met.
    // We don't add them here because we don't know how many lines they'd have.
    // The per-flag columns show "?" for these; --check-targets reports a missing flag against a per-file target.

    if (totalObligations == 0) continue;

    // TotalLines = unique source lines (for display), CoveredEquivalent = met obligations
    var allLineNumbers = new HashSet<int>();
    foreach (var (flag, flagData) in coverageByFlag)
    {
        if (!applicableTests.HasFlag(flag)) continue;
        if (flagData.TryGetValue(file, out var fileLines))
            foreach (var lineNum in fileLines.Keys) allLineNumbers.Add(lineNum);
    }

    int totalLines = allLineNumbers.Count;
    double coveredEquivalent = metObligations;
    double pct = totalObligations > 0 ? Math.Round(metObligations * 100.0 / totalObligations, 2) : 0;

    var fileMethods = methodsByRelFile.TryGetValue(file, out var mList) ? mList : [];
    fileResults.Add(new FileCoverage(file, packageName, totalLines, coveredEquivalent, pct, perFlag, fileMethods));
}

// ─── Aggregate by package ────────────────────────────────────────────────────

// Seeded from the manifest keys (#2827): a manifested package with no measured lines is still a
// row, flagged Measured = false, so it is never silently absent and never reads as 0%.
var packageResults = SeedPackages(manifest.Keys, fileResults, manifestTargets);
var unmeasuredPackages = packageResults.Where(p => !p.Measured).Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
if (unmeasuredPackages.Count > 0)
{
    Console.WriteLine($"\n  ⚠ WARNING: {unmeasuredPackages.Count} package(s) have a coverage manifest but no measured lines (not measured, not counted as met):");
    foreach (var pkg in unmeasuredPackages)
        Console.WriteLine($"    - {pkg}");
}

// ─── Overall (obligations model) ────────────────────────────────────────────

// Overall = sum of all obligations met / sum of all obligations across all packages
int overallObligations = packageResults.Sum(p => p.PerFlag.Values.Sum(v => v.Total));
int overallMet = packageResults.Sum(p => p.PerFlag.Values.Sum(v => v.Covered));
double overallPct = overallObligations > 0 ? Math.Round(overallMet * 100.0 / overallObligations, 2) : 0;
int overallTotal = packageResults.Sum(p => p.TotalLines);
double overallCovered = overallMet;

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
Console.WriteLine($"  ENCINA COVERAGE (obligations model): {overallPct}% ({overallMet:N0} / {overallObligations:N0} obligations)");
Console.WriteLine($"{'═',0}══════════════════════════════════════════════════════════════\n");

Console.WriteLine($"  Packages: {packageResults.Count} | Files: {fileResults.Count}");

// ─── Per-file targets (#1762): measured vs target, report only ───────────────

var fileTargetRows = BuildFileTargetRows(fileObligations, coverageByFlag);
var overallAttainment = fileTargetRows.Count > 0 ? SummariseAttainment(fileTargetRows) : null;

if (fileTargetRows.Count > 0)
{
    var below = fileTargetRows.Where(IsBelowTarget).ToList();
    Console.WriteLine($"\n  Per-file targets: {fileTargetRows.Count} | below target: {below.Count}");
    Console.WriteLine($"  Target attainment: {FormatAttainmentSummary(overallAttainment!)}");
    foreach (var row in below)
        Console.WriteLine($"    BELOW: {row.Package}/{row.File} {row.Flag}: {FormatMeasured(row.Measured)} < {row.Target:0}%");
}

// ─── Generate outputs ────────────────────────────────────────────────────────

Directory.CreateDirectory(outputDir);

// 1. Markdown report
var md = new StringBuilder();
md.AppendLine("# Encina Weighted Coverage Report");
md.AppendLine();
md.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
md.AppendLine();
md.AppendLine($"## Overall: {overallPct}% ({overallMet:N0} / {overallObligations:N0} obligations met)");
md.AppendLine();
md.AppendLine("## By Package");
md.AppendLine();
md.AppendLine("| Package | Unit | Guard | Contract | Property | Integ | Combined | Gap |");
md.AppendLine("|---------|:----:|:-----:|:--------:|:--------:|:-----:|:--------:|:---:|");

foreach (var pkg in packageResults)
{
    if (!pkg.Measured)
    {
        md.AppendLine($"| {pkg.Name} | - | - | - | - | - | not measured | |");
        continue;
    }

    md.AppendLine($"| {pkg.Name} | {FlagPct(pkg, TestType.Unit)} | {FlagPct(pkg, TestType.Guard)} | {FlagPct(pkg, TestType.Contract)} | {FlagPct(pkg, TestType.Property)} | {FlagPct(pkg, TestType.Integration)} | {pkg.Percentage:F1}% | |");
}

if (fileTargetRows.Count > 0)
{
    md.AppendLine();
    md.AppendLine("## Per-file targets");
    md.AppendLine();
    md.AppendLine("Target attainment (pairs with a target of 0 are left out of the aggregates):");
    md.AppendLine();
    md.AppendLine("| Targets met | Line-weighted | Mean gap | Lines to cover | No data | Unweighted | Of which zero-target |");
    md.AppendLine("|:-----------:|:-------------:|:--------:|:--------------:|:-------:|:----------:|:--------------------:|");
    var att = overallAttainment!;
    md.AppendLine($"| {att.Met}/{att.Pairs} ({FormatRatio(att.Ratio)}) | {FormatRatio(att.LineWeighted)} | {(att.MeanGap is { } mg ? mg.ToString("F1", CultureInfo.InvariantCulture) + " points" : "n/a")} | {att.LinesToCover} | {att.NoData} | {att.Unweighted} | {att.ZeroTarget} |");
    md.AppendLine();
    md.AppendLine("| File | Flag | Measured | Target | Gap | Attainment | Lines to cover | Status |");
    md.AppendLine("|------|:----:|:--------:|:------:|:---:|:----------:|:--------------:|:------:|");
    foreach (var row in OrderByGap(fileTargetRows))
    {
        md.AppendLine($"| {row.Package}/{row.File} | {row.Flag} | {FormatMeasured(row.Measured)} | {row.Target:0}% | {FormatGap(GapPoints(row))} | {FormatAttainment(Attainment(row))} | {LinesToCover(row)} | {TargetStatus(row)} |");
    }
}

md.AppendLine();
md.AppendLine("<details><summary>File-level detail</summary>");
md.AppendLine();

foreach (var pkg in packageResults.OrderBy(p => p.Name))
{
    md.AppendLine($"### {pkg.Name} ({(pkg.Measured ? pkg.Percentage.ToString("F1", CultureInfo.InvariantCulture) + "%" : "not measured")})");
    md.AppendLine();
    md.AppendLine("| File | Lines | Covered | Coverage | Max CRAP |");
    md.AppendLine("|------|:-----:|:-------:|:--------:|:--------:|");
    foreach (var file in pkg.Files)
    {
        var shortName = file.RelativePath.Replace($"src/{pkg.Name}/", "");
        var maxCrap = file.Methods.Count > 0 ? file.Methods.Max(m => m.Crap) : 0;
        md.AppendLine($"| {shortName} | {file.TotalLines} | {file.CoveredEquivalent:F1} | {file.Percentage:F1}% | {maxCrap:F1} |");
    }
    md.AppendLine();
}

md.AppendLine("</details>");

File.WriteAllText(Path.Combine(outputDir, "encina-coverage-report.md"), md.ToString());
Console.WriteLine($"\n  Markdown: {Path.Combine(outputDir, "encina-coverage-report.md")}");

// 1b. Pull request comment (#3289): only the touched files' per-file targets.
if (filesFrom is not null)
{
    if (!File.Exists(filesFrom))
    {
        Console.WriteLine($"\nERROR: --files-from file '{filesFrom}' does not exist.");
        Environment.Exit(1);
    }
    var touchedFiles = ParseTouchedFiles(File.ReadAllLines(filesFrom));
    if (touchedFiles.Count == 0)
        Console.WriteLine("  No src/ C# file in the touched-file list: no pull request comment written.");
    else
    {
        var flagsWithData = coverageByFlag.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key.ToString().ToLowerInvariant()).ToList();
        File.WriteAllText(Path.Combine(outputDir, "pr-coverage-comment.md"), RenderPullRequestComment(fileTargetRows, touchedFiles, flagsWithData));
        Console.WriteLine($"  Pull request comment: {Path.Combine(outputDir, "pr-coverage-comment.md")}");
    }
}

// 2. JSON summary
var jsonData = new
{
    timestamp = DateTime.UtcNow.ToString("o"),
    overall = new {
        coverage = overallPct, obligations = overallObligations, met = overallMet,
        lines = overallTotal,
        perFlag = overallPerFlag.ToDictionary(
            kv => kv.Key.ToString().ToLowerInvariant(),
            kv => new { total = kv.Value.Total, covered = kv.Value.Covered,
                        coverage = kv.Value.Total > 0 ? Math.Round(kv.Value.Covered * 100.0 / kv.Value.Total, 2) : 0 }),
        attainment = overallAttainment
    },
    fileTargets = fileTargetRows.Count > 0 ? OrderByGap(fileTargetRows).Select(ToFileTargetEntry).ToList() : null,
    packages = packageResults.Select(p => new
    {
        name = p.Name,
        measured = p.Measured,
        attainment = PackageAttainment(fileTargetRows, p.Name),
        coverage = p.Percentage,
        lines = p.TotalLines,
        covered = Math.Round(p.CoveredEquivalent, 2),
        perFlagTarget = p.PerFlagTarget,
        perFlag = p.PerFlag
            .Where(kv => p.PerFlagTarget is null || p.PerFlagTarget.ContainsKey(kv.Key.ToString().ToLowerInvariant()))
            .ToDictionary(
            kv => kv.Key.ToString().ToLowerInvariant(),
            kv => new { total = kv.Value.Total, covered = kv.Value.Covered,
                        coverage = kv.Value.Total > 0 ? Math.Round(kv.Value.Covered * 100.0 / kv.Value.Total, 2) : 0 }),
        // Top methods by CRAP (Change Risk Anti-Patterns), combined coverage across all flags.
        // Capped at 25/package: the full per-method detail for every method in the solution
        // would make this file unwieldy; the crap-gate.cs script computes CRAP directly from
        // the raw Cobertura files for the methods a PR diff actually touches.
        methods = p.Files.SelectMany(f => f.Methods)
            .OrderByDescending(m => m.Crap)
            .Take(25)
            .Select(m => new
            {
                file = m.RelFile.Replace($"src/{p.Name}/", ""),
                className = m.ClassName,
                method = m.Name,
                signature = m.Signature,
                complexity = m.Complexity,
                coverage = Math.Round(m.Coverage * 100.0, 2),
                crap = Math.Round(m.Crap, 2)
            })
    }),
    untrackedPackages = untrackedPackages.Count > 0 ? untrackedPackages : null,
    unmeasuredPackages = unmeasuredPackages.Count > 0 ? unmeasuredPackages : null
};

var jsonOptions = SummaryJsonOptions();
File.WriteAllText(Path.Combine(outputDir, "encina-coverage-summary.json"),
    JsonSerializer.Serialize(jsonData, jsonOptions));
Console.WriteLine($"  JSON:     {Path.Combine(outputDir, "encina-coverage-summary.json")}");

// 2b. DocRef index for coverage citations (SPEC-001, REQ-002/REQ-003).
//     IDs come from the manifests, not from the Cobertura output, so a file keeps its ID
//     when a partial run produced no data for it; missing data is flagged with noData.
var docRefIndex = BuildDocRefIndex(manifest, manifestTargets, coverageByFlag, DateTime.UtcNow.ToString("o"));
File.WriteAllText(Path.Combine(outputDir, "docref-index.json"),
    docRefIndex.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"  DocRef:   {Path.Combine(outputDir, "docref-index.json")} ({docRefIndex.Count} entries)");

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
// The standalone page has no not-measured state, so it lists measured packages only rather than show 0% (#2827).
var htmlDashboard = GenerateHtmlDashboard(overallPct, overallTotal, overallCovered,
    packageResults.Where(p => p.Measured).ToList(), fileResults);
File.WriteAllText(Path.Combine(outputDir, "index.html"), htmlDashboard);
Console.WriteLine($"  HTML:     {Path.Combine(outputDir, "index.html")}");

Console.WriteLine("\nDone.");

// --check-targets (#1651): the outputs above are written first; the findings go to check-targets.json and the
// console, and only --enforce turns them into an exit code.
if (checkTargets)
{
    // <repo>/.github/coverage-manifest -> <repo>; Combine + GetFullPath is safe with a trailing separator.
    var repoRootForTargets = Path.GetFullPath(Path.Combine(manifestDir, "..", ".."));
    var touchedLines = filesFrom is not null ? File.ReadAllLines(filesFrom) : null;
    var touchedForTargets = touchedLines is not null ? ParseTouchedFiles(touchedLines) : null;
    var droppedForTargets = touchedLines is not null ? CountDroppedSrcPaths(touchedLines) : 0;
    var flagsInRunForTargets = coverageByFlag.Keys.Where(f => f != TestType.None).ToList();
    var targetFindings = FindTargetProblems(fileTargetRows, flagsInRunForTargets, requireAllFlags, coverageParseFailureFiles,
        touchedForTargets, path => File.Exists(Path.Combine(repoRootForTargets, path)), droppedForTargets);
    // A manifest that is missing or did not parse cannot vouch for anything: never a vacuous pass.
    if (manifest.Count == 0 || manifestParseFailures.Count > 0)
        targetFindings.Add(new TargetFinding("no-manifest", manifestDir,
            manifestParseFailures.Count > 0
                ? $"{manifestParseFailures.Count} coverage manifest(s) could not be parsed: {string.Join(", ", manifestParseFailures)}"
                : "no coverage manifest was loaded; nothing was checked"));
    if (RunCheckTargets(targetFindings, fileTargetRows, flagsInRunForTargets, enforce, requireAllFlags, outputDir))
        Environment.Exit(1);
}

// --check-unmeasured (#2827): the outputs above are written first, then the run fails when a
// manifested package has no measured lines. Not wired into CI yet (#2896).
if (checkUnmeasured)
{
    if (manifest.Count == 0 || manifestParseFailures.Count > 0)
    {
        // An unparsed manifest never enters `manifest`, so its package would be invisible to this check.
        Console.WriteLine(manifestParseFailures.Count > 0
            ? $"\nERROR: --check-unmeasured could not parse {manifestParseFailures.Count} coverage manifest(s): {string.Join(", ", manifestParseFailures)}"
            : "\nERROR: --check-unmeasured loaded no coverage manifest; nothing was checked.");
        Environment.Exit(1);
    }

    if (unmeasuredPackages.Count > 0)
    {
        Console.WriteLine($"\nFAIL: {unmeasuredPackages.Count} package(s) have a coverage manifest but no measured lines: {string.Join(", ", unmeasuredPackages)}");
        Environment.Exit(1);
    }

    Console.WriteLine($"\nAll {manifest.Count} manifested package(s) have measured lines.");
}

// ─── Helper functions ────────────────────────────────────────────────────────

// Builds docref-index.json: one entry per manifest file with at least one applicable flag,
// keyed "cov:<Package>/<path-in-package>". Per flag, noData is true when the run produced no
// Cobertura data for that file under that flag (no report for the flag, or the file absent
// from it); such flags carry null totals and are excluded from the file-level obligations,
// exactly as the package aggregation above excludes them. File-level noData is true only
// when every applicable flag has noData.
static JsonObject BuildDocRefIndex(
    Dictionary<string, Dictionary<string, TestType>> manifest,
    Dictionary<string, Dictionary<string, double>> manifestTargets,
    Dictionary<TestType, Dictionary<string, Dictionary<int, int>>> coverageByFlag,
    string lastRun)
{
    TestType[] flagOrder = [TestType.Unit, TestType.Guard, TestType.Contract, TestType.Property, TestType.Integration];
    var index = new JsonObject();

    foreach (var (package, files) in manifest.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
        manifestTargets.TryGetValue(package, out var targets);

        foreach (var (key, applicable) in files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (applicable == TestType.None) continue;

            var relPath = key.Replace('\\', '/');
            var sourcePath = $"src/{package}/{relPath}";

            // A manifest entry whose file no longer exists is stale, not citable (REQ-003).
            if (!File.Exists(sourcePath)) continue;
            var flags = new JsonArray();
            var perFlag = new JsonObject();
            int obligations = 0, met = 0;
            var anyData = false;

            foreach (var flag in flagOrder)
            {
                if (!applicable.HasFlag(flag)) continue;

                var flagName = flag.ToString().ToLowerInvariant();
                flags.Add(JsonValue.Create(flagName));
                double? target = targets is not null && targets.TryGetValue(flagName, out var t) ? t : null;

                if (coverageByFlag.TryGetValue(flag, out var flagData) &&
                    flagData.TryGetValue(sourcePath, out var lines))
                {
                    var total = lines.Count;
                    var covered = lines.Values.Count(h => h > 0);
                    obligations += total;
                    met += covered;
                    anyData = true;
                    perFlag[flagName] = new JsonObject
                    {
                        ["total"] = total,
                        ["covered"] = covered,
                        ["coverage"] = total > 0 ? Math.Round(covered * 100.0 / total, 2) : 0,
                        ["target"] = target,
                        ["noData"] = false
                    };
                }
                else
                {
                    perFlag[flagName] = new JsonObject
                    {
                        ["total"] = null,
                        ["covered"] = null,
                        ["coverage"] = null,
                        ["target"] = target,
                        ["noData"] = true
                    };
                }
            }

            index[$"cov:{package}/{relPath}"] = new JsonObject
            {
                ["package"] = package,
                ["path"] = sourcePath,
                // null (not 0) when no flag produced data: 0% would read as "tested and uncovered".
                ["coverage"] = obligations > 0 ? Math.Round(met * 100.0 / obligations, 2) : null,
                ["obligations"] = obligations,
                ["metObligations"] = met,
                ["flags"] = flags,
                ["noData"] = !anyData,
                ["perFlag"] = perFlag,
                ["lastRun"] = lastRun,
                ["dashboardUrl"] = $"https://dlrivada.github.io/Encina/coverage/#pkg-{package}"
            };
        }
    }

    return index;
}

// Package rows: the measured packages grouped from the file results, plus one not-measured row
// for every manifest key with no measured lines (#2827). Package names match case-insensitively
// (the manifest dictionaries are); the name of a measured package is the one in its file results.
static List<PackageCoverage> SeedPackages(
    IEnumerable<string> manifestKeys,
    IReadOnlyList<FileCoverage> fileResults,
    IReadOnlyDictionary<string, Dictionary<string, double>> manifestTargets)
{
    Dictionary<string, double>? TargetsOf(string package)
        => manifestTargets.TryGetValue(package, out var targets) ? targets : null;

    var measured = fileResults
        .GroupBy(f => f.Package)
        .Select(g =>
        {
            var first = g.First();
            int totalLines = g.Sum(f => f.TotalLines);
            double coveredEquivalent = g.Sum(f => f.CoveredEquivalent);

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

            // Combined = total obligations met / total obligations (sum of flag totals)
            int totalObligations = perFlag.Values.Sum(v => v.Total);
            int metObligations = perFlag.Values.Sum(v => v.Covered);
            double pct = totalObligations > 0 ? Math.Round(metObligations * 100.0 / totalObligations, 2) : 0;

            return new PackageCoverage(first.Package, totalLines, coveredEquivalent, pct,
                perFlag, TargetsOf(first.Package), g.OrderBy(f => f.Percentage).ToList());
        })
        .ToList();

    var seen = measured.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var unmeasured = manifestKeys
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(key => !seen.Contains(key))
        .Select(key => new PackageCoverage(key, 0, 0, 0, new(), TargetsOf(key), [], Measured: false));

    return measured.Concat(unmeasured).OrderBy(p => p.Percentage).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
}

string FlagPct(PackageCoverage pkg, TestType flag)
{
    var flagName = flag.ToString().ToLowerInvariant();
    if (pkg.PerFlagTarget is not null && !pkg.PerFlagTarget.ContainsKey(flagName)) return "-";
    if (pkg.PerFlag.TryGetValue(flag, out var data) && data.Total > 0)
        return $"{Math.Round(data.Covered * 100.0 / data.Total, 0)}%";
    return "-";
}

// FormatTestTypes and CountFlags removed — obligations model, no categories

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
    List<PackageCoverage> packages, List<FileCoverage> files)
{
    var jOpts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };
    var packagesJson = JsonSerializer.Serialize(packages.Select(p => new
    {
        name = p.Name, coverage = p.Percentage,
        lines = p.TotalLines, covered = Math.Round(p.CoveredEquivalent, 2), perFlagTarget = p.PerFlagTarget,
        files = p.Files.Select(f => new
        {
            path = f.RelativePath.Replace($"src/{p.Name}/", ""),
            lines = f.TotalLines, covered = Math.Round(f.CoveredEquivalent, 2), coverage = f.Percentage
        })
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

record FileCoverage(string RelativePath, string Package, int TotalLines,
    double CoveredEquivalent, double Percentage, Dictionary<TestType, (int Total, int Covered)> PerFlag,
    List<MethodCrapInfo> Methods);

// Raw per-method data collected from a single Cobertura report (one flag).
sealed class MethodRaw(string relFile, string className, string name, string signature, int complexity)
{
    public string RelFile { get; } = relFile;
    public string ClassName { get; } = className;
    public string Name { get; } = name;
    public string Signature { get; } = signature;
    public int Complexity { get; } = complexity;
    public Dictionary<int, int> Lines { get; } = new();
}

// Combined (all-flags) per-method CRAP data.
sealed class MethodCrapInfo(string relFile, string className, string name, string signature, int complexity)
{
    public string RelFile { get; } = relFile;
    public string ClassName { get; } = className;
    public string Name { get; } = name;
    public string Signature { get; } = signature;
    public int Complexity { get; } = complexity;
    public Dictionary<int, int> Lines { get; } = new();
    public double Coverage { get; set; }
    public double Crap { get; set; }
}

// One per-file target against its measured value (#1762); Measured is unrounded, null without data.
// TotalLines and CoveredLines are the counts behind Measured; a pair without data may carry a line count
// borrowed from the file's other flags (CoveredLines 0), or null when no flag has the file (#2542).
record FileTargetRow(string Package, string File, string Flag, double Target, double? Measured,
    int? TotalLines = null, int? CoveredLines = null);

// One finding of --check-targets (#1651): Kind is below-target, no-data, parse-error, no-targets, dropped-path, no-coverage or no-manifest.
record TargetFinding(string Kind, string Subject, string Message);

// Target attainment aggregates (#2542); the field names are the contract of latest.json overall.attainment.
record AttainmentSummary(int Pairs, int Met, double? Ratio, double? LineWeighted, double? MeanGap,
    int LinesToCover, int NoData, int Unweighted, int ZeroTarget);

// A source file whose lines a flag executes although the manifest does not assign the flag to it (#2932).
record UnassignedFlagFile(string Package, string File, string Flag, int ExecutedLines, int TotalLines, bool HasFileTarget = false);

// One element of latest.json "fileTargets" (Design Choice 7).
record FileTargetEntry(string Package, string File, string Flag, double Target, double? Measured,
    int? Covered, int? Total, double Gap, double Attainment, int LinesToCover, string Status);

// Measured is false for a package that has a manifest and no measured lines (#2827): its
// Percentage is 0 only as a placeholder and must never be read as a measured 0%.
record PackageCoverage(string Name, int TotalLines, double CoveredEquivalent, double Percentage,
    Dictionary<TestType, (int Total, int Covered)> PerFlag,
    Dictionary<string, double>? PerFlagTarget, List<FileCoverage> Files, bool Measured = true);

record ManifestFile
{
    [JsonPropertyName("package")]
    public string? Package { get; init; }
    [JsonPropertyName("targets")]
    public Dictionary<string, double>? Targets { get; init; }
    [JsonPropertyName("files")]
    public Dictionary<string, ManifestFileEntry>? Files { get; init; }
}

record ManifestFileEntry
{
    [JsonPropertyName("defaultTests")]
    public string[]? DefaultTests { get; init; }
    [JsonPropertyName("override")]
    public string[]? Override { get; init; }
    // Per-file per-flag obligations (#1762): targets are 0-100, every target needs a justification.
    [JsonPropertyName("targets")]
    public Dictionary<string, double>? Targets { get; init; }
    [JsonPropertyName("justifications")]
    public Dictionary<string, string>? Justifications { get; init; }
}

// ─── Zero-target classifier (#2890) ──────────────────────────────────────────────────────────────────
// Decides, by Roslyn SYNTAX analysis only (no compilation, no coverage data), whether a source file may keep a
// per-file target of 0 on unit, guard or contract. The list of exempt constructs is CLOSED (maintainer decision
// of 2026-10-26, option D): it is extended only by a reviewed pull request, never by a marker in the source.
// Per flag, a file is exempt when it has:
//   unit      no executable code: only interfaces, enums, delegates, body-less records, constants,
//             auto-properties, abstract members, [LoggerMessage] partial methods and static
//             Meter / ActivitySource holder fields (any other body or initializer is executable);
//   guard     no non-private method, constructor or primary constructor with a body that takes a
//             reference-type parameter;
//   contract  no public interface, no abstract type, and nothing that implements or extends a type outside the
//             closed list of non-Encina bases.
// A file generated as *.g.cs is exempt for all three (the methodology excludes it from the obligations).

// The verdict for one file and flag: Exempt, and the construct found (what exempts it, or what rejects it).
record ZeroVerdict(bool Exempt, string Construct);

// The shapes of FindRejectedZeros and RunCheckZeros (local functions of the script), for the self-test.
delegate (List<TargetFinding> Findings, int Checked) ZeroFinder(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations, string repoRoot);
delegate int ZeroRunner(
    Dictionary<string, Dictionary<string, ManifestFileEntry>> fileObligations, int manifestCount,
    List<string> manifestParseFailures, string repoRoot, bool enforce, string outputDir);

static class ZeroClassifier
{
    public static readonly HashSet<string> CheckedFlags = new(["unit", "guard", "contract"], StringComparer.Ordinal);

    // Value types named by an identifier (the predefined keywords other than string and object are value types
    // by construction). A name a syntax pass cannot resolve is treated as a reference type: more rejections.
    static readonly HashSet<string> ValueTypeNames = new(
    [
        "DateTime", "DateTimeOffset", "TimeSpan", "Guid", "CancellationToken", "DateOnly", "TimeOnly",
        "Span", "ReadOnlySpan", "Memory", "ReadOnlyMemory", "ArraySegment", "KeyValuePair", "ValueTask", "Nullable"
    ], StringComparer.Ordinal);

    // Base types and interfaces that are not Encina-owned contracts (BCL and Microsoft.Extensions); any other name
    // might be an Encina interface or abstract class, so a type that names one is not exempt for contract.
    static readonly HashSet<string> NonEncinaBases = new(
    [
        "object", "Attribute", "Exception", "ApplicationException", "SystemException", "ArgumentException",
        "InvalidOperationException", "NotSupportedException", "NotImplementedException", "TimeoutException",
        "IDisposable", "IAsyncDisposable", "IEquatable", "IComparable", "IComparer", "IEqualityComparer", "IFormattable",
        "ICloneable", "IConvertible", "IEnumerable", "IEnumerator", "IAsyncEnumerable", "IAsyncEnumerator",
        "ICollection", "IList", "IReadOnlyList", "IReadOnlyCollection", "IDictionary", "IReadOnlyDictionary",
        "ISet", "IReadOnlySet", "ISpanFormattable", "IParsable", "IStructuralEquatable", "ValueType", "Enum",
        "IHostedService", "IHealthCheck", "IValidateOptions", "IConfigureOptions", "IPostConfigureOptions", "JsonConverter"
    ], StringComparer.Ordinal);

    // The entry point: the verdict for the text of one source file under the given flag.
    public static ZeroVerdict Classify(string path, string text, string flag)
    {
        var lowered = flag.ToLowerInvariant();
        if (!CheckedFlags.Contains(lowered))
            return new(false, "the flag is outside the rule (only unit, guard and contract are checked)");
        if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            return new(true, "generated file (*.g.cs)");

        var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview));
        if (tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return new(false, "the source does not parse, so no construct could be found");

        var root = tree.GetRoot();
        // No build symbol is defined here, so the code of an #if branch would be invisible: refuse such a file.
        if (root.DescendantTrivia(descendIntoTrivia: true).Any(t => t.Kind() is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia))
            return new(false, "the file has conditional compilation (#if), which a syntax pass cannot judge");
        var verdict = lowered switch
        {
            "unit" => ClassifyUnit(root),
            "guard" => ClassifyGuard(root),
            _ => ClassifyContract(root)
        };
        return verdict with { Construct = Safe(verdict.Construct) };
    }

    // Replaces control and non-ASCII characters (a manifest key or a source identifier is untrusted text that ends
    // up on a CI console line) and caps the length.
    public static string Safe(string value)
    {
        var sb = new StringBuilder(Math.Min(value.Length, 240));
        foreach (var c in value.Take(240))
            sb.Append(c is >= ' ' and <= '~' ? c : '?');
        return sb.ToString();
    }

    // The text of src/<package>/<key>, or null when the file is missing, unreadable or resolves outside src/.
    public static string? ReadSource(string repoRoot, string package, string key)
    {
        var srcRoot = Path.GetFullPath(Path.Combine(repoRoot, "src"));
        var full = Path.GetFullPath(Path.Combine(srcRoot, package, key.Replace('\\', '/')));
        if (!full.StartsWith(srcRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return null;
        try { return File.Exists(full) ? File.ReadAllText(full) : null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    // ── unit ────────────────────────────────────────────────────────────────────────────────────────

    static ZeroVerdict ClassifyUnit(SyntaxNode root)
    {
        var reason = root.DescendantNodes().Select(ExecutableReason).FirstOrDefault(r => r is not null);
        return reason is not null ? new(false, reason) : new(true, UnitConstructs(root));
    }

    // Why a node is executable code (a body, an initializer, a primary constructor, a top-level statement), or null.
    static string? ExecutableReason(SyntaxNode node) => node switch
    {
        GlobalStatementSyntax => "top-level statements",
        PrimaryConstructorBaseTypeSyntax { ArgumentList.Arguments.Count: > 0 } => "base constructor arguments",
        TypeDeclarationSyntax { ParameterList: not null } t when t.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration
            => $"primary constructor of {t.Identifier.Text}",
        MethodDeclarationSyntax m => MethodReason(m),
        BaseMethodDeclarationSyntax m => m is ConstructorDeclarationSyntax || HasBody(m) ? $"{Describe(m)} with a body" : null,
        BasePropertyDeclarationSyntax p => PropertyReason(p),
        BaseFieldDeclarationSyntax f => FieldReason(f),
        _ => null
    };

    static bool HasBody(BaseMethodDeclarationSyntax m) => m.Body is not null || m.ExpressionBody is not null;

    static string? MethodReason(MethodDeclarationSyntax m)
    {
        if (HasBody(m)) return $"{Describe(m)} with a body";
        if (m.Modifiers.Any(SyntaxKind.PartialKeyword) && !HasLoggerMessage(m))
            return $"partial method {m.Identifier.Text} without [LoggerMessage]";
        return null;
    }

    static string? PropertyReason(BasePropertyDeclarationSyntax p)
    {
        if (p is PropertyDeclarationSyntax { ExpressionBody: not null } || p is IndexerDeclarationSyntax { ExpressionBody: not null })
            return $"{Describe(p)} with an expression body";
        if (p is PropertyDeclarationSyntax { Initializer: { } init } prop && !IsHolderInitializer(prop, prop.Type, init.Value))
            return $"{Describe(p)} with an initializer";
        if (p.AccessorList?.Accessors.Any(a => a.Body is not null || a.ExpressionBody is not null) == true)
            return $"{Describe(p)} with an accessor body";
        return null;
    }

    static string? FieldReason(BaseFieldDeclarationSyntax f)
    {
        if (f.Modifiers.Any(SyntaxKind.ConstKeyword)) return null;
        var isEvent = f is EventFieldDeclarationSyntax;
        var withInitializer = f.Declaration.Variables.FirstOrDefault(
            v => v.Initializer is { } i && (isEvent || !IsHolderInitializer(f, f.Declaration.Type, i.Value)));
        return withInitializer is null ? null : $"{(isEvent ? "event" : "field")} {withInitializer.Identifier.Text} with an initializer";
    }

    // A static Meter / ActivitySource holder member: `new Meter(...)`, `new ActivitySource(...)` or
    // `<meter>.Create...(...)` in a static class, with no lambda in it (a callback is code).
    static bool IsHolderInitializer(MemberDeclarationSyntax member, TypeSyntax declaredType, ExpressionSyntax value)
    {
        if (member.Parent is not TypeDeclarationSyntax owner || !owner.Modifiers.Any(SyntaxKind.StaticKeyword)) return false;
        if (HasNestedCode(value)) return false;
        return value switch
        {
            ObjectCreationExpressionSyntax c => IsHolderType(RightmostName(c.Type)),
            ImplicitObjectCreationExpressionSyntax => IsHolderType(RightmostName(declaredType)),
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } a } =>
                receiver.Identifier.Text.Contains("meter", StringComparison.OrdinalIgnoreCase) && InstrumentFactories.Contains(a.Name.Identifier.Text),
            _ => false
        };
    }

    // The Meter factory methods a holder may call on its own Meter field.
    static readonly HashSet<string> InstrumentFactories = new(
        ["CreateCounter", "CreateHistogram", "CreateUpDownCounter", "CreateGauge"], StringComparer.Ordinal);

    // A lambda, a call (other than nameof) or a creation inside the initializer's arguments is code.
    static bool HasNestedCode(ExpressionSyntax value) => value.DescendantNodes().Any(n =>
        n is AnonymousFunctionExpressionSyntax or BaseObjectCreationExpressionSyntax
        || n is InvocationExpressionSyntax { Expression: not IdentifierNameSyntax { Identifier.Text: "nameof" } });

    static bool IsHolderType(string name) => name is "Meter" or "ActivitySource";

    static bool HasLoggerMessage(MethodDeclarationSyntax m)
        => m.AttributeLists.SelectMany(l => l.Attributes).Any(a => RightmostName(a.Name) is "LoggerMessage" or "LoggerMessageAttribute");

    // The constructs that make a file exempt for unit, sorted, for the report ("no type declarations" when empty).
    static string UnitConstructs(SyntaxNode root)
    {
        var labels = root.DescendantNodes().Select(UnitLabel).OfType<string>().ToHashSet(StringComparer.Ordinal);
        return labels.Count == 0 ? "no executable members" : string.Join(", ", labels.Order(StringComparer.Ordinal));
    }

    static string? UnitLabel(SyntaxNode node) => node switch
    {
        InterfaceDeclarationSyntax => "interface",
        EnumDeclarationSyntax => "enum",
        DelegateDeclarationSyntax => "delegate",
        RecordDeclarationSyntax { ParameterList: not null, Members.Count: 0 } => "body-less record",
        FieldDeclarationSyntax f when f.Modifiers.Any(SyntaxKind.ConstKeyword) => "constants",
        MethodDeclarationSyntax m when m.Modifiers.Any(SyntaxKind.PartialKeyword) && HasLoggerMessage(m) => "[LoggerMessage] partial methods",
        FieldDeclarationSyntax f when f.Declaration.Variables.Any(v => v.Initializer is not null) => "Meter/ActivitySource holder",
        PropertyDeclarationSyntax { Initializer: not null } => "Meter/ActivitySource holder",
        PropertyDeclarationSyntax { Parent: not InterfaceDeclarationSyntax, AccessorList: not null } => "auto-properties",
        _ => null
    };

    // ── guard ───────────────────────────────────────────────────────────────────────────────────────

    static ZeroVerdict ClassifyGuard(SyntaxNode root)
    {
        var hit = root.DescendantNodes().Select(GuardHit).FirstOrDefault(h => h is not null);
        return hit is not null ? new(false, hit)
            : new(true, "no non-private method or constructor with a body takes a reference-type parameter");
    }

    static string? GuardHit(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax m when !IsPrivateMember(m) && (m is ConstructorDeclarationSyntax || HasBody(m))
            => ReferenceParameter(m.ParameterList, Describe(m)),
        IndexerDeclarationSyntax ix when !IsPrivateMember(ix) && (ix.ExpressionBody is not null || ix.AccessorList?.Accessors.Any(a => a.Body is not null || a.ExpressionBody is not null) == true)
            => ReferenceParameter(ix.ParameterList, Describe(ix)),
        // A primary constructor, or the receiver of a C# 14 extension block (the only other type with a parameter list).
        TypeDeclarationSyntax { ParameterList: { } pl } t when t.Kind() == SyntaxKind.ExtensionBlockDeclaration || !IsPrivateMember(t)
            => ReferenceParameter(pl, t.Kind() == SyntaxKind.ExtensionBlockDeclaration ? "extension block" : $"primary constructor of {t.Identifier.Text}"),
        _ => null
    };

    static string? ReferenceParameter(BaseParameterListSyntax list, string owner)
    {
        var p = list.Parameters.FirstOrDefault(p => !p.Modifiers.Any(SyntaxKind.OutKeyword) && p.Type is not null && IsReferenceType(p.Type));
        return p is null ? null : $"{owner} takes the reference-type parameter '{p.Identifier.Text}'";
    }

    static bool IsReferenceType(TypeSyntax type) => type switch
    {
        PredefinedTypeSyntax p => p.Keyword.Text is "string" or "object",
        NullableTypeSyntax n => IsReferenceType(n.ElementType),
        RefTypeSyntax r => IsReferenceType(r.Type),
        _ => !ValueTypeNames.Contains(RightmostName(type))
    };

    // A member without public, protected or internal is private (inside a type) or public (in an interface);
    // an explicit interface implementation is reachable through the interface.
    static bool IsPrivateMember(MemberDeclarationSyntax m)
    {
        if (m.Modifiers.Any(k => k.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword)) return false;
        if (m is MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null }) return false;
        return m.Parent is TypeDeclarationSyntax and not InterfaceDeclarationSyntax;
    }

    // ── contract ────────────────────────────────────────────────────────────────────────────────────

    static ZeroVerdict ClassifyContract(SyntaxNode root)
    {
        var hit = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t => t is not EnumDeclarationSyntax)
            .Select(ContractHit).FirstOrDefault(h => h is not null);
        return hit is not null ? new(false, hit)
            : new(true, "not a public interface or abstract type, and implements nothing outside the closed non-Encina list");
    }

    static string? ContractHit(BaseTypeDeclarationSyntax type)
    {
        var name = type.Identifier.Text;
        if (type is InterfaceDeclarationSyntax && type.Modifiers.Any(SyntaxKind.PublicKeyword)) return $"public interface {name}";
        if (type is not InterfaceDeclarationSyntax && type.Modifiers.Any(SyntaxKind.AbstractKeyword)) return $"abstract type {name}";
        var contract = type.BaseList?.Types.Select(b => RightmostName(b.Type)).FirstOrDefault(n => !NonEncinaBases.Contains(n));
        return contract is null ? null : $"{name} implements or extends '{contract}', which may be an Encina-owned contract";
    }

    // ── shared ──────────────────────────────────────────────────────────────────────────────────────

    static string RightmostName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax i => i.Identifier.Text,
        GenericNameSyntax g => g.Identifier.Text,
        QualifiedNameSyntax q => RightmostName(q.Right),
        AliasQualifiedNameSyntax a => RightmostName(a.Name),
        PredefinedTypeSyntax p => p.Keyword.Text,
        NullableTypeSyntax n => RightmostName(n.ElementType),
        _ => type.ToString()
    };

    // "method Foo", "constructor Bar", "property Baz": a short name of the member for the finding.
    static string Describe(MemberDeclarationSyntax m) => m switch
    {
        MethodDeclarationSyntax x => $"method {x.Identifier.Text}",
        ConstructorDeclarationSyntax x => $"constructor {x.Identifier.Text}",
        PropertyDeclarationSyntax x => $"property {x.Identifier.Text}",
        IndexerDeclarationSyntax => "indexer",
        OperatorDeclarationSyntax => "operator",
        ConversionOperatorDeclarationSyntax => "conversion operator",
        DestructorDeclarationSyntax => "destructor",
        EventDeclarationSyntax x => $"event {x.Identifier.Text}",
        _ => m.Kind().ToString()
    };
}

// The self-test of the classifier (#2890): one passing fixture for EACH exempt construct, so that removing the
// branch that recognises it makes its fixture fail, and a rejected fixture for each way a file leaves the list.
static class ZeroSelfTest
{
    // (name, flag, source): every one must be exempt.
    static readonly (string Name, string Flag, string Source)[] Exempt =
    [
        ("unit: interface", "unit", "public interface IA { string Name { get; } void Run(string a); }"),
        ("unit: enum", "unit", "public enum E { A = 1, B }"),
        ("unit: delegate", "unit", "public delegate void D(string a);"),
        ("unit: constants-only class", "unit", "public static class C { public const int A = 1; public const string B = \"b\"; }"),
        ("unit: body-less record", "unit", "public sealed record R(int A, string B);"),
        ("unit: auto-properties", "unit", "public sealed class P { public string A { get; set; } public int B { get; init; } }"),
        ("unit: abstract members", "unit", "public abstract class B { public abstract void Run(string a); public abstract int X { get; } }"),
        ("unit: [LoggerMessage] partial", "unit", "internal static partial class L { [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = \"m\")] public static partial void Warn(ILogger logger, string a); }"),
        ("unit: Meter holder", "unit", "internal static class M { internal const string N = \"n\"; internal static readonly Meter Meter = new(N, \"1.0\"); internal static readonly Counter<long> T = Meter.CreateCounter<long>(\"t\"); }"),
        ("unit: ActivitySource holder", "unit", "internal static class A { internal static readonly ActivitySource Source = new ActivitySource(\"s\", \"1.0\"); }"),
        ("unit: field-like event and uninitialised field", "unit", "public abstract class B { public event EventHandler Changed; protected int count; }"),
        ("unit: no types (usings and assembly attribute)", "unit", "using System; [assembly: CLSCompliant(true)]"),
        ("guard: no parameters", "guard", "public class G { public void Run() { } public G() { } }"),
        ("guard: value-type parameters only", "guard", "public class G { public int Add(int a, long b, DateTime c, CancellationToken t, int? n) => 1; }"),
        ("guard: out parameter", "guard", "public static class G { public static bool Try(out string value) { value = null; return false; } }"),
        ("guard: private method", "guard", "public class G { private void Run(string a) { } void Other(object o) { } }"),
        ("guard: bodiless interface and abstract members", "guard", "public interface IG { void Run(string a); } public abstract class B { public abstract void Run(string a); }"),
        ("guard: record with value-type primary constructor", "guard", "public sealed record R(int A, bool B);"),
        ("contract: static class", "contract", "public static class C { public static void Run(string a) { } }"),
        ("contract: concrete class with no base", "contract", "public sealed class C { public void Run() { } }"),
        ("contract: enum", "contract", "public enum E : byte { A }"),
        ("contract: internal interface", "contract", "internal interface I { void Run(); }"),
        ("contract: BCL bases only", "contract", "public sealed class X : Exception, IDisposable, IEquatable<X> { public void Dispose() { } public bool Equals(X o) => true; }"),
    ];

    // (name, flag, source, fragment of the construct found): every one must be rejected, naming the construct.
    static readonly (string Name, string Flag, string Source, string Found)[] Rejected =
    [
        ("unit: method with a body", "unit", "public class C { public int A() => 1; }", "method A with a body"),
        ("unit: constructor", "unit", "public class C { public C() { } }", "constructor C with a body"),
        ("unit: property with an initializer", "unit", "public class C { public string A { get; set; } = string.Empty; }", "property A with an initializer"),
        ("unit: property with an expression body", "unit", "public class C { public int A => 1; }", "property A with an expression body"),
        ("unit: property accessor body", "unit", "public class C { int f; public int A { get { return f; } } }", "accessor body"),
        ("unit: field initializer outside a holder", "unit", "public class C { static readonly string A = \"a\"; }", "field A with an initializer"),
        ("unit: Meter field in a class that is not static", "unit", "public class C { readonly Meter M = new(\"n\"); }", "field M with an initializer"),
        ("unit: holder with a callback", "unit", "internal static class M { static readonly Meter Meter = new(\"n\"); static readonly ObservableGauge<int> G = Meter.CreateObservableGauge(\"g\", () => 1); }", "field G with an initializer"),
        ("unit: partial method without [LoggerMessage]", "unit", "public static partial class L { public static partial void Run(string a); }", "partial method Run without [LoggerMessage]"),
        ("unit: static factory call that is not a Meter", "unit", "public static class K { public static readonly Order D = OrderFactory.CreateDefault(); }", "field D with an initializer"),
        ("unit: holder whose arguments call code", "unit", "internal static class M { internal static readonly Meter Meter = new(Name()); }", "field Meter with an initializer"),
        ("unit: event with an initializer", "unit", "public class C { public event EventHandler E = Make(); }", "event E with an initializer"),
        ("unit: record with base constructor arguments", "unit", "public record R(int A) : Base(Compute());", "base constructor arguments"),
        ("unit: conditional compilation", "unit", "#if NET10_0_OR_GREATER\npublic class C { public void A() { } }\n#endif", "conditional compilation"),
        ("guard: extension block receiver", "guard", "public static class E { extension(IServiceCollection s) { public IServiceCollection AddX() { return s; } } }", "extension block takes the reference-type parameter 's'"),
        ("unit: primary constructor of a class", "unit", "public class C(string a);", "primary constructor of C"),
        ("unit: top-level statements", "unit", "System.Console.WriteLine(1);", "top-level statements"),
        ("unit: default interface method", "unit", "public interface I { int A() => 1; }", "method A with a body"),
        ("guard: method with a reference-type parameter", "guard", "public class G { public void Run(string a) { } }", "method Run takes the reference-type parameter 'a'"),
        ("guard: expression-bodied method", "guard", "public static class G { public static int Len(object o) => 1; }", "method Len takes the reference-type parameter 'o'"),
        ("guard: constructor", "guard", "public class G { public G(IFoo foo) { } }", "constructor G takes the reference-type parameter 'foo'"),
        ("guard: positional record with a reference-type parameter", "guard", "public sealed record R(string A);", "primary constructor of R takes the reference-type parameter 'A'"),
        ("guard: internal method", "guard", "internal static class G { internal static void Run(Foo foo) { } }", "method Run takes the reference-type parameter 'foo'"),
        ("guard: explicit interface implementation", "guard", "class G : IG { void IG.Run(string a) { } }", "method Run takes the reference-type parameter 'a'"),
        ("guard: indexer", "guard", "public class G { public int this[string k] { get { return 1; } } }", "indexer takes the reference-type parameter 'k'"),
        ("contract: public interface", "contract", "public interface IA { }", "public interface IA"),
        ("contract: abstract class", "contract", "public abstract class B { }", "abstract type B"),
        ("contract: class implementing an interface", "contract", "public sealed class S : IOutboxStore { }", "S implements or extends 'IOutboxStore'"),
        ("contract: generic and qualified base", "contract", "public sealed record Q : Encina.IRequest<int> { }", "Q implements or extends 'IRequest'"),
        ("contract: derived from an unknown base", "contract", "public class D : SaveChangesInterceptor { }", "D implements or extends 'SaveChangesInterceptor'"),
        ("a flag outside the rule", "integration", "public enum E { A }", "outside the rule"),
        ("source that does not parse", "unit", "public class { ", "does not parse"),
    ];

    // Returns the failed checks; passedChecks is how many held.
    public static List<string> Run(out int passedChecks)
    {
        var failures = new List<string>();
        var passed = 0;
        void Check(bool ok, string name) { if (ok) passed++; else failures.Add(name); }

        foreach (var (name, flag, source) in Exempt)
        {
            var verdict = ZeroClassifier.Classify("F.cs", source, flag);
            Check(verdict.Exempt, $"zero classifier exempts: {name} (got: {verdict.Construct})");
        }
        foreach (var (name, flag, source, found) in Rejected)
        {
            var verdict = ZeroClassifier.Classify("F.cs", source, flag);
            Check(!verdict.Exempt && verdict.Construct.Contains(found, StringComparison.Ordinal),
                $"zero classifier rejects: {name} (got: {verdict.Construct})");
        }

        Check(ZeroClassifier.Classify("Gen.g.cs", "public class C { public void A(string s) { } }", "guard").Exempt, "a generated *.g.cs file is exempt");
        Check(!ZeroClassifier.Classify("Gen.cs", "public class C { public void A(string s) { } }", "guard").Exempt, "a file that only looks generated is not");
        Check(ZeroClassifier.Classify("F.cs", "public interface I { }", "UNIT").Exempt, "the flag is case-insensitive");
        Check(ZeroClassifier.Classify("F.cs", "public enum E { A }", "unit").Construct == "enum", "the construct of an exempt file is named");
        Check(ZeroClassifier.Safe("a\r\nb\u0001cé") == "a??b?c?", "control and non-ASCII characters are replaced");
        Check(ZeroClassifier.Safe(new string('x', 500)).Length == 240, "a long text is capped");

        passedChecks = passed;
        return failures;
    }

    // ReadSource, FindRejectedZeros and RunCheckZeros against a temporary repository. The two runners are local
    // functions of the script, so they come in as delegates.
    public static List<string> CheckFiles(
        ZeroFinder findRejected, ZeroRunner runCheck, out int passedChecks)
    {
        var failures = new List<string>();
        var passed = 0;
        void check(bool ok, string name) { if (ok) passed++; else failures.Add(name); }
        FilesScenario(check, findRejected, runCheck);
        passedChecks = passed;
        return failures;
    }

    static void FilesScenario(Action<bool, string> check, ZeroFinder FindRejectedZeros, ZeroRunner RunCheckZeros)
    {
        var root = Path.Combine(Path.GetTempPath(), "coverage-report-selftest-zeros-" + Guid.NewGuid().ToString("N"));
        try
        {
            var pkg = Directory.CreateDirectory(Path.Combine(root, "src", "Pkg"));
            File.WriteAllText(Path.Combine(pkg.FullName, "Exempt.cs"), "public enum E { A }");
            File.WriteAllText(Path.Combine(pkg.FullName, "Code.cs"), "public class C { public void A(string s) { } }");
            File.WriteAllText(Path.Combine(root, "Outside.cs"), "public enum E { A }");

            check(ZeroClassifier.ReadSource(root, "Pkg", "Exempt.cs") is not null, "a source under src/ is read");
            check(ZeroClassifier.ReadSource(root, "Pkg", "Missing.cs") is null, "a missing source reads as null");
            check(ZeroClassifier.ReadSource(root, "Pkg", "../../Outside.cs") is null, "a key that leaves src/ is refused");
            check(ZeroClassifier.ReadSource(root, "..", "Outside.cs") is null, "a package that leaves src/ is refused");

            static ManifestFileEntry Entry(params (string Flag, double Value)[] targets) => new()
            {
                Targets = targets.ToDictionary(t => t.Flag, t => t.Value),
                Justifications = targets.ToDictionary(t => t.Flag, _ => "x")
            };
            var obligations = new Dictionary<string, Dictionary<string, ManifestFileEntry>>
            {
                ["Pkg"] = new()
                {
                    ["Exempt.cs"] = Entry(("unit", 0), ("guard", 50)),
                    ["Code.cs"] = Entry(("Guard", 0), ("integration", 0), ("property", 0), ("unit", 80)),
                    ["Missing.cs"] = Entry(("contract", 0)),
                    ["../../Outside.cs"] = Entry(("unit", 0))
                }
            };
            var (findings, checkedZeros) = FindRejectedZeros(obligations, root);
            check(checkedZeros == 4, "only unit, guard and contract zeros are checked (integration, property and non-zero targets are not)");
            check(findings.Count == 3 && findings.Count(f => f.Kind == "rejected-zero") == 1 && findings.Count(f => f.Kind == "no-source") == 2,
                "one rejected zero and two unreadable sources are findings; the exempt zero is not");
            check(findings.Any(f => f.Kind == "rejected-zero" && f.Subject == "Pkg: Code.cs guard" && f.Message.Contains("'s'", StringComparison.Ordinal)),
                "a rejected zero names the file, the flag and the construct");

            var outDir = Path.Combine(root, "out");
            check(RunCheckZeros(obligations, 1, [], root, enforce: false, outDir) == 0, "report-only never fails the run");
            var first = File.ReadAllText(Path.Combine(outDir, "check-zeros.json"));
            check(RunCheckZeros(obligations, 1, [], root, enforce: true, outDir) == 1, "enforce fails the run on a finding");
            check(File.ReadAllText(Path.Combine(outDir, "check-zeros.json")).Replace("\"enforce\": true", "\"enforce\": false") == first,
                "check-zeros.json is deterministic apart from the enforce flag");
            var written = JsonNode.Parse(first)!.AsObject();
            check(string.Join(",", written.Select(kv => kv.Key).Order(StringComparer.Ordinal)) == "byPackage,checkedZeros,enforce,findings,passed,rejectedZeros",
                "check-zeros.json has exactly the documented fields");
            check(RunCheckZeros([], 1, [], root, enforce: true, outDir) == 0, "enforce passes with no zero");
            check(RunCheckZeros([], 0, [], root, enforce: true, outDir) == 1, "no manifest at all is a finding, never a pass");
            check(RunCheckZeros([], 1, ["Broken.json"], root, enforce: true, outDir) == 1, "an unparsed manifest is a finding, never a pass");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
