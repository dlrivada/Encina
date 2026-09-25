// crap-gate.cs — CRAP (Change Risk Anti-Patterns) gate for a PR diff.
#pragma warning disable CA1305, CA1310, CA1852 // Globalization/sealed warnings not relevant for standalone scripts
//
// Reports, and optionally fails on, the methods a diff touches whose CRAP score is above a
// threshold (default 10). Adopted from unclebob/swarm-forge's engineering constitution: CRAP <= 10
// on changed code is a gate (see issue #1346).
//
// CRAP(m) = comp(m)^2 * (1 - cov(m))^3 + comp(m)
//   comp = the Cobertura <method complexity=""> attribute.
//   cov  = the method's line coverage in [0,1], combined across every Cobertura file given
//          (the union of covered lines across all flags — see coverage-measurement-methodology.md).
//
// Usage:
//   dotnet run crap-gate.cs -- [--cobertura <path>]... <path.xml...> [--diff <path> | < diff on stdin]
//                              [--threshold 10] [--report | --enforce]
//
// The diff is the output of `git diff -U0 <base>...HEAD` (unified diff, repo-root-relative paths).
// A method is exempt from the gate when the source line immediately above its declaration reads:
//   // crap-exempt: single-question switch — <reason>
// (the swarm-forge exception: one switch or pattern match that answers one question). Exemptions
// are always listed in the output, never silent.
//
// --report always exits 0. --enforce exits 1 when a non-exempt violation exists.
//
// Requires: .NET 10+ (C# 14 file-based app)

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// ─── Args ──────────────────────────────────────────────────────────────────

var coberturaFiles = new List<string>();
string? diffPath = null;
double threshold = 10.0;
bool enforce = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--diff":
            diffPath = args[++i];
            break;
        case "--threshold":
            threshold = double.Parse(args[++i], CultureInfo.InvariantCulture);
            break;
        case "--enforce":
            enforce = true;
            break;
        case "--report":
            enforce = false;
            break;
        case "--cobertura":
            coberturaFiles.Add(args[++i]);
            break;
        default:
            if (args[i].EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                coberturaFiles.Add(args[i]);
            break;
    }
}

if (coberturaFiles.Count == 0)
{
    Console.Error.WriteLine("ERROR: no Cobertura file given. Pass one or more paths (--cobertura <path> or a positional *.xml).");
    return 2;
}

foreach (var f in coberturaFiles)
{
    if (!File.Exists(f))
    {
        Console.Error.WriteLine($"ERROR: Cobertura file not found: {f}");
        return 2;
    }
}

string diffText = diffPath is not null ? File.ReadAllText(diffPath) : Console.In.ReadToEnd();

// ─── Parse Cobertura: one MethodInfo per (file, class, method+signature), merged across inputs ──

var methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);

foreach (var path in coberturaFiles)
{
    XDocument doc;
    try
    {
        doc = XDocument.Load(path);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"ERROR parsing {path}: {ex.Message}");
        return 2;
    }

    var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
    var sourceEl = doc.Descendants(ns + "source").FirstOrDefault()?.Value?.Replace('\\', '/') ?? "";
    var sourceEndsSrc = sourceEl.EndsWith("/src/", StringComparison.Ordinal) || sourceEl.EndsWith("/src", StringComparison.Ordinal);

    foreach (var cls in doc.Descendants(ns + "class"))
    {
        var rawFilename = cls.Attribute("filename")?.Value;
        if (rawFilename is null) continue;

        var relFile = NormalizeCoberturaFilename(rawFilename, sourceEndsSrc);
        // Build-generated sources are not something a human PR "touches" in a meaningful sense.
        if (relFile.Contains("/obj/", StringComparison.OrdinalIgnoreCase)) continue;

        var className = cls.Attribute("name")?.Value ?? "";
        var methodsEl = cls.Element(ns + "methods");
        if (methodsEl is null) continue;

        foreach (var m in methodsEl.Elements(ns + "method"))
        {
            var name = m.Attribute("name")?.Value ?? "";
            var signature = m.Attribute("signature")?.Value ?? "";
            var complexity = int.Parse(m.Attribute("complexity")?.Value ?? "1", CultureInfo.InvariantCulture);
            var key = $"{relFile}|{className}|{name}{signature}";

            if (!methods.TryGetValue(key, out var info))
            {
                info = new MethodInfo(key, relFile, className, name, signature, complexity);
                methods[key] = info;
            }

            foreach (var line in m.Descendants(ns + "line"))
            {
                var num = int.Parse(line.Attribute("number")?.Value ?? "0", CultureInfo.InvariantCulture);
                var hits = int.Parse(line.Attribute("hits")?.Value ?? "0", CultureInfo.InvariantCulture);
                if (num <= 0) continue;

                if (info.Lines.TryGetValue(num, out var existing))
                    info.Lines[num] = Math.Max(existing, hits);
                else
                    info.Lines[num] = hits;
            }
        }
    }
}

