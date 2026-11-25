// check-target-ratchet.cs — A pull request cannot lower or drop a coverage target without the
// maintainer label (#2895, EPIC #2845 Phase 4).
//
// Compares .github/coverage-manifest/*.json (package "targets" and per-file "files[*].targets")
// between a base ref and a head ref and applies two rules, per (package, flag) and per
// (package, file, flag):
//   R-LOW   the head value is lower than the base value.
//   R-DROP  the base has a target and the head has none while the source still exists in the head
//           tree: src/<Package>/<file> for a file target, a src/**/<Package>.csproj for a package
//           target. A deleted or renamed source file, or a retired package, may drop its entry.
//   R-ZERO  (#2898) a unit, guard or contract file target that is 0 in the head and absent in the
//           base (a base value above 0 lowered to 0 is R-LOW). Accepted automatically when the
//           closed-list classifier of #2890 says the source file has no executable code for that
//           flag, by asking `coverage-report.cs --classify-zero` (no second copy of the test); the
//           source is read from the head revision. If the source or the verdict is missing, the
//           zero is a violation (fail closed). integration and property zeros are out of the rule.
// A value equal to its base value is not a change, so existing zeros are never touched.
//
// The base used for every read is the merge base of --base and --head, so commits that landed on
// the base branch after the pull request branched are not mistaken for lowerings. A path absent
// from the base (`git show` fails) is a new manifest; absent from the head, a deleted one. Renames
// are followed (git diff -M): the old path is the base side, the new path the head side.
//
// Label exception: with violations, the script asks GitHub at run time (`gh pr view <pr> --json
// labels`) whether the pull request carries `target-change-approved`; if so the violations are
// printed as approved and the exit code is 0. If the lookup fails, or --pr is missing, the script
// fails closed (exit 1). This is a tripwire against silent change, not a protection against an
// author who labels their own pull request; the violations are also written to the job summary.
//
// Usage:
//   dotnet run --file .github/scripts/check-target-ratchet.cs -- --base <ref> [--head <ref>] [--pr <n>]
//       [--no-label-lookup | --assume-label] [--root <repo root>]
//   dotnet run --file .github/scripts/check-target-ratchet.cs -- --self-test
//
// --head defaults to HEAD. --no-label-lookup treats the label as absent and --assume-label as
// present (both are test flags and need no --pr). --root defaults to the nearest ancestor of the
// current directory that holds Encina.slnx.
//
// Exit codes: 0 no violation, or every violation approved by the label; 1 a violation without the
// label, or any failure (git, parse, gh: fail closed); 2 invalid arguments.
// Requires: .NET 10+ (C# 14 file-based app) and git; gh only when there are violations and a --pr;
// `dotnet run` of coverage-report.cs once per new zero of a file that is not yet classified.
#pragma warning disable CA1305, CA1310, CA1859, CA1852, CA1307, CA1861

using System.Diagnostics;
using System.Text;
using System.Text.Json;

const string LabelName = "target-change-approved";
const string ManifestDir = ".github/coverage-manifest/";

var parsed = ParseArgs(args, out var argError);
if (parsed is null)
{
    Console.Error.WriteLine(argError);
    return 2;
}

var (baseRef, headRef, rootArg, pr, selfTest, noLabel, assumeLabel) = parsed;
if (selfTest) return SelfTest();

if (baseRef is null || (noLabel && assumeLabel))
{
    Console.Error.WriteLine("Usage: --base <ref> [--head <ref>] [--pr <n>] [--no-label-lookup | --assume-label] | --self-test");
    return 2;
}

var root = rootArg ?? FindRoot(Directory.GetCurrentDirectory());
var errors = new List<string>();

var mergeBase = Git(root, "merge-base", baseRef, headRef);
if (mergeBase.Exit != 0 || string.IsNullOrWhiteSpace(mergeBase.Out))
{
    Console.Error.WriteLine($"ERROR: git merge-base {baseRef} {headRef} failed: {mergeBase.Err.Trim()}");
    return 1;
}

var baseSha = mergeBase.Out.Trim();
var diff = Git(root, "diff", "--name-status", "-z", "-M", baseSha, headRef, "--", ManifestDir);
if (diff.Exit != 0)
{
    Console.Error.WriteLine($"ERROR: git diff failed: {diff.Err.Trim()}");
    return 1;
}

var tree = Git(root, "ls-tree", "-r", "--name-only", "-z", headRef, "--", "src");
if (tree.Exit != 0)
{
    Console.Error.WriteLine($"ERROR: git ls-tree failed: {tree.Err.Trim()}");
    return 1;
}

