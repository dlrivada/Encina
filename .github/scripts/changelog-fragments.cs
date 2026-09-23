// changelog-fragments.cs — Changelog fragment tooling (see changelog.d/README.md).
//
// PRs developed in parallel all appended to "## [Unreleased]" in CHANGELOG.md and conflicted on
// every merge. Instead, a PR drops one file per changelog entry under changelog.d/; the entries
// are merged (this script) only at release time, so parallel PRs never touch the same line.
//
// File name:    changelog.d/<issue>-<short-slug>.<section>.md (directly under changelog.d/, no
//               subfolders; changelog.d/README.md is the only exempt file)
// section is one of: added | changed | deprecated | removed | fixed | security (Keep a Changelog)
// Content:      one or more ENTRIES. An entry is a top-level "- " bullet plus every following
//               line that is indented (continuation prose, nested bullets), blank while inside a
//               fenced code block, or part of a fenced code block (opened either indented or at
//               column 0), up to the next top-level bullet or heading. A blank line NOT inside a
//               fence, followed by something other than indented/fence content, ends the entry.
//
// Usage:
//   dotnet run .github/scripts/changelog-fragments.cs -- --check [--fragments-dir <dir>]
//   dotnet run .github/scripts/changelog-fragments.cs -- --preview [--fragments-dir <dir>] [--changelog <file>]
//   dotnet run .github/scripts/changelog-fragments.cs -- --release <version> <yyyy-MM-dd> [title] \
//       [--fragments-dir <dir>] [--changelog <file>]
//   dotnet run .github/scripts/changelog-fragments.cs -- --check-unreleased-unchanged <base-ref> [--changelog <file>]
//
// --check      validates every fragment file (name, location, section and entry grammar);
//              exit 1 on violation.
// --preview    prints the "## [Unreleased]" section as it would read: CHANGELOG.md's current
//              Unreleased entries merged with the fragments, grouped by section in Keep a
//              Changelog order. Read-only. Never drops content: if "## [Unreleased]" contains
//              anything that is not the pending note, a known "### <Section>" heading or a
//              well-formed entry, nothing is printed — the offending lines are listed and the
//              process exits 1, telling the caller to move them into a known section or a
//              changelog.d/ fragment.
// --release    folds the merged Unreleased content into CHANGELOG.md as a new dated
//              "## [<version>] - <date>[ - <title>]" section (keeping an empty "## [Unreleased]"
//              above it, with exactly one blank line before the new heading), then deletes the
//              fragment files that were folded in. Same content guarantee as --preview, plus:
//              refuses an empty/whitespace version, a version that already has a "## [<version>]"
//              section, and a merge that would produce an empty release section.
// --check-unreleased-unchanged <base-ref>
//              fails if the "## [Unreleased]" section of --changelog differs between <base-ref>
//              and the working tree — used in CI to catch a PR that hand-edits Unreleased instead
//              of adding a changelog.d/ fragment (the whole reason this tooling exists).
//
// Ordering is deterministic: by section (Added, Changed, Deprecated, Removed, Fixed, Security),
// then issue number, then file name. CHANGELOG.md's line endings and byte-order-mark are preserved.
//
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305, CA1310, CA1859

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var fragmentsDir = "changelog.d";
var changelogPath = "CHANGELOG.md";
var mode = "";
string? releaseVersion = null;
string? releaseDate = null;
string? releaseTitle = null;
string? baseRef = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--check":
            mode = "check";
            break;
        case "--preview":
            mode = "preview";
            break;
        case "--release":
            mode = "release";
            if (i + 1 >= args.Length || i + 2 >= args.Length)
            {
                Console.Error.WriteLine("ERROR: --release requires <version> <yyyy-MM-dd> [title]");
                Environment.Exit(2);
                return;
            }
            releaseVersion = args[++i];
            releaseDate = args[++i];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                releaseTitle = args[++i];
            break;
        case "--check-unreleased-unchanged":
            mode = "check-unreleased-unchanged";
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("ERROR: --check-unreleased-unchanged requires <base-ref>");
                Environment.Exit(2);
                return;
            }
            baseRef = args[++i];
            break;
        case "--fragments-dir":
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("ERROR: --fragments-dir requires a value");
                Environment.Exit(2);
                return;
            }
            fragmentsDir = args[++i];
            break;
        case "--changelog":
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("ERROR: --changelog requires a value");
                Environment.Exit(2);
                return;
            }
            changelogPath = args[++i];
            break;
        default:
            Console.Error.WriteLine($"ERROR: unrecognized argument '{args[i]}'");
            Environment.Exit(2);
            return;
    }
}

