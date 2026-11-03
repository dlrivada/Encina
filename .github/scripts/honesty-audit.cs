// honesty-audit.cs — Weekly audit of the evidence behind the test obligations (#2899, Phase 4 of EPIC #2845).
//
// Re-reads the repository tree (no build, no tests) and counts four kinds of drift:
//   H1  per-file flag targets equal to 0, from .github/coverage-manifest/*.json. INFORMATIONAL ONLY for now:
//       the rule "zeros of files with executable lines" needs the #2890 classifier, so the all-zeros total is
//       reported and never fails the run. H1 waits for #2890.
//   H2  `Status: Not Implemented` justification .md files under tests/, per test project; those in a project where
//       AGENTS.md section 9 forbids them (UnitTests, GuardTests, ContractTests); stale candidates (a benchmark or
//       load-test file whose every named package already has that test type). Only the forbidden count gates.
//   H3  reflection-only contract test files: tests/Encina.ContractTests/**/*.cs that contain [Fact] or [Theory]
//       and none of the tokens that mean product code runs (the heuristic written in #1749). Files and attributes gate.
//   H4  boilerplate justification strings in the manifests: total, distinct, most repeated. Reported only.
//
// The gating counts are compared with a committed strict-ratchet baseline (.github/honesty-audit-baseline.json,
// decisions D7, D10, D11 and D13 of #2899): a count above its ceiling or an item not in the baseline (RISE), and a
// count below it or an accepted item that no longer occurs (IMPROVED), both fail the run, so the pull request that
// fixes drift regenerates the file with --print-baseline in the same change. A run never raises a ceiling; a
// deliberate rise is a pull request that raises the baseline (the maintainer-label rule is tracked by #3134).
//
// Usage (from the repository root, or pass --root):
//   dotnet run --file .github/scripts/honesty-audit.cs -- \
//       [--output artifacts/honesty-audit/report.json] \
//       --baseline .github/honesty-audit-baseline.json (or --no-baseline for a report-only run) \
//       [--summary "$GITHUB_STEP_SUMMARY"] [--root <repo root>]
//   dotnet run --file .github/scripts/honesty-audit.cs -- --print-baseline [--root <repo root>]
//   dotnet run --file .github/scripts/honesty-audit.cs -- --self-test
//
// Exit codes: 0 every gating state equals its baseline (or --no-baseline); 1 at least one RISE or IMPROVED;
// 2 invalid arguments, no --baseline, unreadable or missing repository evidence, or an invalid baseline.
// Requires: .NET 10+ (C# 14 file-based app).
#:property JsonSerializerIsReflectionEnabledByDefault=true
#pragma warning disable CA1305, CA1310, CA1859, CA1852, CA1812

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

string? rootArg = null, outputPath = null, baselinePath = null, summaryPath = null;
var selfTest = false;
var printBaseline = false;
var noBaseline = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--self-test": selfTest = true; break;
        case "--print-baseline": printBaseline = true; break;
        case "--no-baseline": noBaseline = true; break;
        case "--root" when i + 1 < args.Length: rootArg = args[++i]; break;
        case "--output" when i + 1 < args.Length: outputPath = args[++i]; break;
        case "--baseline" when i + 1 < args.Length: baselinePath = args[++i]; break;
        case "--summary" when i + 1 < args.Length: summaryPath = args[++i]; break;
        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            return 2;
    }
}

if (selfTest)
{
    return SelfTest.Run();
}

string? root;
try
{
    root = rootArg is not null ? Path.GetFullPath(rootArg) : Audit.FindRoot(Directory.GetCurrentDirectory());
}
catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
{
    Console.Error.WriteLine($"Invalid --root: {ex.GetType().Name}");
    return 2;
}

if (root is null || !Directory.Exists(Path.Combine(root, ".github", "coverage-manifest")))
{
    Console.Error.WriteLine("Repository root not found (no .github/coverage-manifest); run from the repository root or pass --root.");
    return 2;
}

// A run that gates must be given its baseline: forgetting --baseline must not turn the audit into a silent pass.
if (!printBaseline && baselinePath is null && !noBaseline)
{
    Console.Error.WriteLine("No baseline: pass --baseline <file> (or --no-baseline for a report-only run, or --print-baseline).");
    return 2;
}

Report report;
try
{
    report = Audit.Measure(root, TimeProvider.System.GetUtcNow());
}
catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Cannot read the repository: {ex.GetType().Name}");
    return 2;
}

if (printBaseline)
{
    Console.WriteLine(Audit.BaselineText(report));
    return 0;
}

Baseline? baseline = null;
if (baselinePath is not null)
{
    try
    {
        baseline = Baseline.Load(Path.GetFullPath(baselinePath));
    }
    catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
    {
        Console.Error.WriteLine($"Cannot read the baseline '{baselinePath}': {ex.GetType().Name}");
        return 2;
    }
}

var evaluation = Audit.Evaluate(report, baseline);
report.Checks = evaluation.Checks;