var changes = ParseDiff(diff.Out);
var headTree = new HashSet<string>(tree.Out.Split('\0', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
string? ReadAt(string rev, string path)
{
    var r = Git(root, "show", $"{rev}:{path}");
    return r.Exit == 0 ? r.Out : null; // a failing `git show` means "absent at that revision"
}

using var classifier = new ZeroSource(headRef, headTree, ReadAt, (file, flag) =>
{
    var r = Run("dotnet", root, "run", "--file", Path.Combine(root, ".github", "scripts", "coverage-report.cs"),
        "--", "--classify-zero", file, flag);
    return (r.Exit, r.Out);
});
var violations = Evaluate(changes, p => ReadAt(baseSha, p), p => ReadAt(headRef, p), headTree, errors, classifier.Classify);
if (errors.Count > 0)
{
    foreach (var e in errors) Console.Error.WriteLine($"ERROR: {e}");
    return 1;
}

LabelResult Lookup()
{
    if (noLabel) return LabelResult.Absent;
    if (assumeLabel) return LabelResult.Present;
    return pr is null ? LabelResult.Failed : LookupLabel(root, pr.Value);
}

var outcome = Decide(changes.Count, violations, Lookup, LabelName);
foreach (var line in outcome.Lines) Console.WriteLine(line);
var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summary) && violations.Count > 0)
{
    File.AppendAllText(summary, "### Target ratchet\n\n```text\n" + string.Join("\n", outcome.Lines) + "\n```\n");
}

return outcome.Exit;

// ---------------------------------------------------------------------------------------------

static Options? ParseArgs(string[] a, out string error)
{
    string? baseRef = null, headRef = "HEAD", rootArg = null;
    int? pr = null;
    bool selfTest = false, noLabel = false, assumeLabel = false;
    error = "";
    for (var i = 0; i < a.Length; i++)
    {
        switch (a[i])
        {
            case "--self-test": selfTest = true; break;
            case "--no-label-lookup": noLabel = true; break;
            case "--assume-label": assumeLabel = true; break;
            case "--base" when i + 1 < a.Length: baseRef = a[++i]; break;
            case "--head" when i + 1 < a.Length: headRef = a[++i]; break;
            case "--root" when i + 1 < a.Length: rootArg = a[++i]; break;
            case "--pr" when i + 1 < a.Length && int.TryParse(a[i + 1], out var n): pr = n; i++; break;
            default:
                error = $"Unknown or incomplete argument: {Safe(a[i])}";
                return null;
        }
    }

    return new Options(baseRef, headRef, rootArg, pr, selfTest, noLabel, assumeLabel);
}

// Control and non-ASCII characters are replaced and the length capped: a manifest key, a path or an
// argument is untrusted text that ends up on a CI console line and in the job summary.
static string Safe(string value) => Text.Safe(value);

static string FindRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Encina.slnx"))) return dir.FullName;
    }

    return start;
}

static (int Exit, string Out, string Err) Run(string file, string workDir, params string[] argv)
{
    var psi = new ProcessStartInfo(file)
    {
        WorkingDirectory = workDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
    };
    foreach (var a in argv) psi.ArgumentList.Add(a);
    try
    {
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output, err.Result);
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
    {
        return (127, "", $"{file}: {ex.GetType().Name}");
    }
}

static (int Exit, string Out, string Err) Git(string root, params string[] argv) => Run("git", root, argv);

static LabelResult LookupLabel(string root, int pr)
{
    var r = Run("gh", root, "pr", "view", pr.ToString(), "--json", "labels");
    if (r.Exit != 0) return LabelResult.Failed;
    try
    {
        using var doc = JsonDocument.Parse(r.Out);
        foreach (var l in doc.RootElement.GetProperty("labels").EnumerateArray())
        {
            if (l.TryGetProperty("name", out var n) && n.GetString() == "target-change-approved") return LabelResult.Present;
        }

        return LabelResult.Absent;
    }
    catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
    {
        return LabelResult.Failed;
    }
}

static bool IsManifest(string? path) =>
    path is not null && path.StartsWith(ManifestDir, StringComparison.Ordinal)
    && path.EndsWith(".json", StringComparison.Ordinal)
    && path.IndexOf('/', ManifestDir.Length) < 0 // top level only: coverage-report.cs loads GetFiles(dir, "*.json")
    && path != ManifestDir + "defaults.json";