if (mode.Length == 0)
{
    Console.Error.WriteLine("Usage: changelog-fragments.cs (--check | --preview | --release <version> <yyyy-MM-dd> [title] | --check-unreleased-unchanged <base-ref>) [--fragments-dir <dir>] [--changelog <file>]");
    Environment.Exit(2);
    return;
}

// Keep a Changelog section order.
var sectionOrder = new[] { "Added", "Changed", "Deprecated", "Removed", "Fixed", "Security" };
var sectionKeyToTitle = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["added"] = "Added",
    ["changed"] = "Changed",
    ["deprecated"] = "Deprecated",
    ["removed"] = "Removed",
    ["fixed"] = "Fixed",
    ["security"] = "Security",
};

const string PendingNote = "> Pending changelog entries are added as fragments under `changelog.d/` until release; see `changelog.d/README.md`.";

// Structural name check first (loose section group) so a bad-name error and an unknown-section
// error are reported as two distinct problems, not folded into one generic message.
var nameRegex = new Regex(@"^(?<issue>[0-9]+)-(?<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.(?<section>[a-z]+)\.md$", RegexOptions.Compiled);

// ── --check-unreleased-unchanged: standalone, does not touch fragments ─────────────────────────
if (mode == "check-unreleased-unchanged")
{
    if (!File.Exists(changelogPath))
    {
        Console.Error.WriteLine($"ERROR: changelog not found: {changelogPath}");
        Environment.Exit(2);
        return;
    }

    var currentUnreleased = NormalizeUnreleasedForComparison(ExtractUnreleasedRaw(File.ReadAllText(changelogPath)));

    string mergeBase;
    string baseText;
    try
    {
        mergeBase = RunGitMergeBase(baseRef!);
        baseText = RunGitShow(mergeBase, changelogPath);
    }
    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
    {
        Console.Error.WriteLine($"ERROR: failed to read {changelogPath} from the merge base of '{baseRef}': {ex.Message}");
        Environment.Exit(2);
        return;
    }

    var baseUnreleased = NormalizeUnreleasedForComparison(ExtractUnreleasedRaw(baseText));

    if (!string.Equals(currentUnreleased, baseUnreleased, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"ERROR: this change edits the '## [Unreleased]' section of {changelogPath} — add a fragment under changelog.d/ instead");
        Environment.Exit(1);
        return;
    }

    Console.WriteLine($"OK: '## [Unreleased]' section of {changelogPath} is unchanged relative to the merge base of '{baseRef}' ({mergeBase})");
    return;
}

var (fragments, errors) = LoadFragments(fragmentsDir);

if (errors.Count > 0)
{
    foreach (var error in errors)
        Console.Error.WriteLine($"ERROR: {error}");
    Console.Error.WriteLine($"{errors.Count} fragment error(s) in {fragmentsDir}/");
    Environment.Exit(1);
    return;
}

if (mode == "check")
{
    Console.WriteLine(fragments.Count == 0
        ? $"OK: no fragments in {fragmentsDir}/"
        : $"OK: {fragments.Count} fragment(s) validated in {fragmentsDir}/");
    return;
}

// --preview and --release both need the merged Unreleased content.
if (!File.Exists(changelogPath))
{
    Console.Error.WriteLine($"ERROR: changelog not found: {changelogPath}");
    Environment.Exit(1);
    return;
}