var summary = Audit.Summary(report);
Console.WriteLine(summary);
try
{
    if (outputPath is not null)
    {
        var full = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, JsonSerializer.Serialize(report, Audit.JsonOptions));
    }

    if (!string.IsNullOrEmpty(summaryPath))
    {
        File.AppendAllText(summaryPath, summary + Environment.NewLine);
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
{
    Console.Error.WriteLine($"Cannot write the output: {ex.GetType().Name}");
    return 2;
}

return evaluation.ExitCode;

// ----------------------------------------------------------------------------------------------------------------

sealed class Report
{
    public string GeneratedAtUtc { get; set; } = "";
    public H1Result H1 { get; set; } = new();
    public H2Result H2 { get; set; } = new();
    public H3Result H3 { get; set; } = new();
    public H4Result H4 { get; set; } = new();
    public List<CheckResult> Checks { get; set; } = [];
}

sealed class H1Result
{
    // H1 waits for #2890: this is the all-zeros total only; it never fails the run.
    public string Note { get; set; } = "Informational only. The gating H1 rule (zeros of files with executable lines) waits for the #2890 classifier.";
    public int AllZerosTotal { get; set; }
    public SortedDictionary<string, int> ByFlag { get; set; } = [];
    public List<string> Items { get; set; } = [];
}

sealed class H2Result
{
    public int Total { get; set; }
    public SortedDictionary<string, int> ByProject { get; set; } = [];
    public List<string> Forbidden { get; set; } = [];
    public List<string> StaleCandidates { get; set; } = [];
}

sealed class H3Result
{
    public string Detector { get; set; } = "";
    public int Files { get; set; }
    public int Attributes { get; set; }
    public List<H3File> Flagged { get; set; } = [];
}

sealed record H3File(string File, int Attributes);

sealed class H4Result
{
    public int Total { get; set; }
    public int Distinct { get; set; }
    public List<RepeatedText> MostRepeated { get; set; } = [];
}

sealed record RepeatedText(string Text, int Count);

sealed class CheckResult
{
    public string Name { get; set; } = "";
    public bool Gating { get; set; }
    public int Current { get; set; }
    public int? Ceiling { get; set; }
    public int? Delta { get; set; }
    public string Status { get; set; } = "";
    public List<string> NewItems { get; set; } = [];
    // Accepted items that no longer occur: the baseline must be lowered (strict ratchet).
    public List<string> GoneItems { get; set; } = [];
}

sealed record Evaluation(int ExitCode, List<CheckResult> Checks);

sealed class Baseline
{
    public int Schema { get; set; } = 1;
    // H1 has no ceiling until #2890 lands: the entry exists so the shape can take it later.
    public CeilingEntry H1 { get; set; } = new();
    public CeilingEntry H2Forbidden { get; set; } = new();
    public H3Entry H3 { get; set; } = new();

    public static Baseline Load(string path)
    {
        var b = JsonSerializer.Deserialize<Baseline>(File.ReadAllText(path), Audit.JsonOptions)
            ?? throw new InvalidDataException("empty baseline");
        if (b.Schema != 1)
        {
            throw new InvalidDataException("unsupported baseline schema");
        }

        // JSON null must be a bad baseline, never a section or list that silently skips a check (fail closed).
        if (b.H1 is null || b.H2Forbidden is null || b.H3 is null
            || b.H1.Items is null || b.H2Forbidden.Items is null || b.H3.Items is null)
        {
            throw new InvalidDataException("baseline section or items list is null");
        }
        return b;
    }
}

sealed class CeilingEntry
{
    public int? Ceiling { get; set; }
    public List<string> Items { get; set; } = [];
}

sealed class H3Entry
{
    public int? FilesCeiling { get; set; }
    public int? AttributesCeiling { get; set; }
    public List<string> Items { get; set; } = [];
}

static class Audit
{
    public const int SummaryCap = 20;

    static readonly string[] ForbiddenProjects = ["Encina.UnitTests", "Encina.GuardTests", "Encina.ContractTests"];
    static readonly string[] StaleProjects = ["Encina.BenchmarkTests", "Encina.LoadTests"];

    // `## Status: Not Implemented` (the form of the real files), also as a plain or bold `Status:` line, any case.
    static readonly Regex NotImplemented = new(@"^[ \t]*(?:#{1,6}[ \t]*)?\*{0,2}Status\*{0,2}[ \t]*:[ \t]*\*{0,2}[ \t]*Not[ \t]+Implemented\b", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex EncinaToken = new(@"Encina(?:\.[A-Za-z0-9_]+)+", RegexOptions.Compiled);
    static readonly Regex TestAttribute = new(@"\[(?:Fact|Theory)\b", RegexOptions.Compiled);

    // The tokens of #1749: any of these means the file can run product code.
    static readonly Regex[] ProductCodeTokens =
    [
        new(@"\bnew\b", RegexOptions.Compiled),
        new(@"\bCreate\w*\s*\(", RegexOptions.Compiled),
        new(@"\bawait\b", RegexOptions.Compiled),
        new(@"Substitute\.For", RegexOptions.Compiled),
        new(@"BuildServiceProvider", RegexOptions.Compiled),
        new(@"GetRequiredService", RegexOptions.Compiled),
        new(@"\.Invoke\s*\(", RegexOptions.Compiled),
        new(@"\.Compute\s*\(", RegexOptions.Compiled),
        new(@"\.Validate\w*\s*\(", RegexOptions.Compiled),
    ];

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static string? FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".github", "coverage-manifest")))
            {
                return dir.FullName;
            }
        }
        return null;
    }

    static string Rel(string root, string file) => Path.GetRelativePath(root, file).Replace('\\', '/');

    // bin/obj are filtered on the path below `dir`, so a checkout under a directory named bin or obj still scans.
    static IEnumerable<string> Files(string dir, string pattern) =>
        !Directory.Exists(dir)
            ? []
            : Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories)
                .Where(f => !Path.GetRelativePath(dir, f).Split(Path.DirectorySeparatorChar)[..^1]
                    .Any(seg => seg is "bin" or "obj"));

    public static Report Measure(string root, DateTimeOffset now)
    {
        // Missing evidence is an error, never an improvement: an empty scan must not read as zero drift.
        foreach (var required in new[] { "src", "tests", Path.Combine("tests", "Encina.ContractTests") })
        {
            if (!Directory.Exists(Path.Combine(root, required)))
            {
                throw new DirectoryNotFoundException($"required directory missing: {required}");
            }
        }

        var report = new Report { GeneratedAtUtc = now.ToString("yyyy-MM-ddTHH:mm:ssZ") };
        MeasureManifests(root, report);
        MeasureJustificationFiles(root, report);
        MeasureReflectionOnly(root, report);
        return report;
    }

    // H1 (informational) and H4 both read the manifests.
    static void MeasureManifests(string root, Report report)
    {
        var texts = new List<string>();
        foreach (var path in Files(Path.Combine(root, ".github", "coverage-manifest"), "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            if (Path.GetFileName(path).Equals("defaults.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var package = Path.GetFileNameWithoutExtension(path);
            foreach (var file in files.EnumerateObject())
            {
                if (file.Value.TryGetProperty("targets", out var targets) && targets.ValueKind == JsonValueKind.Object)
                {
                    foreach (var t in targets.EnumerateObject())
                    {
                        if (t.Value.ValueKind == JsonValueKind.Number && t.Value.TryGetInt32(out var v) && v == 0)
                        {
                            report.H1.AllZerosTotal++;
                            report.H1.ByFlag[t.Name] = report.H1.ByFlag.GetValueOrDefault(t.Name) + 1;
                            report.H1.Items.Add($"{package}:{file.Name}:{t.Name}");
                        }
                    }
                }

                if (file.Value.TryGetProperty("justifications", out var just) && just.ValueKind == JsonValueKind.Object)
                {
                    foreach (var j in just.EnumerateObject())
                    {
                        if (j.Value.ValueKind == JsonValueKind.String && j.Value.GetString() is { Length: > 0 } s)
                        {
                            texts.Add(s);
                        }
                    }
                }
            }
        }

        var groups = texts.GroupBy(t => t, StringComparer.Ordinal).ToList();
        report.H4.Total = texts.Count;
        report.H4.Distinct = groups.Count;
        report.H4.MostRepeated = groups
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(5).Select(g => new RepeatedText(g.Key, g.Count())).ToList();
    }

    static void MeasureJustificationFiles(string root, Report report)
    {
        var testsDir = Path.Combine(root, "tests");
        var packages = PackageNames(root);
        var referencedCache = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var md in Files(testsDir, "*.md").OrderBy(p => p, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(md);
            if (!NotImplemented.IsMatch(text))
            {
                continue;
            }

            var rel = Rel(root, md);
            var relParts = rel.Split('/');
            var project = relParts.Length > 2 ? relParts[1] : "(tests root)";
            report.H2.Total++;
            report.H2.ByProject[project] = report.H2.ByProject.GetValueOrDefault(project) + 1;

            if (ForbiddenProjects.Contains(project, StringComparer.Ordinal))
            {
                report.H2.Forbidden.Add(rel);
            }

            if (StaleProjects.Contains(project, StringComparer.Ordinal))
            {
                var named = EncinaToken.Matches(text).SelectMany(m => Prefixes(m.Value)).Where(packages.Contains).ToHashSet(StringComparer.Ordinal);
                if (named.Count > 0)
                {
                    if (!referencedCache.TryGetValue(project, out var referenced))
                    {
                        referenced = ReferencedPackages(Path.Combine(testsDir, project), packages);
                        referencedCache[project] = referenced;
                    }

                    if (named.All(referenced.Contains))
                    {
                        report.H2.StaleCandidates.Add(rel);
                    }
                }
            }
        }
    }

    static HashSet<string> PackageNames(string root)
    {
        var src = Path.Combine(root, "src");
        return Directory.Exists(src)
            ? Directory.GetDirectories(src).Select(d => Path.GetFileName(d)).Where(n => n.StartsWith("Encina", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal)
            : [];
    }

    static IEnumerable<string> Prefixes(string dotted)
    {
        var parts = dotted.Split('.');
        for (var n = 1; n <= parts.Length; n++)
        {
            yield return string.Join('.', parts.Take(n));
        }
    }

    // Packages that the .cs files of a test project name (usings, namespaces, type references).
    static HashSet<string> ReferencedPackages(string projectDir, HashSet<string> packages)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cs in Files(projectDir, "*.cs"))
        {
            foreach (Match m in EncinaToken.Matches(File.ReadAllText(cs)))
            {
                foreach (var p in Prefixes(m.Value).Where(packages.Contains))
                {
                    set.Add(p);
                }
            }
        }
        return set;
    }

    static void MeasureReflectionOnly(string root, Report report)
    {
        // A heuristic, not proof: any `new`, `await` or `.Validate(` (even a `where T : new()` constraint) removes a file
        // from the count, only [Fact]/[Theory] are counted and only tests/Encina.ContractTests is scanned. The gate is a
        // ratchet on this heuristic.
        report.H3.Detector = "#1749 token heuristic over code with comments and string literals removed (interpolation holes stay code): a tests/Encina.ContractTests *.cs file with [Fact]/[Theory] and none of new, Create...(, await, Substitute.For, BuildServiceProvider, GetRequiredService, .Invoke(, .Compute(, .Validate...(. A heuristic, not proof.";
        foreach (var cs in Files(Path.Combine(root, "tests", "Encina.ContractTests"), "*.cs").OrderBy(p => p, StringComparer.Ordinal))
        {
            var code = StripCommentsAndStrings(File.ReadAllText(cs));
            var attributes = TestAttribute.Count(code);
            if (attributes == 0 || ProductCodeTokens.Any(t => t.IsMatch(code)))
            {
                continue;
            }

            report.H3.Flagged.Add(new H3File(Rel(root, cs), attributes));
            report.H3.Attributes += attributes;
        }
        report.H3.Files = report.H3.Flagged.Count;
    }

    // Removes comments, string literals (regular, verbatim, interpolated, raw) and char literals so a word in a
    // comment or a message cannot count as product code.
    public static string StripCommentsAndStrings(string s)
    {
        var sb = new StringBuilder(s.Length);
        ScanCode(s, 0, sb, false);
        return sb.ToString();
    }

    static bool StartsString(string s, int i)
    {
        var j = i;
        while (j < s.Length && s[j] is '$' or '@') { j++; }
        return j < s.Length && s[j] == '"';
    }

    // Copies code to sb (comments and literal text removed). Interpolation holes are code, so a `new` inside
    // `$"{new Foo()}"` still counts, while a quoted "new" inside a hole does not. With inHole it stops after the
    // `}` that closes the hole and returns the index after it.
    static int ScanCode(string s, int i, StringBuilder sb, bool inHole)
    {
        var braces = 0;
        var parens = 0;
        while (i < s.Length)
        {
            var c = s[i];
            var next = i + 1 < s.Length ? s[i + 1] : '\0';
            if (c == '/' && next == '/')
            {
                while (i < s.Length && s[i] != '\n') { i++; }
                continue;
            }

            if (c == '/' && next == '*')
            {
                var end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? s.Length : end + 2;
                continue;
            }

            if (c == '"' || ((c is '$' or '@') && StartsString(s, i)))
            {
                i = ScanString(s, i, sb);
                continue;
            }

            if (c == '@' && (char.IsLetter(next) || next == '_'))
            {
                // A verbatim identifier such as @new is not the keyword.
                sb.Append("@X");
                i++;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) { i++; }
                continue;
            }

            if (c == '\'')
            {
                var close = next == '\\' ? s.IndexOf('\'', i + 3) : (i + 2 < s.Length && s[i + 2] == '\'' ? i + 2 : -1);
                if (close > 0 && close - i <= 10)
                {
                    sb.Append("''");
                    i = close + 1;
                    continue;
                }
            }

            if (inHole)
            {
                if (c == '{')
                {
                    braces++;
                }
                else if (c == '}')
                {
                    if (braces == 0)
                    {
                        return i + 1;
                    }
                    braces--;
                }
                else if (c is '(' or '[')
                {
                    parens++;
                }
                else if (c is ')' or ']')
                {
                    parens--;
                }
                else if (c == ':' && next == ':')
                {
                    sb.Append("::");
                    i += 2;
                    continue;
                }
                else if (c == ':' && braces == 0 && parens <= 0)
                {
                    // The rest of the hole is a format string (literal text).
                    while (i < s.Length && s[i] != '}') { i++; }
                    return i < s.Length ? i + 1 : i;
                }
            }

            sb.Append(c);
            i++;
        }
        return i;
    }

    // Skips one string literal (regular, verbatim, interpolated, raw, raw interpolated) that starts at i and
    // returns the index after it; sb receives an empty-string placeholder plus the code of every interpolation hole.
    static int ScanString(string s, int i, StringBuilder sb)
    {
        var dollars = 0;
        var verbatim = false;
        while (i < s.Length && s[i] is '$' or '@')
        {
            if (s[i] == '$') { dollars++; } else { verbatim = true; }
            i++;
        }

        var quotes = 0;
        while (i < s.Length && s[i] == '"' && (!verbatim || quotes == 0)) { quotes++; i++; }
        sb.Append("\"\"");
        if (quotes == 2)
        {
            return i; // the empty string ""
        }

        var raw = quotes >= 3;
        while (i < s.Length)
        {
            var c = s[i];
            if (raw)
            {
                if (c == '"')
                {
                    var run = 0;
                    while (i + run < s.Length && s[i + run] == '"') { run++; }
                    if (run >= quotes) { return i + run; }
                    i += run;
                    continue;
                }
            }
            else
            {
                if (c == '"')
                {
                    if (verbatim && i + 1 < s.Length && s[i + 1] == '"') { i += 2; continue; }
                    return i + 1;
                }
                if (c == '\\' && !verbatim) { i += 2; continue; }
            }

            if (dollars > 0 && c == '{')
            {
                var run = 0;
                while (i + run < s.Length && s[i + run] == '{') { run++; }
                if (raw)
                {
                    if (run < dollars) { i += run; continue; }
                    i += run; // extra braces are text; the last `dollars` open the hole
                }
                else
                {
                    if (run >= 2) { i += 2; continue; } // {{ is an escaped brace
                    i++;
                }

                sb.Append(' ');
                i = ScanCode(s, i, sb, true);
                sb.Append(' ');
                for (var extra = raw ? dollars - 1 : 0; extra > 0 && i < s.Length && s[i] == '}'; extra--) { i++; }
                continue;
            }

            i++;
        }
        return i;
    }

    public static Evaluation Evaluate(Report report, Baseline? baseline)
    {
        var checks = new List<CheckResult>
        {
            // H1 waits for #2890: informational, never gating.
            Info("H1 zero targets (all zeros, informational; gating rule waits for #2890)", report.H1.AllZerosTotal),
            Gate("H2 forbidden-location justification files", report.H2.Forbidden.Count, baseline?.H2Forbidden.Ceiling, baseline?.H2Forbidden.Items, report.H2.Forbidden, baseline is not null),
            Info("H2 Not Implemented files (total)", report.H2.Total),
            Info("H2 stale candidates (proxy)", report.H2.StaleCandidates.Count),
            Gate("H3 reflection-only contract test files", report.H3.Files, baseline?.H3.FilesCeiling, baseline?.H3.Items, report.H3.Flagged.Select(f => f.File).ToList(), baseline is not null),
            Gate("H3 reflection-only test attributes", report.H3.Attributes, baseline?.H3.AttributesCeiling, null, [], baseline is not null),
            Info("H4 justification strings (total)", report.H4.Total),
        };

        // Strict ratchet: a gating count above its ceiling (RISE) and one below it (IMPROVED) both fail, so the
        // pull request that fixes drift must lower the baseline in the same change.
        var exit = checks.Any(c => c.Status == "MISSING CEILING") ? 2
            : checks.Any(c => c.Gating && c.Status is "RISE" or "IMPROVED") ? 1 : 0;
        return new Evaluation(exit, checks);
    }

    static CheckResult Info(string name, int current) =>
        new() { Name = name, Gating = false, Current = current, Status = "reported" };

    static CheckResult Gate(string name, int current, int? ceiling, List<string>? accepted, List<string> currentItems, bool hasBaseline)
    {
        var c = new CheckResult { Name = name, Gating = true, Current = current, Ceiling = ceiling };
        if (!hasBaseline)
        {
            c.Status = "no baseline";
            return c;
        }

        if (ceiling is null)
        {
            // Fail closed: a baseline that lacks the ceiling of a gating check must not switch the gate off.
            c.Status = "MISSING CEILING";
            return c;
        }

        c.Delta = current - ceiling;
        c.Status = current > ceiling ? "RISE" : current < ceiling ? "IMPROVED" : "ok";
        if (accepted is not null)
        {
            // An item that is not in the accepted list is a rise even when the count is equal (a swap) or lower;
            // an accepted item that no longer occurs means the baseline must be lowered.
            var known = accepted.ToHashSet(StringComparer.Ordinal);
            var present = currentItems.ToHashSet(StringComparer.Ordinal);
            c.NewItems = currentItems.Where(i => !known.Contains(i)).ToList();
            c.GoneItems = accepted.Where(a => !present.Contains(a)).ToList();
            if (c.NewItems.Count > 0)
            {
                c.Status = "RISE";
            }
            else if (c.GoneItems.Count > 0)
            {
                c.Status = "IMPROVED";
            }
        }
        return c;
    }

    public static string BaselineText(Report report)
    {
        var b = new Baseline
        {
            H1 = new CeilingEntry { Ceiling = null, Items = [] },
            H2Forbidden = new CeilingEntry { Ceiling = report.H2.Forbidden.Count, Items = report.H2.Forbidden.ToList() },
            H3 = new H3Entry
            {
                FilesCeiling = report.H3.Files,
                AttributesCeiling = report.H3.Attributes,
                Items = report.H3.Flagged.Select(f => f.File).ToList(),
            },
        };
        return JsonSerializer.Serialize(b, JsonOptions);
    }

    public static string Summary(Report report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Honesty audit").AppendLine();
        sb.AppendLine("| Check | Current | Ceiling | Delta | Status |").AppendLine("| --- | ---: | ---: | ---: | --- |");
        foreach (var c in report.Checks)
        {
            sb.AppendLine($"| {c.Name} | {c.Current} | {(c.Ceiling?.ToString() ?? "-")} | {(c.Delta?.ToString("+#;-#;0") ?? "-")} | {c.Status} |");
        }

        sb.AppendLine().AppendLine($"H1 all zeros by flag: {string.Join(", ", report.H1.ByFlag.Select(kv => $"{kv.Key} {kv.Value}"))}. H2 by project: {string.Join(", ", report.H2.ByProject.Select(kv => $"{kv.Key} {kv.Value}"))}.");
        sb.AppendLine($"H4: {report.H4.Distinct} distinct of {report.H4.Total}; most repeated: {(report.H4.MostRepeated.Count > 0 ? $"\"{report.H4.MostRepeated[0].Text}\" x{report.H4.MostRepeated[0].Count}" : "-")}.");

        foreach (var c in report.Checks.Where(c => c.Status == "IMPROVED"))
        {
            sb.AppendLine().AppendLine($"### IMPROVED: {c.Name}");
            sb.AppendLine("IMPROVED: lower the baseline in .github/honesty-audit-baseline.json (regenerate it with `--print-baseline`); the ceiling only shrinks.");
            foreach (var item in c.GoneItems.Take(SummaryCap))
            {
                sb.AppendLine($"- no longer present: {item}");
            }
        }

        foreach (var c in report.Checks.Where(c => c.Status == "RISE"))
        {
            sb.AppendLine().AppendLine($"### New items: {c.Name}");
            foreach (var item in c.NewItems.Take(SummaryCap))
            {
                sb.AppendLine($"- {item}");
            }
            if (c.NewItems.Count > SummaryCap)
            {
                sb.AppendLine($"- ... {c.NewItems.Count - SummaryCap} more in report.json");
            }
        }
        return sb.ToString();
    }
}