// git diff --name-status -z: "<status>\0<path>\0" or, for R/C, "<status>\0<old>\0<new>\0".
static List<Change> ParseDiff(string raw)
{
    var t = raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    var list = new List<Change>();
    for (var i = 0; i < t.Length;)
    {
        var s = t[i++];
        if (s[0] is 'R' or 'C')
        {
            if (i + 1 >= t.Length) break;
            list.Add(s[0] == 'R' ? new Change(t[i], t[i + 1]) : new Change(null, t[i + 1]));
            i += 2;
            continue;
        }

        if (i >= t.Length) break;
        var path = t[i++];
        list.Add(s[0] switch { 'A' => new Change(null, path), 'D' => new Change(path, null), _ => new Change(path, path) });
    }

    return list.Where(c => IsManifest(c.BasePath) || IsManifest(c.HeadPath)).ToList();
}

static List<Violation> Evaluate(
    List<Change> changes, Func<string, string?> readBase, Func<string, string?> readHead,
    HashSet<string> headTree, List<string> errors, Func<string, string, string, ZeroVerdict> classify)
{
    var csprojNames = new HashSet<string>(StringComparer.Ordinal);
    foreach (var p in headTree)
    {
        if (p.EndsWith(".csproj", StringComparison.Ordinal)) csprojNames.Add(Path.GetFileNameWithoutExtension(p));
    }

    var result = new List<Violation>();
    foreach (var c in changes)
    {
        // A new manifest (or one moved in) has nothing to ratchet against for R-LOW and R-DROP, but
        // every one of its zeros is new for R-ZERO.
        Manifest? b = null;
        if (IsManifest(c.BasePath))
        {
            var baseJson = readBase(c.BasePath!);
            if (baseJson is null) { errors.Add($"base manifest {c.BasePath} could not be read"); continue; }

            b = Manifest.Parse(baseJson, c.BasePath!);
            if (b is null) { errors.Add($"base manifest {c.BasePath} is not a readable manifest"); continue; }
        }

        Manifest? h = null;
        // A head path the coverage report does not load (renamed to .json.disabled, moved into a
        // subfolder) is a deleted manifest.
        var headJson = IsManifest(c.HeadPath) ? readHead(c.HeadPath!) : null;
        if (headJson is not null)
        {
            h = Manifest.Parse(headJson, c.HeadPath!);
            if (h is null) { errors.Add($"head manifest {c.HeadPath} is not a readable manifest"); continue; }
        }

        if (b is not null) Compare(b, h, csprojNames, headTree, result);
        if (h is not null) CompareZeros(b, h, classify, result);
    }

    return result;
}

// R-ZERO (#2898): a unit, guard or contract file target that is 0 in the head and absent in the base.
// A base value above 0 lowered to 0 is already an R-LOW violation with the same label gate, so it is
// not reported twice. A file the closed-list classifier of #2890 says has no executable code for the
// flag is accepted; one it cannot answer for (source missing or unreadable, no verdict) fails closed.
static void CompareZeros(Manifest? b, Manifest h, Func<string, string, string, ZeroVerdict> classify, List<Violation> result)
{
    foreach (var (file, flags) in h.Files)
    {
        var baseFlags = new HashSet<string>(StringComparer.Ordinal);
        if (b is not null && b.Files.TryGetValue(file, out var bf)) baseFlags.UnionWith(bf.Keys.Select(k => k.ToLowerInvariant()));
        foreach (var (flag, hv) in flags)
        {
            var lowered = flag.ToLowerInvariant();
            if (hv != 0 || lowered is not ("unit" or "guard" or "contract") || baseFlags.Contains(lowered)) continue;
            var verdict = classify(h.Package, file, lowered);
            if (verdict is { Answered: true, Exempt: true }) continue;
            result.Add(new Violation(h.Package, Safe(file), lowered, null, 0,
                verdict.Answered ? "file has executable code" : "source not readable or not classified; failing closed"));
        }
    }
}