var (rawText, useCrlf, hasBom, trailingNewline) = ReadRaw(changelogPath);
var lines = SplitLines(rawText);
var unreleasedStart = lines.FindIndex(l => l.Trim() == "## [Unreleased]");
if (unreleasedStart < 0)
{
    Console.Error.WriteLine($"ERROR: '## [Unreleased]' heading not found in {changelogPath}");
    Environment.Exit(1);
    return;
}
var unreleasedEnd = lines.FindIndex(unreleasedStart + 1, l => l.StartsWith("## [", StringComparison.Ordinal));
if (unreleasedEnd < 0) unreleasedEnd = lines.Count;

var (existingSections, problems) = ParseUnreleasedBody(lines, unreleasedStart + 1, unreleasedEnd, sectionOrder, PendingNote);
if (problems.Count > 0)
{
    Console.Error.WriteLine($"ERROR: '## [Unreleased]' in {changelogPath} has content that --{mode} cannot merge safely:");
    foreach (var p in problems)
        Console.Error.WriteLine($"  {p}");
    Console.Error.WriteLine("Move each line above into a known section (### Added/Changed/Deprecated/Removed/Fixed/Security) or into a changelog.d/ fragment, then retry. Nothing was written.");
    Environment.Exit(1);
    return;
}

var merged = MergeSections(existingSections, fragments, sectionOrder);

if (mode == "preview")
{
    Console.WriteLine("## [Unreleased]");
    Console.WriteLine();
    Console.WriteLine(PendingNote);
    foreach (var line in BuildSectionBlock(merged, sectionOrder))
        Console.WriteLine(line);
    return;
}

// mode == "release"
if (string.IsNullOrWhiteSpace(releaseVersion))
{
    Console.Error.WriteLine("ERROR: --release version must not be empty or whitespace");
    Environment.Exit(1);
    return;
}

