// check-pr-size.cs — Fails a pull request over 15 files or 1500 changed lines unless it carries
// the label size-exception-approved (#2901, EPIC #2845 working rule).
//
// Size is read from `git diff --shortstat <base>...<head>`: files changed, and lines = insertions
// plus deletions. Nothing is excluded (not lockfiles, generated files, docs or changelog.d): the
// label is the only exception route. Exactly 15 files and exactly 1500 lines pass.
//
// The label is read at run time with `gh pr view <pr> --json labels` (the job has no `labeled`
// trigger). A gh failure fails closed (exit 1). --assume-label skips gh (local runs, tests).
//
// Usage:
//   dotnet run --file .github/scripts/check-pr-size.cs -- --base <ref> --head <ref> (--pr <number> | --assume-label none|size-exception-approved)
//   dotnet run --file .github/scripts/check-pr-size.cs -- --self-test
//
// In CI pass --head ${{ github.event.pull_request.head.sha }}, not HEAD: on `pull_request` the
// checkout is the merge commit of the head into the current base tip (see
// changelog-fragments.cs, --check-unreleased-unchanged).
//
// Output: `files: N, lines: M (limits 15 / 1500)`, then `OK`, `APPROVED by size-exception-approved`
// or `FAIL: ...`.
// Exit codes: 0 within limits or approved; 1 over a limit without the label, git or gh failure;
// 2 invalid arguments.
// Requires: .NET 10+ (C# 14 file-based app).
#pragma warning disable CA1305, CA1310, CA1859, CA1852

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

const int MaxFiles = 15;
const int MaxLines = 1500;
const string ExceptionLabel = "size-exception-approved";

string? baseRef = null;
string? headRef = null;
string? pr = null;
string? assume = null;
var selfTest = false;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--self-test") selfTest = true;
    else if (args[i] == "--base" && i + 1 < args.Length) baseRef = args[++i];
    else if (args[i] == "--head" && i + 1 < args.Length) headRef = args[++i];
    else if (args[i] == "--pr" && i + 1 < args.Length) pr = args[++i];
    else if (args[i] == "--assume-label" && i + 1 < args.Length) assume = args[++i];
    else
    {
        Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
        return 2;
    }
}

if (selfTest) return SelfTest();

if (string.IsNullOrWhiteSpace(baseRef) || string.IsNullOrWhiteSpace(headRef))
{
    Console.Error.WriteLine("--base and --head are required.");
    return 2;
}

if (assume is not null && assume is not ("none" or ExceptionLabel))
{
    Console.Error.WriteLine($"--assume-label must be 'none' or '{ExceptionLabel}'.");
    return 2;
}

if (assume is not null && pr is not null)
{
    Console.Error.WriteLine("--pr and --assume-label are mutually exclusive.");
    return 2;
}