static void Compare(Manifest b, Manifest? h, HashSet<string> csprojNames, HashSet<string> headTree, List<Violation> result)
{
    var name = h?.Package ?? b.Package;
    // Existence is judged under both the base and the head package name, so renaming the
    // "package" field cannot make the source look gone.
    var packageLives = csprojNames.Contains(b.Package) || csprojNames.Contains(name);

    foreach (var (flag, bv) in b.Targets)
    {
        if (h is not null && h.Targets.TryGetValue(flag, out var hv))
        {
            if (hv < bv) result.Add(new Violation(name, "package", flag, bv, hv));
        }
        else if (packageLives)
        {
            result.Add(new Violation(name, "package", flag, bv, null));
        }
    }

    foreach (var (file, flags) in b.Files)
    {
        Dictionary<string, double>? headFlags = null;
        h?.Files.TryGetValue(file, out headFlags);
        var sourceLives = headTree.Contains($"src/{name}/{file}") || headTree.Contains($"src/{b.Package}/{file}");
        foreach (var (flag, bv) in flags)
        {
            if (headFlags is not null && headFlags.TryGetValue(flag, out var hv))
            {
                if (hv < bv) result.Add(new Violation(name, file, flag, bv, hv));
            }
            else if (h is null ? packageLives : sourceLives)
            {
                result.Add(new Violation(name, file, flag, bv, null));
            }
        }
    }
}

static Outcome Decide(int manifestsChanged, List<Violation> violations, Func<LabelResult> lookup, string labelName)
{
    if (violations.Count == 0)
    {
        return new Outcome(0, [$"Target ratchet: {manifestsChanged} manifests changed, 0 violations"]);
    }

    var label = lookup();
    var lines = new List<string>();
    var tag = label == LabelResult.Present ? $"APPROVED by {labelName}  " : "";
    foreach (var v in violations) lines.Add(tag + v.Format());
    lines.Add($"Target ratchet: {manifestsChanged} manifests changed, {violations.Count} violations");
    switch (label)
    {
        case LabelResult.Present:
            return new Outcome(0, lines);
        case LabelResult.Absent:
            lines.Add($"FAIL: add the label '{labelName}' (maintainer) and re-run this job to accept the change.");
            return new Outcome(1, lines);
        default:
            lines.Add("FAIL: could not read the pull request labels (gh failed or --pr missing); failing closed.");
            return new Outcome(1, lines);
    }
}

// ---------------------------------------------------------------------------------------------
// Self-test: in-memory manifests, no git and no gh.