if (!DateOnly.TryParseExact(releaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
{
    Console.Error.WriteLine($"ERROR: --release date '{releaseDate}' is not in yyyy-MM-dd format");
    Environment.Exit(1);
    return;
}

var versionHeadingPrefix = $"## [{releaseVersion}]";
if (lines.Any(l => l.StartsWith(versionHeadingPrefix, StringComparison.Ordinal)))
{
    Console.Error.WriteLine($"ERROR: a '{versionHeadingPrefix}' section already exists in {changelogPath}");
    Environment.Exit(1);
    return;
}

if (merged.Values.All(entries => entries.Count == 0))
{
    Console.Error.WriteLine($"ERROR: nothing to release — '## [Unreleased]' in {changelogPath} and {fragmentsDir}/ have no entries");
    Environment.Exit(1);
    return;
}

var headerLine = releaseTitle is null
    ? $"## [{releaseVersion}] - {releaseDate}"
    : $"## [{releaseVersion}] - {releaseDate} - {releaseTitle}";

// BuildSectionBlock prefixes each section with its own blank line and (since merged is
// non-empty, guaranteed above) always ends with exactly one blank line, which is what keeps a
// single blank line between the folded release section and the next "## [" heading (or EOF).
var releaseBlock = new List<string> { headerLine };
releaseBlock.AddRange(BuildSectionBlock(merged, sectionOrder));

var newLines = new List<string>
{
    lines[unreleasedStart],
    "",
    PendingNote,
    "",
};
newLines.AddRange(releaseBlock);
newLines.AddRange(lines.Skip(unreleasedEnd));

WriteRaw(changelogPath, newLines, useCrlf, hasBom, trailingNewline);

foreach (var fragment in fragments)
    File.Delete(fragment.Path);

Console.WriteLine($"OK: folded {fragments.Count} fragment(s) into {changelogPath} as [{releaseVersion}] - {releaseDate}" + (releaseTitle is null ? "" : $" - {releaseTitle}"));
Console.WriteLine($"OK: deleted {fragments.Count} fragment file(s) from {fragmentsDir}/");
return;

// ── Helpers ─────────────────────────────────────────────────────────────────

(List<Fragment> Fragments, List<string> Errors) LoadFragments(string dir)
{
    var found = new List<Fragment>();
    var errs = new List<string>();

    if (!Directory.Exists(dir))
        return (found, errs);

    foreach (var path in Directory.GetFiles(dir, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
    {
        var relPath = Path.GetRelativePath(dir, path).Replace('\\', '/');

        if (string.Equals(relPath, "README.md", StringComparison.OrdinalIgnoreCase))
            continue;

        if (relPath.Contains('/', StringComparison.Ordinal))
        {
            errs.Add($"{relPath}: fragments must live directly under {dir}/, not in a subfolder");
            continue;
        }

        var match = nameRegex.Match(relPath);
        if (!match.Success)
        {
            errs.Add($"{relPath}: bad fragment file name — expected '<issue>-<short-slug>.<section>.md' (e.g. '1125-define-eventids.fixed.md')");
            continue;
        }

        var sectionKey = match.Groups["section"].Value;
        if (!sectionKeyToTitle.TryGetValue(sectionKey, out var sectionTitle))
        {
            errs.Add($"{relPath}: unknown section '{sectionKey}' — expected one of added, changed, deprecated, removed, fixed, security");
            continue;
        }

        var issueText = match.Groups["issue"].Value;
        if (!int.TryParse(issueText, NumberStyles.None, CultureInfo.InvariantCulture, out var issue))
        {
            errs.Add($"{relPath}: issue number '{issueText}' does not fit a 32-bit integer");
            continue;
        }
        if (issue <= 0)
        {
            errs.Add($"{relPath}: issue number must be greater than 0, got '{issueText}'");
            continue;
        }

        var contentLines = File.ReadAllLines(path).ToList();
        var (entries, entryErrors) = ParseEntryBlock(contentLines);
        if (entryErrors.Count > 0)
        {
            foreach (var e in entryErrors)
                errs.Add($"{relPath}: {e}");
            continue;
        }
        if (entries.Count == 0)
        {
            errs.Add($"{relPath}: fragment has no entries");
            continue;
        }

        found.Add(new Fragment(path, relPath, issue, sectionTitle, entries));
    }

    return (found, errs);
}

static (string Text, bool UseCrlf, bool HasBom, bool TrailingNewline) ReadRaw(string path)
{
    var bytes = File.ReadAllBytes(path);
    var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    var text = File.ReadAllText(path); // strips the BOM if present, decodes correctly either way
    var useCrlf = text.Contains("\r\n", StringComparison.Ordinal);
    var trailingNewline = text.EndsWith('\n');
    return (text, useCrlf, hasBom, trailingNewline);
}

static List<string> SplitLines(string text)
{
    var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
    var trailingNewline = normalized.EndsWith('\n');
    var lines = normalized.Split('\n').ToList();
    if (trailingNewline && lines.Count > 0 && lines[^1].Length == 0)
        lines.RemoveAt(lines.Count - 1);
    return lines;
}

static void WriteRaw(string path, List<string> lines, bool useCrlf, bool hasBom, bool trailingNewline)
{
    var newline = useCrlf ? "\r\n" : "\n";
    var text = string.Join(newline, lines);
    if (trailingNewline) text += newline;
    var encoding = new UTF8Encoding(hasBom);
    File.WriteAllText(path, text, encoding);
}

static string ExtractUnreleasedRaw(string text)
{
    var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
    var lines = normalized.Split('\n').ToList();
    var start = lines.FindIndex(l => l.Trim() == "## [Unreleased]");
    if (start < 0) return "";
    var end = lines.FindIndex(start + 1, l => l.StartsWith("## [", StringComparison.Ordinal));
    if (end < 0) end = lines.Count;
    return string.Join("\n", lines.Skip(start).Take(end - start)).TrimEnd();
}

static string RunGitMergeBase(string baseRef)
{
    var psi = new System.Diagnostics.ProcessStartInfo("git")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    psi.ArgumentList.Add("merge-base");
    psi.ArgumentList.Add(baseRef);
    psi.ArgumentList.Add("HEAD");

    using var proc = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("failed to start git");
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    if (proc.ExitCode != 0)
        throw new InvalidOperationException(stderr.Trim().Length > 0 ? stderr.Trim() : $"git merge-base exited with code {proc.ExitCode}");
    return stdout.Trim();
}

// Ignores the tooling's own pointer note (added independently of any real content change) plus
// trailing whitespace per line and blank-line-only differences, so a PR that only adds the note
// (or gets reformatted by whitespace) is not flagged as editing '## [Unreleased]'.
static string NormalizeUnreleasedForComparison(string text)
{
    var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
    var lines = normalized.Split('\n')
        .Select(l => l.TrimEnd())
        .Where(l => l.Length > 0 && !l.StartsWith("> Pending changelog entries are added as fragments under", StringComparison.Ordinal));
    return string.Join("\n", lines);
}

static string RunGitShow(string gitRef, string path)
{
    var gitPath = path.Replace('\\', '/');
    var psi = new System.Diagnostics.ProcessStartInfo("git")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    psi.ArgumentList.Add("show");
    psi.ArgumentList.Add($"{gitRef}:{gitPath}");

    using var proc = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("failed to start git");
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    if (proc.ExitCode != 0)
        throw new InvalidOperationException(stderr.Trim().Length > 0 ? stderr.Trim() : $"git show exited with code {proc.ExitCode}");
    return stdout;
}

// A top-level bullet has no leading whitespace: "- " at column 0.
static bool IsTopLevelBullet(string line) => line.Length >= 2 && line[0] == '-' && line[1] == ' ';

// Any unindented line starting with '#' is heading-like and always ends entry/entry-block
// consumption, whether or not it turns out to be a section we recognize.
static bool IsHeadingLine(string line) => line.Length > 0 && line[0] == '#';

static bool IsIndented(string line) => line.Length > 0 && (line[0] == ' ' || line[0] == '\t');

static bool IsFenceLine(string line)
{
    var trimmed = line.TrimStart();
    return trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal);
}

// Consumes one entry starting at a top-level bullet: the bullet line itself, plus every
// following line that is indented, part of a fenced code block (open at any indentation), or
// blank immediately before more indented/fenced content. Stops at the next top-level bullet,
// any heading line, or a blank line that is not followed by continuation content.
static (List<string> Lines, int NextIndex, bool EmptyBullet) ConsumeEntry(List<string> lines, int start, int end)
{
    var entryLines = new List<string> { lines[start] };
    var emptyBullet = lines[start].Length <= 2 || lines[start][2..].Trim().Length == 0;
    var i = start + 1;
    var inFence = false;

    while (i < end)
    {
        var l = lines[i];

        if (inFence)
        {
            entryLines.Add(l);
            i++;
            if (IsFenceLine(l)) inFence = false;
            continue;
        }

        if (l.Trim().Length == 0)
        {
            var j = i;
            while (j < end && lines[j].Trim().Length == 0) j++;
            if (j < end && (IsIndented(lines[j]) || IsFenceLine(lines[j])))
            {
                while (i < j) { entryLines.Add(lines[i]); i++; }
                continue;
            }
            break; // trailing blank line(s) are separator whitespace, not part of this entry
        }

        if (IsTopLevelBullet(l) || IsHeadingLine(l))
            break;

        if (IsIndented(l))
        {
            entryLines.Add(l);
            if (IsFenceLine(l)) inFence = true;
            i++;
            continue;
        }

        if (IsFenceLine(l))
        {
            entryLines.Add(l);
            inFence = true;
            i++;
            continue;
        }

        break; // unindented prose/other content does not belong to this entry
    }

    return (entryLines, i, emptyBullet);
}

// Parses a whole fragment file (or any flat block) as zero or more entries; anything that is not
// blank and not a top-level bullet (nor that bullet's continuation, consumed by ConsumeEntry) is
// an error.
static (List<List<string>> Entries, List<string> Errors) ParseEntryBlock(List<string> lines)
{
    var entries = new List<List<string>>();
    var errs = new List<string>();
    var i = 0;
    var n = lines.Count;

    while (i < n)
    {
        var line = lines[i];
        if (line.Trim().Length == 0) { i++; continue; }

        if (IsTopLevelBullet(line))
        {
            var (entryLines, next, emptyBullet) = ConsumeEntry(lines, i, n);
            if (emptyBullet)
                errs.Add($"line {i + 1}: bullet has no text: '{line}'");
            else
                entries.Add(entryLines);
            i = next;
            continue;
        }

        errs.Add($"line {i + 1}: expected a top-level '- ' bullet or a blank line, found: '{line}'");
        i++;
    }

    return (entries, errs);
}

// Parses the body of "## [Unreleased]" (the lines strictly between the heading and the next
// "## [" heading or EOF) into its known sections. Anything that is not the pending note, a known
// "### <Section>" heading or a well-formed entry under one is reported as a problem instead of
// being silently dropped.
static (Dictionary<string, List<List<string>>> Sections, List<string> Problems) ParseUnreleasedBody(
    List<string> lines, int start, int end, string[] knownSections, string pendingNote)
{
    var sections = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal);
    var problems = new List<string>();
    var knownSet = new HashSet<string>(knownSections, StringComparer.Ordinal);
    var i = start;
    string? current = null;
    var currentKnown = false;

    while (i < end)
    {
        var line = lines[i];

        if (line.Trim().Length == 0) { i++; continue; }
        if (line == pendingNote) { i++; continue; }

        if (line.StartsWith("### ", StringComparison.Ordinal))
        {
            var name = line[4..].Trim();
            current = name;
            currentKnown = knownSet.Contains(name);
            if (!currentKnown)
                problems.Add($"line {i + 1}: unknown section heading, expected one of ### {string.Join('/', knownSections)}: '{line}'");
            else if (!sections.ContainsKey(name))
                sections[name] = new List<List<string>>();
            i++;
            continue;
        }

        if (IsTopLevelBullet(line))
        {
            var (entryLines, next, emptyBullet) = ConsumeEntry(lines, i, end);
            if (emptyBullet)
                problems.Add($"line {i + 1}: bullet has no text: '{line}'");
            else if (current is null)
                problems.Add($"line {i + 1}: entry appears before any '### ' section heading: '{line}'");
            else if (!currentKnown)
                problems.Add($"line {i + 1}: entry under unknown section '{current}': '{line}'");
            else
                sections[current!].Add(entryLines);
            i = next;
            continue;
        }

        problems.Add($"line {i + 1}: unexpected content in '## [Unreleased]': '{line}'");
        i++;
    }

    return (sections, problems);
}

static Dictionary<string, List<List<string>>> MergeSections(
    Dictionary<string, List<List<string>>> existing, List<Fragment> fragments, string[] sectionOrder)
{
    var merged = new Dictionary<string, List<List<string>>>(StringComparer.Ordinal);
    foreach (var (section, entries) in existing)
        merged[section] = new List<List<string>>(entries);

    var rank = sectionOrder.Select((name, index) => (name, index)).ToDictionary(t => t.name, t => t.index, StringComparer.Ordinal);

    var orderedFragments = fragments
        .OrderBy(f => rank.TryGetValue(f.SectionTitle, out var r) ? r : int.MaxValue)
        .ThenBy(f => f.Issue)
        .ThenBy(f => f.RelPath, StringComparer.Ordinal);

    foreach (var fragment in orderedFragments)
    {
        if (!merged.TryGetValue(fragment.SectionTitle, out var list))
        {
            list = new List<List<string>>();
            merged[fragment.SectionTitle] = list;
        }

        list.AddRange(fragment.Entries);
    }

    return merged;
}

static List<string> BuildSectionBlock(Dictionary<string, List<List<string>>> merged, string[] sectionOrder)
{
    var block = new List<string>();
    foreach (var section in sectionOrder)
    {
        if (!merged.TryGetValue(section, out var entries) || entries.Count == 0)
            continue;

        block.Add("");
        block.Add($"### {section}");
        block.Add("");
        foreach (var entry in entries)
            block.AddRange(entry);
    }

    if (block.Count > 0)
        block.Add("");

    return block;
}

internal sealed record Fragment(string Path, string RelPath, int Issue, string SectionTitle, List<List<string>> Entries);