var methodsByFile = methods.Values
    .Where(m => m.Lines.Count > 0)
    .GroupBy(m => m.File)
    .ToDictionary(g => g.Key, g => g.ToList());

// ─── Parse the diff: file → set of changed/added line numbers (in the NEW file) ─────────────

var changedLinesByFile = ParseDiff(diffText);

// ─── Find methods the diff touches ──────────────────────────────────────────

var touched = new List<MethodInfo>();
foreach (var (file, changedLines) in changedLinesByFile)
{
    if (!methodsByFile.TryGetValue(file, out var fileMethods)) continue;

    foreach (var m in fileMethods)
    {
        if (changedLines.Any(l => l >= m.MinLine && l <= m.MaxLine))
            touched.Add(m);
    }
}

touched = touched.Distinct().OrderByDescending(m => m.Crap).ToList();

// ─── Report ──────────────────────────────────────────────────────────────────

Console.WriteLine($"CRAP gate — threshold {threshold.ToString(CultureInfo.InvariantCulture)}, mode {(enforce ? "enforce" : "report")}");
Console.WriteLine($"Cobertura file(s): {string.Join(", ", coberturaFiles)}");
Console.WriteLine($"Changed files with coverage data: {changedLinesByFile.Keys.Count(f => methodsByFile.ContainsKey(f))}");
Console.WriteLine($"Changed methods analyzed: {touched.Count}");
Console.WriteLine();

var above = touched.Where(m => m.Crap > threshold).ToList();
var exempted = new List<(MethodInfo Method, string Reason)>();
var blocking = new List<MethodInfo>();

foreach (var m in above)
{
    if (TryGetExemption(m, out var reason))
        exempted.Add((m, reason!));
    else
        blocking.Add(m);
}

if (above.Count == 0)
{
    Console.WriteLine("No changed method exceeds the CRAP threshold.");
}
else
{
    Console.WriteLine("Methods above threshold:");
    foreach (var m in blocking)
        Console.WriteLine($"  [VIOLATION] {m.File}:{m.ClassName}.{m.Name} complexity={m.Complexity} coverage={m.Coverage:P0} CRAP={m.Crap:F2}");
    foreach (var (m, reason) in exempted)
        Console.WriteLine($"  [EXEMPT]    {m.File}:{m.ClassName}.{m.Name} complexity={m.Complexity} coverage={m.Coverage:P0} CRAP={m.Crap:F2} — {reason}");
}

var touchedKeys = touched.Select(m => m.Key).ToHashSet(StringComparer.Ordinal);
var backlog = methods.Values.Count(m => m.Crap > threshold && !touchedKeys.Contains(m.Key));
Console.WriteLine();
Console.WriteLine($"Backlog: {backlog} method(s) with CRAP > {threshold.ToString(CultureInfo.InvariantCulture)} outside this diff (not gated).");

if (enforce && blocking.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"FAIL: {blocking.Count} method(s) exceed the CRAP threshold and are not exempt.");
    return 1;
}

return 0;

// ─── Helper functions ────────────────────────────────────────────────────────

// Mirrors coverage-report.cs's filename normalization so a Cobertura <class filename=""> lines up
// with a git-diff path (both end up "src/Package/Path.cs").
static string NormalizeCoberturaFilename(string filename, bool sourceEndsSrc)
{
    filename = filename.Replace('\\', '/');
    if (sourceEndsSrc && !filename.Contains("/src/", StringComparison.Ordinal) && !filename.StartsWith("src/", StringComparison.Ordinal))
        filename = "src/" + filename;

    var srcIdx = filename.IndexOf("/src/", StringComparison.Ordinal);
    if (srcIdx < 0) return filename;
    return filename[(srcIdx + 1)..];
}

static string NormalizeDiffPath(string path)
{
    path = path.Replace('\\', '/');
    if (path.StartsWith("a/", StringComparison.Ordinal) || path.StartsWith("b/", StringComparison.Ordinal))
        path = path[2..];

    var srcIdx = path.IndexOf("/src/", StringComparison.Ordinal);
    return srcIdx >= 0 ? path[(srcIdx + 1)..] : path;
}