static int SelfTest()
{
    var failures = 0;
    void Check(string name, bool ok)
    {
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}");
        if (!ok) failures++;
    }

    string P(string n) => $".github/coverage-manifest/{n}.json";
    string J(string pkg, string pt, params (string File, string T)[] files)
    {
        var sb = new StringBuilder($"{{\"package\":\"{pkg}\",\"targets\":{{{Obj(pt)}}},\"files\":{{");
        sb.Append(string.Join(",", files.Select(f => $"\"{f.File}\":{{\"targets\":{{{Obj(f.T)}}}}}")));
        return sb.Append("}}").ToString();
    }

    static string Obj(string spec) =>
        string.Join(",", spec.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(kv => { var p = kv.Split('='); return $"\"{p[0]}\":{p[1]}"; }));

    HashSet<string> Tree(params string[] paths) => new(paths);
    var live = Tree("src/Pkg/Pkg.csproj", "src/Pkg/A.cs", "src/Pkg/B.cs");

    var exempt = new ZeroVerdict(true, true);
    var code = new ZeroVerdict(true, false);
    var unanswered = new ZeroVerdict(false, false);

    List<Violation> Eval(Change c, string? baseJson, string? headJson, HashSet<string> tree, ZeroVerdict? verdict = null)
    {
        var errs = new List<string>();
        var r = Evaluate([c], _ => baseJson, _ => headJson, tree, errs, (_, _, _) => verdict ?? exempt);
        if (errs.Count > 0) r.Add(new Violation("ERR", errs[0], "", 0, null));
        return r;
    }

    var same = new Change(P("Pkg"), P("Pkg"));
    var b0 = J("Pkg", "unit=50,guard=40", ("A.cs", "unit=90,guard=0"));

    Check("lowered package target is R-LOW",
        Eval(same, b0, J("Pkg", "unit=41,guard=40", ("A.cs", "unit=90,guard=0")), live) is [{ Scope: "package", Flag: "unit", Base: 50, Head: 41 }]);
    Check("lowered file target is R-LOW",
        Eval(same, b0, J("Pkg", "unit=50,guard=40", ("A.cs", "unit=80,guard=0")), live) is [{ Scope: "A.cs", Flag: "unit" }]);
    Check("package flag dropped while the project exists is R-DROP",
        Eval(same, b0, J("Pkg", "unit=50", ("A.cs", "unit=90,guard=0")), live) is [{ Scope: "package", Flag: "guard", Head: null }]);
    Check("file flag dropped while the source exists is R-DROP",
        Eval(same, b0, J("Pkg", "unit=50,guard=40", ("A.cs", "unit=90")), live) is [{ Scope: "A.cs", Flag: "guard", Head: null }]);
    Check("file entry dropped while the source exists is R-DROP",
        Eval(same, b0, J("Pkg", "unit=50,guard=40"), live) is [{ Scope: "A.cs", Flag: "unit" }, { Scope: "A.cs", Flag: "guard" }]);
    Check("file entry dropped after the source was deleted is allowed",
        Eval(same, b0, J("Pkg", "unit=50,guard=40"), Tree("src/Pkg/Pkg.csproj")).Count == 0);
    Check("raised targets are allowed",
        Eval(same, b0, J("Pkg", "unit=60,guard=40", ("A.cs", "unit=95,guard=10")), live).Count == 0);
    Check("equal zeros are untouched",
        Eval(same, J("Pkg", "unit=0", ("A.cs", "guard=0")), J("Pkg", "unit=0", ("A.cs", "guard=0")), live).Count == 0);
    Check("a new file entry with positive targets is allowed",
        Eval(same, b0, J("Pkg", "unit=50,guard=40", ("A.cs", "unit=90,guard=0"), ("B.cs", "unit=70")), live).Count == 0);
    Check("a new manifest (absent in base) is allowed",
        Eval(new Change(null, P("Pkg")), null, b0, live).Count == 0);

    var del = new Change(P("Pkg"), null);
    var deleted = Eval(del, b0, null, live);
    Check("deleted manifest with a live project: R-DROP for every target",
        deleted.Count == 4 && deleted.All(v => v.Head is null));
    Check("deleted manifest whose project is gone is allowed",
        Eval(del, b0, null, Tree("src/Other/Other.csproj")).Count == 0);

    var ren = new Change(P("Old"), P("Pkg"));
    var oldJson = J("Old", "unit=50", ("A.cs", "unit=90"));
    Check("renamed manifest with the same targets is allowed",
        Eval(ren, oldJson, J("Pkg", "unit=50", ("A.cs", "unit=90")), live).Count == 0);
    Check("renamed manifest with higher targets is allowed",
        Eval(ren, oldJson, J("Pkg", "unit=55", ("A.cs", "unit=95")), live).Count == 0);
    Check("renamed manifest with lower targets is R-LOW",
        Eval(ren, oldJson, J("Pkg", "unit=40", ("A.cs", "unit=90")), live) is [{ Package: "Pkg", Scope: "package", Flag: "unit", Base: 50, Head: 40 }]);

    var disabled = Eval(new Change(P("Pkg"), P("Pkg") + ".disabled"), b0, b0, live);
    Check("renaming a manifest to .json.disabled is a deleted manifest (R-DROP)",
        disabled.Count == 4 && disabled.All(v => v.Head is null));
    Check("moving a manifest into a subfolder is a deleted manifest (R-DROP)",
        Eval(new Change(P("Pkg"), ".github/coverage-manifest/sub/Pkg.json"), b0, b0, live).Count == 4);
    Check("renaming away a manifest whose project is gone is allowed",
        Eval(new Change(P("Pkg"), P("Pkg") + ".disabled"), b0, b0, Tree("src/Other/Other.csproj")).Count == 0);
    Check("a file moved into the manifest folder from outside is a new manifest",
        Eval(new Change(P("Pkg") + ".disabled", P("Pkg")), b0, J("Pkg", "unit=1"), live).Count == 0);
    Check("an unreadable base of a modified manifest fails closed",
        Eval(same, null, b0, live) is [{ Package: "ERR" }]);
    Check("renaming the package field does not hide a dropped target",
        Eval(same, b0, J("Ghost", "unit=50", ("A.cs", "unit=90,guard=0")), live) is [{ Scope: "package", Flag: "guard", Head: null }]);
    Check("manifest changed to invalid JSON fails closed",
        Eval(same, b0, "{ not json", live) is [{ Package: "ERR" }]);

    // R-ZERO (#2898)
    var bz = J("Pkg", "unit=50", ("A.cs", "unit=90"));
    var newGuardZero = J("Pkg", "unit=50", ("A.cs", "unit=90,guard=0"));
    var zeroCode = Eval(same, bz, newGuardZero, live, code);
    Check("a new 0 on a file with executable code is R-ZERO and prints the issue's line",
        zeroCode is [{ Scope: "A.cs", Flag: "guard", Base: null, Head: 0 }] && zeroCode[0].Format() == "Pkg  A.cs  guard  absent -> 0  (file has executable code)");
    Check("a new 0 on a file with no executable code is accepted",
        Eval(same, bz, newGuardZero, live, exempt).Count == 0);
    Check("a 0 unchanged from the base is accepted without asking the classifier",
        Eval(same, b0, b0, live, code).Count == 0);
    Check("a new 0 on integration or property is out of the rule",
        Eval(same, bz, J("Pkg", "unit=50", ("A.cs", "unit=90,integration=0,property=0")), live, code).Count == 0);
    Check("a new 0 on unit and on contract is R-ZERO too",
        Eval(same, bz, J("Pkg", "unit=50", ("A.cs", "unit=0,contract=0")), live, code) is [{ Flag: "unit" }, { Flag: "contract" }]);
    Check("a new 0 the classifier cannot answer for fails closed",
        Eval(same, bz, newGuardZero, live, unanswered) is [{ Flag: "guard", Note: "source not readable or not classified; failing closed" }]);
    Check("a new file entry with a 0 is R-ZERO",
        Eval(same, bz, J("Pkg", "unit=50", ("A.cs", "unit=90"), ("B.cs", "guard=0")), live, code) is [{ Scope: "B.cs", Flag: "guard" }]);
    Check("a new manifest with a 0 is R-ZERO",
        Eval(new Change(null, P("Pkg")), null, newGuardZero, live, code) is [{ Scope: "A.cs", Flag: "guard" }]);
    Check("the flag name is folded to lower case",
        Eval(same, bz, J("Pkg", "unit=50", ("A.cs", "unit=90,Guard=0")), live, code) is [{ Flag: "guard" }]);
    Check("a target lowered to 0 is reported once, by R-LOW",
        Eval(same, bz, J("Pkg", "unit=50", ("A.cs", "unit=0")), live, code) is [{ Flag: "unit", Base: 90, Head: 0, Note: null }]);
    Check("a control character in a file key is replaced on the line",
        Eval(same, bz, J("Pkg", "unit=50", ("A.cs", "unit=90"), ("B\\u0001.cs", "guard=0")), live, code) is [{ Scope: "B?.cs" }]);

    Check("differently cased manifest keys (Files, Targets) do not hide a zero",
        Eval(same, bz, "{\"Package\":\"Pkg\",\"Targets\":{\"unit\":50},\"Files\":{\"A.cs\":{\"targets\":{\"unit\":90}},\"B.cs\":{\"Targets\":{\"guard\":0}}}}", live, code)
            is [{ Scope: "B.cs", Flag: "guard" }]);
    Check("a package name with control characters or backticks is sanitized on the line",
        new Violation("P\u001b[0m`\n::x", "A.cs", "guard", null, 0).Format().StartsWith("P?[0m??::x  A.cs"));

    var zero = new List<Violation> { new("Pkg", "A.cs", "guard", null, 0, "file has executable code") };
    Check("R-ZERO without the label exits 1", Decide(1, zero, () => LabelResult.Absent, "target-change-approved").Exit == 1);
    Check("R-ZERO with the label exits 0 and prints it as approved",
        Decide(1, zero, () => LabelResult.Present, "target-change-approved") is { Exit: 0, Lines: [var z, ..] }
        && z == "APPROVED by target-change-approved  Pkg  A.cs  guard  absent -> 0  (file has executable code)");

    Check("the classifier verdict: exempt, not exempt, and anything else fails closed",
        ZeroSource.ParseVerdict(0, "exempt: interface\n") is { Answered: true, Exempt: true }
        && ZeroSource.ParseVerdict(1, "not exempt: method body\n") is { Answered: true, Exempt: false }
        && ZeroSource.ParseVerdict(1, "error CS1002: build failed\n") is { Answered: false }
        && ZeroSource.ParseVerdict(0, "garbage") is { Answered: false }
        && ZeroSource.ParseVerdict(2, "exempt: x") is { Answered: false }
        && ZeroSource.ParseVerdict(2, "not exempt: x") is { Answered: false }
        && ZeroSource.ParseVerdict(1, "exempt: x") is { Answered: false }
        && ZeroSource.ParseVerdict(0, "not exempt: x") is { Answered: false });

    var reads = 0;
    var seen = new List<string>();
    using (var src = new ZeroSource("HEAD", Tree("src/Pkg/A.cs"), (_, _) => { reads++; return "interface I { }"; },
        (file, flag) => { seen.Add($"{Path.GetFileName(file)}:{flag}:{File.ReadAllText(file)}"); return (0, "exempt: interface"); }))
    {
        Check("the classifier reads the head source once per file and asks the CLI per flag",
            src.Classify("Pkg", "A.cs", "guard") is { Exempt: true } && src.Classify("Pkg", "A.cs", "unit") is { Exempt: true }
            && reads == 1 && seen is ["A.cs:guard:interface I { }", "A.cs:unit:interface I { }"]);
        Check("a key that leaves the package folder or is not in the head tree is not classified",
            src.Classify("Pkg", "../Other/A.cs", "guard") is { Answered: false } && src.Classify("Pkg", "Missing.cs", "guard") is { Answered: false });
    }

    Check("a head source that cannot be read is not classified",
        new ZeroSource("HEAD", Tree("src/Pkg/A.cs"), (_, _) => null, (_, _) => (0, "exempt: x")).Classify("Pkg", "A.cs", "guard") is { Answered: false });

    Check("arguments: a valid command line parses",
        ParseArgs(["--base", "origin/main", "--pr", "7", "--assume-label"], out _) is { BaseRef: "origin/main", Pr: 7, AssumeLabel: true, HeadRef: "HEAD" });
    Check("arguments: a trailing option with no value is an error",
        ParseArgs(["--base"], out var e1) is null && e1.StartsWith("Unknown or incomplete argument: --base")
        && ParseArgs(["--base", "x", "--head"], out _) is null && ParseArgs(["--root"], out _) is null && ParseArgs(["--pr"], out _) is null);
    Check("arguments: a non-numeric --pr and an unknown option are errors, echoed safely",
        ParseArgs(["--pr", "x"], out _) is null && ParseArgs(["--bad\u001b[31m"], out var e2) is null && e2.EndsWith("--bad?[31m"));

    Check("ParseDiff follows renames, adds and deletes, and ignores defaults.json",
        ParseDiff($"R100\0{P("Old")}\0{P("Pkg")}\0A\0{P("New")}\0D\0{P("Gone")}\0M\0.github/coverage-manifest/defaults.json\0")
            is [{ BasePath: var a1, HeadPath: var a2 }, { BasePath: null }, { HeadPath: null }]
            && a1 == P("Old") && a2 == P("Pkg"));

    var viol = new List<Violation> { new("Pkg", "package", "unit", 50, 41) };
    var absent = Decide(1, viol, () => LabelResult.Absent, "target-change-approved");
    var present = Decide(1, viol, () => LabelResult.Present, "target-change-approved");
    var failed = Decide(1, viol, () => LabelResult.Failed, "target-change-approved");
    var clean = Decide(3, [], () => throw new InvalidOperationException("label must not be read without violations"), "x");
    Check("violation without the label exits 1 and prints the line", absent.Exit == 1 && absent.Lines[0] == "Pkg  package  unit  50 -> 41");
    Check("violation with the label exits 0 and prints it as approved",
        present.Exit == 0 && present.Lines[0] == "APPROVED by target-change-approved  Pkg  package  unit  50 -> 41");
    Check("a gh failure fails closed", failed.Exit == 1);
    Check("no violation exits 0 without reading the label", clean.Exit == 0 && clean.Lines[0] == "Target ratchet: 3 manifests changed, 0 violations");

    Console.WriteLine(failures == 0 ? "Self-test: all cases passed." : $"Self-test: {failures} case(s) FAILED.");
    return failures == 0 ? 0 : 1;
}

