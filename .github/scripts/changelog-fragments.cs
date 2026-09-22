// changelog-fragments.cs — Changelog fragment tooling (see changelog.d/README.md).
//
// PRs developed in parallel all appended to "## [Unreleased]" in CHANGELOG.md and conflicted on
// every merge. Instead, a PR drops one file per changelog entry under changelog.d/; the entries
// are merged (this script) only at release time, so parallel PRs never touch the same line.
//
// File name:    changelog.d/<issue>-<short-slug>.<section>.md
// section is one of: added | changed | deprecated | removed | fixed | security (Keep a Changelog)
// Content:      one or more markdown bullets, each starting with "- ".
//
// Usage:
//   dotnet run .github/scripts/changelog-fragments.cs -- --check [--fragments-dir <dir>]
//   dotnet run .github/scripts/changelog-fragments.cs -- --preview [--fragments-dir <dir>] [--changelog <file>]
//   dotnet run .github/scripts/changelog-fragments.cs -- --release <version> <yyyy-MM-dd> [title] \
//       [--fragments-dir <dir>] [--changelog <file>]
//
// --check     validates every fragment file name, section and bullet content; exit 1 on violation.
// --preview   prints the "## [Unreleased]" section as it would read: CHANGELOG.md's current
//             Unreleased entries merged with the fragments, grouped by section in Keep a Changelog
//             order. Read-only.
// --release   folds the merged Unreleased content into CHANGELOG.md as a new dated
//             "## [<version>] - <date>[ - <title>]" section (keeping an empty "## [Unreleased]"
//             above it), then deletes the fragment files that were folded in.
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
                Environment.Exit(1);
                return;
            }
            releaseVersion = args[++i];
            releaseDate = args[++i];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                releaseTitle = args[++i];
            break;
        case "--fragments-dir":
            fragmentsDir = args[++i];
            break;
        case "--changelog":
            changelogPath = args[++i];
            break;
        default:
            Console.Error.WriteLine($"ERROR: unrecognized argument '{args[i]}'");
            Environment.Exit(1);
            return;
    }
}

if (mode.Length == 0)
{
    Console.Error.WriteLine("Usage: changelog-fragments.cs (--check | --preview | --release <version> <yyyy-MM-dd> [title]) [--fragments-dir <dir>] [--changelog <file>]");
    Environment.Exit(1);
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

// Structural name check first (loose section group) so a bad-name error and an unknown-section
// error are reported as two distinct problems, not folded into one generic message.
var nameRegex = new Regex(@"^(?<issue>[0-9]+)-(?<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.(?<section>[a-z]+)\.md$", RegexOptions.Compiled);

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

var existingSections = ParseSections(lines, unreleasedStart, unreleasedEnd);
var merged = MergeSections(existingSections, fragments, sectionOrder);

const string PendingNote = "> Pending changelog entries are added as fragments under `changelog.d/` until release; see `changelog.d/README.md`.";

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
if (!DateOnly.TryParseExact(releaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
{
    Console.Error.WriteLine($"ERROR: --release date '{releaseDate}' is not in yyyy-MM-dd format");
    Environment.Exit(1);
    return;
}

var headerLine = releaseTitle is null
    ? $"## [{releaseVersion}] - {releaseDate}"
    : $"## [{releaseVersion}] - {releaseDate} - {releaseTitle}";

// BuildSectionBlock prefixes each section with its own blank line, so the header itself carries
// none — that keeps exactly one blank line between the header and the first "### " heading,
// matching the existing release sections in CHANGELOG.md.
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

    foreach (var path in Directory.GetFiles(dir, "*.md", SearchOption.TopDirectoryOnly).OrderBy(p => p, StringComparer.Ordinal))
    {
        var fileName = Path.GetFileName(path);
        if (string.Equals(fileName, "README.md", StringComparison.OrdinalIgnoreCase))
            continue;

        var match = nameRegex.Match(fileName);
        if (!match.Success)
        {
            errs.Add($"{fileName}: bad fragment file name — expected '<issue>-<short-slug>.<section>.md' (e.g. '1125-define-eventids.fixed.md')");
            continue;
        }

        var sectionKey = match.Groups["section"].Value;
        if (!sectionKeyToTitle.TryGetValue(sectionKey, out var sectionTitle))
        {
            errs.Add($"{fileName}: unknown section '{sectionKey}' — expected one of added, changed, deprecated, removed, fixed, security");
            continue;
        }

        var issueText = match.Groups["issue"].Value;
        if (!int.TryParse(issueText, NumberStyles.None, CultureInfo.InvariantCulture, out var issue) || issue <= 0)
        {
            errs.Add($"{fileName}: issue number must be greater than 0, got '{issueText}'");
            continue;
        }

        var contentLines = File.ReadAllLines(path);
        var bullets = new List<string>();
        var contentErrorFound = false;
        foreach (var rawLine in contentLines)
        {
            if (rawLine.Trim().Length == 0)
                continue;

            if (!rawLine.StartsWith("- ", StringComparison.Ordinal))
            {
                errs.Add($"{fileName}: line does not start with '- ': '{rawLine}'");
                contentErrorFound = true;
                continue;
            }

            bullets.Add(rawLine.TrimEnd());
        }

        if (contentErrorFound)
            continue;

        if (bullets.Count == 0)
        {
            errs.Add($"{fileName}: fragment has no bullet lines");
            continue;
        }

        found.Add(new Fragment(path, fileName, issue, sectionTitle, bullets));
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

static Dictionary<string, List<string>> ParseSections(List<string> lines, int start, int end)
{
    var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    string? current = null;
    for (var i = start + 1; i < end; i++)
    {
        var line = lines[i];
        if (line.StartsWith("### ", StringComparison.Ordinal))
        {
            current = line[4..].Trim();
            if (!result.ContainsKey(current))
                result[current] = new List<string>();
            continue;
        }

        if (current is not null && line.StartsWith("- ", StringComparison.Ordinal))
            result[current].Add(line);
    }

    return result;
}

static Dictionary<string, List<string>> MergeSections(Dictionary<string, List<string>> existing, List<Fragment> fragments, string[] sectionOrder)
{
    var merged = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var (section, bullets) in existing)
        merged[section] = new List<string>(bullets);

    var rank = sectionOrder.Select((name, index) => (name, index)).ToDictionary(t => t.name, t => t.index, StringComparer.Ordinal);

    var orderedFragments = fragments
        .OrderBy(f => rank.TryGetValue(f.SectionTitle, out var r) ? r : int.MaxValue)
        .ThenBy(f => f.Issue)
        .ThenBy(f => f.FileName, StringComparer.Ordinal);

    foreach (var fragment in orderedFragments)
    {
        if (!merged.TryGetValue(fragment.SectionTitle, out var list))
        {
            list = new List<string>();
            merged[fragment.SectionTitle] = list;
        }

        list.AddRange(fragment.Bullets);
    }

    return merged;
}

static List<string> BuildSectionBlock(Dictionary<string, List<string>> merged, string[] sectionOrder)
{
    var block = new List<string>();
    foreach (var section in sectionOrder)
    {
        if (!merged.TryGetValue(section, out var bullets) || bullets.Count == 0)
            continue;

        block.Add("");
        block.Add($"### {section}");
        block.Add("");
        block.AddRange(bullets);
    }

    if (block.Count > 0)
        block.Add("");

    return block;
}

internal sealed record Fragment(string Path, string FileName, int Issue, string SectionTitle, List<string> Bullets);
