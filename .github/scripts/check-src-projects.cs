// check-src-projects.cs — Fails when a directory directly under src/ has no direct-child *.csproj (#2838).
//
// Every directory counts, tracked or not: a directory that holds only git-ignored bin/ and obj/
// output (the leftover of a removed package) fails too, because a CI checkout can never contain it
// and the guard is meant to be run by hand on a disk. For each offending directory the message says
// how many files git tracks in it (git ls-files; skipped when git is unavailable) and, when none,
// "delete the local leftover directory".
//
// Usage:
//   dotnet run --file .github/scripts/check-src-projects.cs -- [--root <repo root>]
//   dotnet run --file .github/scripts/check-src-projects.cs -- --self-test
//
// The repository root is --root, otherwise the nearest ancestor of the current directory that holds
// Encina.slnx, otherwise the current directory.
//
// Exit codes: 0 every src/ directory has a project; 1 at least one has none, or src/ is missing or
// empty (never a vacuous pass); 2 invalid arguments.
// Requires: .NET 10+ (C# 14 file-based app).
#pragma warning disable CA1305, CA1310, CA1859, CA1852

using System.Diagnostics;

string? rootArg = null;
var selfTest = false;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--self-test") selfTest = true;
    else if (args[i] == "--root" && i + 1 < args.Length) rootArg = args[++i];
    else
    {
        Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
        return 2;
    }
}

if (selfTest) return SelfTest();

var root = rootArg ?? FindRoot(Directory.GetCurrentDirectory());
var problems = Check(root, withGit: true);
if (problems.Count == 0)
{
    var count = Directory.GetDirectories(Path.Combine(root, "src")).Length;
    Console.WriteLine($"OK: all {count} directories under src/ contain a project file.");
    return 0;
}

foreach (var p in problems) Console.Error.WriteLine($"ERROR: {p}");
Console.Error.WriteLine($"{problems.Count} problem(s) found.");
return 1;

static string FindRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Encina.slnx"))) return dir.FullName;
    }

    return start;
}

static List<string> Check(string root, bool withGit)
{
    var problems = new List<string>();
    var src = Path.Combine(root, "src");
    if (!Directory.Exists(src))
    {
        problems.Add($"src/ does not exist under '{root}'; nothing was checked.");
        return problems;
    }

    var dirs = Directory.GetDirectories(src);
    if (dirs.Length == 0)
    {
        problems.Add("src/ contains no directories; nothing was checked.");
        return problems;
    }

    foreach (var dir in dirs.OrderBy(d => d, StringComparer.Ordinal))
    {
        if (Directory.EnumerateFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Any()) continue;

        var name = Path.GetFileName(dir);
        var message = $"src/{name} has no .csproj directly inside it.";
        var tracked = withGit ? CountTracked(root, $"src/{name}") : null;
        if (tracked is null) message += " Tracked file count unknown (git unavailable).";
        else if (tracked == 0) message += " git tracks 0 files in it: delete the local leftover directory.";
        else message += $" git tracks {tracked} file(s) in it: add the project or move the files out.";
        problems.Add(message);
    }

    return problems;
}

static int? CountTracked(string root, string relativeDir)
{
    try
    {
        var psi = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(root);
        psi.ArgumentList.Add("ls-files");
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add(relativeDir);
        using var proc = Process.Start(psi);
        if (proc is null) return null;
        var stdout = proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0) return null;
        return stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }
    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
    {
        return null;
    }
}

static int SelfTest()
{
    var failures = 0;

    void Case(string name, Action<string> build, bool expectFail, string? expectText = null)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "check-src-projects-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            build(tmp);
            var problems = Check(tmp, withGit: true);
            var ok = (problems.Count > 0) == expectFail
                && (expectText is null || problems.Any(p => p.Contains(expectText, StringComparison.Ordinal)));
            Console.WriteLine($"{(ok ? "PASS" : "FAIL")}: {name} ({problems.Count} problem(s))");
            if (!ok)
            {
                failures++;
                foreach (var p in problems) Console.WriteLine($"  {p}");
            }
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch (IOException) { }
        }
    }

    static void Pkg(string root, string name)
    {
        var d = Path.Combine(root, "src", name);
        Directory.CreateDirectory(d);
        File.WriteAllText(Path.Combine(d, name + ".csproj"), "<Project />");
    }

    Case("all directories have a project", r => { Pkg(r, "Encina.A"); Pkg(r, "Encina.B"); }, expectFail: false);

    Case("a directory without a project fails", r =>
    {
        Pkg(r, "Encina.A");
        var d = Path.Combine(r, "src", "Encina.Loose");
        Directory.CreateDirectory(d);
        File.WriteAllText(Path.Combine(d, "Class1.cs"), "class C {}");
    }, expectFail: true, expectText: "src/Encina.Loose");

    Case("a directory with only bin/obj fails", r =>
    {
        Pkg(r, "Encina.A");
        var d = Path.Combine(r, "src", "Encina.Gone");
        Directory.CreateDirectory(Path.Combine(d, "bin"));
        Directory.CreateDirectory(Path.Combine(d, "obj"));
        File.WriteAllText(Path.Combine(d, "Encina.Gone.csproj.user"), "<Project />");
    }, expectFail: true, expectText: "src/Encina.Gone");

    Case("an empty src/ fails", r => Directory.CreateDirectory(Path.Combine(r, "src")), expectFail: true, expectText: "no directories");

    Case("a missing src/ fails", _ => { }, expectFail: true, expectText: "does not exist");

    Console.WriteLine(failures == 0 ? "Self-test passed." : $"Self-test failed: {failures} case(s).");
    return failures == 0 ? 0 : 1;
}