// ---------------------------------------------------------------------------------------------

enum LabelResult { Absent, Present, Failed }

record Change(string? BasePath, string? HeadPath);

record Outcome(int Exit, List<string> Lines);

static class Text
{
    // Backticks are replaced too: the violations are written inside a fenced block of the job summary.
    public static string Safe(string value)
    {
        var sb = new StringBuilder(Math.Min(value.Length, 240));
        foreach (var c in value.Take(240)) sb.Append(c is >= ' ' and <= '~' and not '`' ? c : '?');
        return sb.ToString();
    }
}

record Options(string? BaseRef, string HeadRef, string? Root, int? Pr, bool SelfTest, bool NoLabel, bool AssumeLabel);

// Answered: the classifier of #2890 gave a verdict. Exempt: it says the file has no executable code.
record ZeroVerdict(bool Answered, bool Exempt);

record Violation(string Package, string Scope, string Flag, double? Base, double? Head, string? Note = null)
{
    public string Format() =>
        $"{Text.Safe(Package)}  {Text.Safe(Scope)}  {Text.Safe(Flag)}{(Base is { } b ? b.ToString("0.##") : "absent")} -> {(Head is { } h ? h.ToString("0.##") : "(dropped)")}"
        + (Note is null ? "" : $"  ({Note})");
}