if (assume is null && !(pr is not null && int.TryParse(pr, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0))
{
    Console.Error.WriteLine("Give --pr <number> (positive integer) or --assume-label.");
    return 2;
}

var (ok, shortstat) = Run("git", DiffArguments(baseRef, headRef));
if (!ok)
{
    Console.Error.WriteLine("FAIL: git diff --shortstat failed (failing closed).");
    return 1;
}

return Evaluate(shortstat, assume is not null ? () => (bool?)(assume == ExceptionLabel) : () => PrHasLabel(pr!));

// The measurement must not depend on the pull request's own tree. `--attr-source=<empty tree>`
// (git 2.40+) ignores every .gitattributes, so a PR cannot hide lines with `*.cs -diff`; `--text`
// counts the lines of files git considers binary; `--no-renames` counts a rename as delete + add.
// (--text alone does not override a `-diff` attribute: verified with a scratch repository.)
static string[] DiffArguments(string baseRef, string headRef) =>
    ["--attr-source=4b825dc642cb6eb9a060e54bf8d69288fbee4904", "diff", "--text", "--no-renames", "--shortstat", $"{baseRef}...{headRef}"];

static int Evaluate(string shortstat, Func<bool?> hasLabel)
{
    if (!string.IsNullOrWhiteSpace(shortstat) && !Regex.IsMatch(shortstat, @"\d+ files? changed"))
    {
        Console.Error.WriteLine("FAIL: unrecognized git --shortstat output (failing closed).");
        return 1;
    }

    var (files, lines) = ParseShortstat(shortstat);
    Console.WriteLine($"files: {files}, lines: {lines} (limits {MaxFiles} / {MaxLines})");
    if (files <= MaxFiles && lines <= MaxLines)
    {
        Console.WriteLine("OK");
        return 0;
    }

    var label = hasLabel();
    if (label is null)
    {
        Console.Error.WriteLine("FAIL: over the limits and the label lookup failed (failing closed).");
        return 1;
    }

    if (label == true)
    {
        Console.WriteLine($"APPROVED by {ExceptionLabel}");
        return 0;
    }

    Console.Error.WriteLine($"FAIL: over the limits (files > {MaxFiles} or lines > {MaxLines}); split the pull request or a maintainer adds the label {ExceptionLabel}.");
    return 1;
}

static (int Files, int Lines) ParseShortstat(string text)
{
    static int Num(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
    }

    return (Num(text, @"(\d+) files? changed"),
        Num(text, @"(\d+) insertions?\(\+\)") + Num(text, @"(\d+) deletions?\(-\)"));
}

// null: gh failed or its output is unreadable.
static bool? PrHasLabel(string pr)
{
    var (ok, json) = Run("gh", ["pr", "view", pr, "--json", "labels"]);
    return ok ? ParseLabels(json) : null;
}

static bool? ParseLabels(string json)
{
    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("labels", out var labels)) return null;
        foreach (var l in labels.EnumerateArray())
        {
            if (l.TryGetProperty("name", out var name) && name.GetString() == ExceptionLabel) return true;
        }

        return false;
    }
    catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
    {
        return null;
    }
}

static (bool Ok, string Output) Run(string file, string[] arguments, string? workDir = null)
{
    try
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.Environment["LC_ALL"] = "C"; // git/gh output parsing must not depend on the runner locale
        if (workDir is not null) psi.WorkingDirectory = workDir;
        foreach (var a in arguments) psi.ArgumentList.Add(a);
        using var proc = Process.Start(psi);
        if (proc is null) return (false, "");
        var stderr = proc.StandardError.ReadToEndAsync();
        var stdout = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            Console.Error.WriteLine(stderr.Result.Trim());
            return (false, stdout);
        }

        return (true, stdout);
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
    {
        Console.Error.WriteLine($"{file} could not be started.");
        return (false, "");
    }
}