static class SelfTest
{
    static int failed;

    static void Check(string name, bool ok)
    {
        Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {name}");
        if (!ok) { failed++; }
    }

    static void Put(string root, string rel, string content)
    {
        var p = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, content);
    }

    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "honesty-audit-selftest-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            Put(root, "src/Encina.Foo/Foo.cs", "class Foo {}");
            Put(root, "src/Encina.Bar/Bar.cs", "class Bar {}");
            Put(root, ".github/coverage-manifest/defaults.json", "{\"rules\":[]}");
            Put(root, ".github/coverage-manifest/Encina.Foo.json",
                "{\"package\":\"Encina.Foo\",\"files\":{" +
                "\"WithCode.cs\":{\"targets\":{\"unit\":0,\"guard\":50},\"justifications\":{\"unit\":\"same\",\"guard\":\"g1\"}}," +
                "\"IFoo.cs\":{\"targets\":{\"unit\":0,\"contract\":0},\"justifications\":{\"unit\":\"same\",\"contract\":\"same\"}}}}");

            Put(root, "tests/Encina.ContractTests/Security/Sanitization/SanitizationContract.md", "# ContractTests\n\n## Status: Not Implemented\n");
            Put(root, "tests/Encina.UnitTests/Done.md", "# x\n\n## Status: Implemented\n");
            Put(root, "tests/Encina.BenchmarkTests/Stale.md", "# Benchmark\n\n## Status: Not Implemented\n\nSee Encina.Foo.\n");
            Put(root, "tests/Encina.BenchmarkTests/Live.md", "# Benchmark\n\n## Status: Not Implemented\n\nSee Encina.Bar.\n");
            Put(root, "tests/Encina.BenchmarkTests/FooBench.cs", "using Encina.Foo;\nclass FooBench {}");

            Put(root, "tests/Encina.ContractTests/Reflect.cs",
                "// a new idea\nclass R { [Fact] void A() { var m = \"new\"; typeof(object).GetMethod(m); } [Theory] void B() {} }");
            Put(root, "tests/Encina.ContractTests/Real.cs", "class T { [Fact] void A() { var x = new Foo(); } }");
            Put(root, "tests/Encina.ContractTests/NoTests.cs", "class N { void A() {} }");

            var report = Audit.Measure(root, TimeProvider.System.GetUtcNow());

            Check("H1 counts every zero target (informational total includes the interface file)", report.H1.AllZerosTotal == 3 && report.H1.ByFlag["unit"] == 2 && report.H1.ByFlag["contract"] == 1);
            Check("H2 counts Not Implemented files only", report.H2.Total == 3);
            Check("H2 Not Implemented file in ContractTests is forbidden", report.H2.Forbidden.Count == 1 && report.H2.Forbidden[0].EndsWith("SanitizationContract.md", StringComparison.Ordinal));
            Check("H2 benchmark file whose package already has benchmarks is a stale candidate (and the other is not)", report.H2.StaleCandidates.Count == 1 && report.H2.StaleCandidates[0].EndsWith("Stale.md", StringComparison.Ordinal));
            Check("H3 flags the reflection-only file with its two attributes (comment and string 'new' ignored)", report.H3.Files == 1 && report.H3.Attributes == 2 && report.H3.Flagged[0].File.EndsWith("Reflect.cs", StringComparison.Ordinal));
            Check("H3 does not flag a file with new", !report.H3.Flagged.Any(f => f.File.EndsWith("Real.cs", StringComparison.Ordinal)));
            Check("H4 counts total, distinct and the most repeated", report.H4.Total == 4 && report.H4.Distinct == 2 && report.H4.MostRepeated[0].Text == "same" && report.H4.MostRepeated[0].Count == 3);

            var current = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            var equal = Audit.Evaluate(report, current);
            Check("equal ceilings exit 0 and every delta is 0", equal.ExitCode == 0 && equal.Checks.Where(c => c.Gating).All(c => c.Delta == 0));

            var lowerForbidden = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            lowerForbidden.H2Forbidden.Ceiling = 0;
            lowerForbidden.H2Forbidden.Items = [];
            var lowered = Audit.Evaluate(report, lowerForbidden);
            Check("a lower H2 forbidden ceiling exits 1 and names the check with the new item", lowered.ExitCode == 1 && lowered.Checks.Any(c => c.Status == "RISE" && c.Name.StartsWith("H2 forbidden", StringComparison.Ordinal) && c.NewItems.Count == 1));

            var lowerH3 = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            lowerH3.H3.AttributesCeiling = 1;
            Check("a lower H3 attributes ceiling exits 1", Audit.Evaluate(report, lowerH3).ExitCode == 1);

            var higher = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            higher.H2Forbidden.Ceiling = 5;
            higher.H3.FilesCeiling = 4;
            var below = Audit.Evaluate(report, higher);
            Check("strict ratchet: a count below its ceiling exits 1 as IMPROVED (lower the baseline)", below.ExitCode == 1 && below.Checks.Count(c => c.Status == "IMPROVED") == 2);

            var movedOut = Audit.Measure(root, TimeProvider.System.GetUtcNow());
            movedOut.H2.Forbidden = [];
            var moved = Audit.Evaluate(movedOut, current);
            Check("a justification file moved out of a forbidden project is IMPROVED (exit 1) until the baseline is updated", moved.ExitCode == 1 && moved.Checks.Any(c => c.Status == "IMPROVED" && c.GoneItems.Count == 1));
            Check("the summary tells how to lower the baseline", Audit.Summary(new Report { Checks = moved.Checks }).Contains("IMPROVED: lower the baseline in .github/honesty-audit-baseline.json", StringComparison.Ordinal));

            var staleItems = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            staleItems.H3.Items = [.. staleItems.H3.Items, "tests/Encina.ContractTests/Gone.cs"];
            Check("an accepted item that no longer occurs is IMPROVED (exit 1)", Audit.Evaluate(report, staleItems).ExitCode == 1);

            foreach (var bad in new[]
            {
                "{\"schema\":1,\"h1\":{\"ceiling\":null,\"items\":[]},\"h2Forbidden\":{\"ceiling\":1,\"items\":null},\"h3\":{\"filesCeiling\":1,\"attributesCeiling\":2,\"items\":[]}}",
                "{\"schema\":1,\"h1\":{\"ceiling\":null,\"items\":[]},\"h2Forbidden\":null,\"h3\":{\"filesCeiling\":1,\"attributesCeiling\":2,\"items\":[]}}",
                "{\"schema\":1,\"h1\":{\"ceiling\":null,\"items\":[]},\"h2Forbidden\":{\"ceiling\":1,\"items\":[]},\"h3\":null}",
                "{\"schema\":1,\"h1\":null,\"h2Forbidden\":{\"ceiling\":1,\"items\":[]},\"h3\":{\"filesCeiling\":1,\"attributesCeiling\":2,\"items\":null}}",
            })
            {
                var rejected = false;
                try { Baseline.Load(WriteBaseline(root, bad)); }
                catch (InvalidDataException) { rejected = true; }
                Check("a baseline with a null section or null items is rejected (exit 2): " + bad[..Math.Min(bad.Length, 60)], rejected);
            }

            var emptyRoot = Path.Combine(root, "empty");
            Put(emptyRoot, ".github/coverage-manifest/Encina.Foo.json", "{\"files\":{}}");
            var missingDir = false;
            try { Audit.Measure(emptyRoot, TimeProvider.System.GetUtcNow()); }
            catch (DirectoryNotFoundException) { missingDir = true; }
            Check("a checkout without src/, tests/ or tests/Encina.ContractTests fails instead of reading as zero drift", missingDir);

            var badManifestRoot = Path.Combine(root, "badmanifest");
            Put(badManifestRoot, "src/Encina.Foo/Foo.cs", "class Foo {}");
            Put(badManifestRoot, "tests/Encina.ContractTests/A.cs", "class A {}");
            Put(badManifestRoot, ".github/coverage-manifest/Encina.Foo.json", "{\"files\":{\"X.cs\":null}}");
            var badManifest = false;
            try { Audit.Measure(badManifestRoot, TimeProvider.System.GetUtcNow()); }
            catch (InvalidOperationException) { badManifest = true; }
            Check("a manifest with a null file entry throws InvalidOperationException (caught by the caller as exit 2)", badManifest);

            var variantsRoot = Path.Combine(root, "variants");
            Put(variantsRoot, "src/Encina.Foo/Foo.cs", "class Foo {}");
            Put(variantsRoot, ".github/coverage-manifest/Encina.Foo.json", "{\"files\":{}}");
            Put(variantsRoot, "tests/Encina.ContractTests/A.cs", "class A {}");
            Put(variantsRoot, "tests/Encina.UnitTests/Lower.md", "## Status: not implemented\n");
            Put(variantsRoot, "tests/Encina.UnitTests/Plain.md", "Status: Not Implemented\n");
            Put(variantsRoot, "tests/Encina.GuardTests/Bold.md", "**Status**: Not  Implemented\n");
            Put(variantsRoot, "tests/Encina.GuardTests/Prose.md", "This is not implemented yet; Status: Not Implemented is only mentioned mid-line.\n");
            var variants = Audit.Measure(variantsRoot, TimeProvider.System.GetUtcNow());
            Check("H2 heading is case-insensitive and also accepts a plain or bold Status line (3 forbidden, prose not counted)", variants.H2.Forbidden.Count == 3 && variants.H2.Total == 3);

            var objRoot = Path.Combine(root, "obj", "tree");
            Put(objRoot, "src/Encina.Foo/Foo.cs", "class Foo {}");
            Put(objRoot, ".github/coverage-manifest/Encina.Foo.json", "{\"files\":{}}");
            Put(objRoot, "tests/Encina.ContractTests/A.cs", "class A {}");
            Put(objRoot, "tests/Encina.UnitTests/Plain.md", "Status: Not Implemented\n");
            Check("a checkout below a directory named obj is still scanned", Audit.Measure(objRoot, TimeProvider.System.GetUtcNow()).H2.Total == 1);

            Check("stripper: a quoted new inside an interpolation hole is ignored", !HasNew("var s = $\"{string.Join(\"new\", x)}\";"));
            Check("stripper: new as code inside an interpolation hole is kept", HasNew("var s = $\"{new Foo()}\";"));
            Check("stripper: nested quotes in a hole and a format specifier", !HasNew("var s = $\"{(a ? \"x\" : \"y\")} new {v:N2} new\";"));
            Check("stripper: interpolated verbatim string", !HasNew("var s = $@\"say \"\"new\"\" {x} new\";") && HasNew("var s = $@\"{new Foo()} x\";"));
            Check("stripper: raw interpolated string", !HasNew("var s = $\"\"\"a \"new\" {x} new\"\"\";") && HasNew("var s = $\"\"\"a {new Foo()} x\"\"\";"));
            Check("stripper: double-dollar raw interpolated string", !HasNew("var s = $$\"\"\"a {new} {{x}} new\"\"\";") && HasNew("var s = $$\"\"\"a {{new Foo()}} x\"\"\";"));
            Check("stripper: escaped braces, char literals and verbatim identifiers", !HasNew("var s = $\"{{new}} {x}\"; var c = '\"'; var d = '\\''; var @new = 1;"));

            var missing = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            missing.H3.FilesCeiling = null;
            Check("a baseline without a gating ceiling fails closed with exit 2", Audit.Evaluate(report, missing).ExitCode == 2);

            var swapped = Baseline.Load(WriteBaseline(root, Audit.BaselineText(report)));
            swapped.H2Forbidden.Items = ["tests/Encina.ContractTests/Other.md"];
            var swap = Audit.Evaluate(report, swapped);
            Check("a swapped item with an equal count is a rise (exit 1)", swap.ExitCode == 1 && swap.Checks.Any(c => c.Status == "RISE" && c.NewItems.Count == 1));

            var misspelled = false;
            try { Baseline.Load(WriteBaseline(root, "{\"schema\":1,\"H2forbidden\":{\"ceiling\":9}}")); }
            catch (JsonException) { misspelled = true; }
            Check("a misspelled baseline key is rejected", misspelled);

            Check("H1 never gates even with no ceiling", equal.Checks[0].Gating == false && equal.Checks[0].Status == "reported");
            Check("without a baseline the run exits 0", Audit.Evaluate(report, null).ExitCode == 0);
            Check("comment and string stripping keeps code", Audit.StripCommentsAndStrings("a // new\nb \"new\" c @\"x\"\"new\" d").Contains('b', StringComparison.Ordinal) && !Audit.StripCommentsAndStrings("a // new\nb \"new\"").Contains("new", StringComparison.Ordinal));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }

        Console.WriteLine(failed == 0 ? "Self-test passed." : $"Self-test failed: {failed} check(s).");
        return failed == 0 ? 0 : 1;
    }

    static bool HasNew(string code) => Regex.IsMatch(Audit.StripCommentsAndStrings(code), @"\bnew\b");

    static string WriteBaseline(string root, string text)
    {
        var p = Path.Combine(root, "baseline-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(p, text);
        return p;
    }
}