// Asks the ONE classifier of #2890 (`coverage-report.cs --classify-zero`) about a source file of the head
// tree. The file is read from the head revision (git show), not from the working tree, so --head can be
// any revision; it is written to a temp folder under its own file name and its path goes to the CLI.
sealed class ZeroSource(
    string headRef, HashSet<string> headTree, Func<string, string, string?> readAt,
    Func<string, string, (int Exit, string Out)> cli) : IDisposable
{
    // "exempt: <construct>" with exit 0 and "not exempt: <reason>" with exit 1 are the only answers of
    // `coverage-report.cs --classify-zero` (#2890). Anything else (a build failure also exits 1, a bad
    // argument exits 2) is no answer: the caller fails closed.
    public static ZeroVerdict ParseVerdict(int exit, string stdout)
    {
        var lines = stdout.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (exit == 0 && lines.Any(l => l.StartsWith("exempt:", StringComparison.Ordinal))) return new ZeroVerdict(true, true);
        if (exit == 1 && lines.Any(l => l.StartsWith("not exempt:", StringComparison.Ordinal))) return new ZeroVerdict(true, false);
        return new ZeroVerdict(false, false);
    }

    readonly Dictionary<string, string?> files = new(StringComparer.Ordinal);
    string? tempDir;

    public ZeroVerdict Classify(string package, string file, string flag)
    {
        var key = file.Replace('\\', '/');
        var path = $"src/{package}/{key}";
        // The head tree lists normalized paths, so a key with ".." or one outside src/ is not in it.
        if (!headTree.Contains(path)) return new ZeroVerdict(false, false);
        if (!files.TryGetValue(path, out var local))
        {
            var text = readAt(headRef, path);
            local = null;
            if (text is not null)
            {
                tempDir ??= Directory.CreateTempSubdirectory("ratchet-zero-").FullName;
                var dir = Directory.CreateDirectory(Path.Combine(tempDir, files.Count.ToString())).FullName;
                local = Path.Combine(dir, Path.GetFileName(key));
                File.WriteAllText(local, text);
            }

            files[path] = local;
        }

        if (local is null) return new ZeroVerdict(false, false);
        var r = cli(local, flag);
        return ParseVerdict(r.Exit, r.Out);
    }

    public void Dispose()
    {
        if (tempDir is not null) Directory.Delete(tempDir, recursive: true);
    }
}