static int SelfTest()
{
    var failures = 0;

    void Expect(string name, bool condition)
    {
        if (condition) return;
        failures++;
        Console.Error.WriteLine($"SELF-TEST FAILED: {name}");
    }

    int Quiet(string stat, Func<bool?> label)
    {
        var o = Console.Out;
        var e = Console.Error;
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
        try { return Evaluate(stat, label); }
        finally { Console.SetOut(o); Console.SetError(e); }
    }

    bool? Absent() => false;
    bool? Present() => true;
    bool? GhFails() => null;
    bool? MustNotBeCalled() => throw new InvalidOperationException("label read although within limits");

    Expect("15 files / 1500 lines pass", Quiet(" 15 files changed, 1000 insertions(+), 500 deletions(-)", MustNotBeCalled) == 0);
    Expect("16 files fail", Quiet(" 16 files changed, 10 insertions(+)", Absent) == 1);
    Expect("1501 lines fail", Quiet(" 2 files changed, 1000 insertions(+), 501 deletions(-)", Absent) == 1);
    Expect("label present passes", Quiet(" 16 files changed, 10 insertions(+)", Present) == 0);
    Expect("label present passes on lines", Quiet(" 3 files changed, 2000 insertions(+)", Present) == 0);
    Expect("label absent fails", Quiet(" 20 files changed, 5 deletions(-)", Absent) == 1);
    Expect("gh failure fails closed", Quiet(" 16 files changed, 10 insertions(+)", GhFails) == 1);
    Expect("gh failure within limits does not matter", Quiet(" 1 file changed, 1 insertion(+)", GhFails) == 0);
    Expect("unrecognized shortstat fails closed", Quiet(" 1 fichier modifie, 3 insertions(+)", Absent) == 1);
    Expect("empty diff passes",Quiet("", MustNotBeCalled) == 0);
    Expect("files only (mode change)", ParseShortstat(" 4 files changed") == (4, 0));
    Expect("insertions only", ParseShortstat(" 1 file changed, 7 insertions(+)") == (1, 7));
    Expect("deletions only", ParseShortstat(" 1 file changed, 1 deletion(-)") == (1, 1));
    Expect("both", ParseShortstat(" 2 files changed, 3 insertions(+), 4 deletions(-)\n") == (2, 7));
    Expect("empty shortstat", ParseShortstat("") == (0, 0));
    Expect("labels parse: present", ParseLabels("{\"labels\":[{\"name\":\"bug\"},{\"name\":\"size-exception-approved\"}]}") == true);
    Expect("labels parse: absent", ParseLabels("{\"labels\":[{\"name\":\"bug\"}]}") == false);
    Expect("labels parse: empty", ParseLabels("{\"labels\":[]}") == false);
    Expect("labels parse: garbage fails closed", ParseLabels("not json") is null);
    Expect("labels parse: no labels property fails closed", ParseLabels("{}") is null);

    var diffArgs = DiffArguments("A", "B");
    Expect("diff command ignores .gitattributes", diffArgs.Any(a => a.StartsWith("--attr-source=", StringComparison.Ordinal)));
    Expect("diff command uses --text", diffArgs.Contains("--text"));
    Expect("diff command uses --no-renames", diffArgs.Contains("--no-renames"));
    Expect("diff command is a three-dot shortstat", diffArgs.Contains("--shortstat") && diffArgs.Contains("A...B"));

    // End to end: a PR that adds `*.cs -diff` and two renamed-and-edited files must still count fully.
    var scratch = Path.Combine(Path.GetTempPath(), "check-pr-size-selftest-" + Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(scratch);
        bool Git(params string[] a) => Run("git", ["-c", "user.name=t", "-c", "user.email=t@t", "-c", "commit.gpgsign=false", .. a], scratch).Ok;
        string Head() => Run("git", ["rev-parse", "HEAD"], scratch).Output.Trim();
        var body = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"line {i}")) + "\n";
        File.WriteAllText(Path.Combine(scratch, "old.txt"), body);
        var setup = Git("init", "-q") && Git("add", "-A") && Git("commit", "-q", "-m", "base");
        var baseSha = setup ? Head() : "";
        for (var i = 1; i <= 3; i++) File.WriteAllText(Path.Combine(scratch, $"f{i}.cs"), body);
        File.WriteAllText(Path.Combine(scratch, ".gitattributes"), "*.cs -diff\n");
        setup = setup && Git("add", "-A") && Git("commit", "-q", "-m", "pr");
        var headSha = setup ? Head() : "";
        Expect("scratch repository built", setup);
        var (diffOk, stat) = Run("git", DiffArguments(baseSha, headSha), scratch);
        Expect("scratch diff succeeds", diffOk);
        Expect("`*.cs -diff` cannot hide lines (301 expected)", ParseShortstat(stat) == (4, 301));
    }
    finally
    {
        try { Directory.Delete(scratch, recursive: true); } catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    if (failures == 0)
    {
        Console.WriteLine("Self-test OK.");
        return 0;
    }

    Console.Error.WriteLine($"{failures} self-test case(s) failed.");
    return 1;
}