// Parses a `git diff -U0` unified diff into file → set of line numbers added/changed in the new file.
static Dictionary<string, HashSet<int>> ParseDiff(string diffText)
{
    var result = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
    string? currentFile = null;
    int newLine = 0;
    var hunkRegex = new Regex(@"^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@", RegexOptions.CultureInvariant);

    foreach (var raw in diffText.Split('\n'))
    {
        var line = raw.TrimEnd('\r');

        if (line.StartsWith("diff --git ", StringComparison.Ordinal))
        {
            currentFile = null;
            continue;
        }

        if (line.StartsWith("+++ ", StringComparison.Ordinal))
        {
            var path = line[4..].Trim();
            currentFile = path == "/dev/null" ? null : NormalizeDiffPath(path);
            continue;
        }

        if (line.StartsWith("--- ", StringComparison.Ordinal)) continue;

        var hunkMatch = hunkRegex.Match(line);
        if (hunkMatch.Success)
        {
            newLine = int.Parse(hunkMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            continue;
        }

        if (currentFile is null || line.Length == 0) continue;

        switch (line[0])
        {
            case '+':
                if (!result.TryGetValue(currentFile, out var set))
                    result[currentFile] = set = new HashSet<int>();
                set.Add(newLine);
                newLine++;
                break;
            case '-':
                // Removed line: does not exist in the new file, does not consume a new line number.
                break;
            case ' ':
                newLine++;
                break;
            default:
                // "\ No newline at end of file" and similar diff metadata: ignore.
                break;
        }
    }

    return result;
}

// The swarm-forge exception: a method whose declaration is preceded by
//   // crap-exempt: single-question switch — <reason>
// is exempt. Since Cobertura only reports executable lines (not the signature line), this scans
// upward from the method's first coverable line for a line containing the method's simple name
// followed by '(' or '<' (a declaration), then checks the line directly above it. Compiler-generated
// names (state machines, lambdas, property accessors) cannot be reliably matched back to a source
// declaration this way and are therefore never exempt.
static bool TryGetExemption(MethodInfo m, out string? reason)
{
    reason = null;

    var simpleName = m.Name;
    if (simpleName.StartsWith("get_", StringComparison.Ordinal) || simpleName.StartsWith("set_", StringComparison.Ordinal))
        simpleName = simpleName[4..];
    if (simpleName.Length == 0 || simpleName[0] == '<') return false;

    if (!File.Exists(m.File)) return false;
    string[] lines;
    try
    {
        lines = File.ReadAllLines(m.File);
    }
    catch (IOException)
    {
        return false;
    }

    var startIdx = m.MinLine - 1; // 0-based
    if (startIdx < 0 || startIdx >= lines.Length) return false;

    var declRegex = new Regex($@"\b{Regex.Escape(simpleName)}\b\s*[<(]", RegexOptions.CultureInvariant);
    var searchFloor = Math.Max(0, startIdx - 25);
    var declIdx = -1;
    for (var i = startIdx; i >= searchFloor; i--)
    {
        if (declRegex.IsMatch(lines[i]))
        {
            declIdx = i;
            break;
        }
    }

    if (declIdx <= 0) return false;

    var commentLine = lines[declIdx - 1].Trim();
    var match = Regex.Match(commentLine, @"^//\s*crap-exempt:\s*single-question switch\s*—\s*(.+)$", RegexOptions.CultureInvariant);
    if (!match.Success) return false;

    reason = match.Groups[1].Value.Trim();
    return true;
}

// ─── Types (must be after top-level statements in C# 14) ─────────────────────

sealed class MethodInfo(string key, string file, string className, string name, string signature, int complexity)
{
    public string Key { get; } = key;
    public string File { get; } = file;
    public string ClassName { get; } = className;
    public string Name { get; } = name;
    public string Signature { get; } = signature;
    public int Complexity { get; } = complexity;
    public Dictionary<int, int> Lines { get; } = new();

    public int MinLine => Lines.Count > 0 ? Lines.Keys.Min() : 0;
    public int MaxLine => Lines.Count > 0 ? Lines.Keys.Max() : 0;
    public double Coverage => Lines.Count > 0 ? Lines.Values.Count(h => h > 0) / (double)Lines.Count : 1.0;
    public double Crap => (Complexity * (double)Complexity * Math.Pow(1 - Coverage, 3)) + Complexity;
}