sealed class Manifest
{
    public string Package { get; private init; } = "";
    public Dictionary<string, double> Targets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, Dictionary<string, double>> Files { get; } = new(StringComparer.Ordinal);

    public static Manifest? Parse(string json, string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var name = Prop(root, "package", out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()! : Path.GetFileNameWithoutExtension(path);
            var m = new Manifest { Package = name };
            if (Prop(root, "targets", out var t)) Read(t, m.Targets);
            if (Prop(root, "files", out var f) && f.ValueKind == JsonValueKind.Object)
            {
                foreach (var file in f.EnumerateObject())
                {
                    var flags = new Dictionary<string, double>(StringComparer.Ordinal);
                    if (file.Value.ValueKind == JsonValueKind.Object && Prop(file.Value, "targets", out var ft)) Read(ft, flags);
                    m.Files[file.Name] = flags;
                }
            }

            return m;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // coverage-report.cs deserializes with PropertyNameCaseInsensitive, so "Targets" and "Files" are
    // loaded there; matching case-insensitively here keeps a differently cased key from hiding a target.
    private static bool Prop(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var kv in obj.EnumerateObject())
        {
            if (string.Equals(kv.Name, name, StringComparison.OrdinalIgnoreCase)) { value = kv.Value; return true; }
        }

        value = default;
        return false;
    }

    private static void Read(JsonElement obj, Dictionary<string, double> into)
    {
        if (obj.ValueKind != JsonValueKind.Object) return;
        foreach (var kv in obj.EnumerateObject())
        {
            if (kv.Value.ValueKind == JsonValueKind.Number) into[kv.Name] = kv.Value.GetDouble();
        }
    }
}
