// knowledge-records.cs — SPEC-003 knowledge record validator and generator (batch 1, #1311).
//
// The record schema is SPEC-003 §3.1 front matter, amended by the pilot-1 report (not yet folded
// into the specification text): `outcome: rejected` is split into `rejected-reasoned` and
// `rejected-unexplained`, and a `linked_prs` field lists every PR number in the issue's evidence
// set (superset of `prs`, which is issue-worker/closing-PR driven). The schema lives in ONE place
// in this file (see RecordSchema below) so a later amendment is one edit.
//
// Schemas (#1735). The front-matter `schema` field selects the rule set; any other value is rejected.
//   schema 1  the strict shape above: block lists only (flow syntax like `[]` is rejected), every source
//             marked quote:/paraphrase: with a link and a date (REQ-002), a `done` destination whose target
//             exists, `current: yes` with a non-`none` destination (REQ-005), `audit` required.
//   schema 2  the audit pipeline's records (docs/knowledge/issues/1..10 as archived): the same required
//             fields, enums and block of audit keys, but flow lists (`packages: [A, B]`, `prs: []`) and
//             block scalars (`>-`) are accepted, `audit` may be absent (never audited), `audit.unit` and
//             `knowledge_unverified` are allowed, destination statuses are free text (`present`,
//             `planned (#1317)`), destination targets may be prose, and sources need no link/date marker.
//             Schema 2 checks structure and enums only; REQ-002/REQ-005 and `done`-target existence are
//             not applied to it. Unifying both on one schema is future work (see #1735).
// Both schemas share the audit-result rule: `audit.record` must name an existing file (relative to the
// repository root) unless `audit.verdict` is `not-audited`, in which case it names no file ("not written yet").
//
// Usage:
//   dotnet run .github/scripts/knowledge-records.cs -- --check [--dir <records-dir>] [--audits-dir <dir>] [--skip-audit-links]
//   dotnet run .github/scripts/knowledge-records.cs -- --generate --dir <records-dir> --out <output-dir>
//
// --check     validates every docs/knowledge/issues/<n>.md (or every *.md under --dir) against the
//             record schema: required front-matter fields, enum values, that a `done` destination's
//             target file exists (relative to the repository root of --dir's checkout), that every
//             knowledge item with current: yes has a non-`none` destination (REQ-005), and that
//             every knowledge item carries at least one source (REQ-002). It also checks the audit
//             results next to the records (--audits-dir, default <dir>/../audits): every issue-<n>.md and
//             every <n>/stages folder needs the record <n>.md. --skip-audit-links turns off the
//             audit.record existence check and the audits folder check (audit-done.ps1 uses it before the
//             result is in docs/knowledge). Exit 1 on any violation, with one line per error naming the
//             file and the field.
// --generate  reads every record under --dir and writes, under --out: an `index.md` grouped by
//             area (REQ-015) and a `PROJECT-HISTORY.md` grouped by knowledge kind within area, both
//             stamped as generated (REQ-016). Never writes to docs/engineering/ directly — --out is
//             mandatory so a batch script always names its destination explicitly.
//
// Requires: .NET 10+ (C# 14 file-based app)
#pragma warning disable CA1305, CA1310, CA1859, CA1852

using System.Globalization;
using System.Text;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var mode = args.FirstOrDefault(a => a is "--check" or "--generate");
if (mode is null)
{
    Console.Error.WriteLine("Usage: knowledge-records.cs -- --check [--dir <records-dir>] | --generate --dir <dir> --out <dir>");
    return 2;
}

string? GetOpt(string name) {
    var idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

var dir = GetOpt("--dir") ?? Path.Combine("docs", "knowledge", "issues");
var repoRoot = GetOpt("--repo-root") ?? FindRepoRoot(dir);
var skipAuditLinks = args.Contains("--skip-audit-links");
// The audit results sit next to the records directory: docs/knowledge/issues -> docs/knowledge/audits.
var auditsDir = GetOpt("--audits-dir") ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dir)) ?? ".", "audits");

if (!Directory.Exists(dir))
{
    if (mode == "--check")
    {
        Console.WriteLine($"No records directory at '{dir}' — nothing to check.");
        return 0;
    }
    Console.Error.WriteLine($"Records directory '{dir}' does not exist.");
    return 1;
}

var files = Directory.GetFiles(dir, "*.md", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.Ordinal).ToArray();

if (mode == "--check")
{
    var errors = new List<string>();
    foreach (var file in files)
    {
        errors.AddRange(ValidateRecord(file, repoRoot, skipAuditLinks));
    }

    var auditFiles = 0;
    if (!skipAuditLinks && Directory.Exists(auditsDir))
    {
        var (auditErrors, checkedFiles) = ValidateAudits(auditsDir, dir);
        errors.AddRange(auditErrors);
        auditFiles = checkedFiles;
    }

    if (errors.Count > 0)
    {
        foreach (var e in errors) Console.Error.WriteLine(e);
        Console.Error.WriteLine($"knowledge-records --check: {errors.Count} error(s) across {files.Length + auditFiles} file(s).");
        return 1;
    }

    Console.WriteLine($"knowledge-records --check: {files.Length} record(s) and {auditFiles} audit file(s) OK.");
    return 0;
}

// --generate
var outDir = GetOpt("--out");
if (string.IsNullOrWhiteSpace(outDir))
{
    Console.Error.WriteLine("--generate requires --out <dir> (never docs/engineering/PROJECT-HISTORY.md directly).");
    return 2;
}
Directory.CreateDirectory(outDir);

var records = new List<Dictionary<string, object?>>();
var skipped = 0;
foreach (var file in files)
{
    var (front, _, parseErrors) = ParseRecordFile(file);
    if (parseErrors.Count > 0)
    {
        // --check is the gate for malformed records; --generate skips them, but silently is not
        // acceptable — a skipped record makes index.md and PROJECT-HISTORY.md incomplete.
        skipped++;
        foreach (var pe in parseErrors) Console.Error.WriteLine($"{Path.GetFileName(file)}: {pe} (skipped)");
        continue;
    }
    records.Add(front!);
}

WriteIndex(Path.Combine(outDir, "index.md"), records, files);
WriteProjectHistory(Path.Combine(outDir, "PROJECT-HISTORY.md"), records, files);
Console.WriteLine($"knowledge-records --generate: {records.Count} record(s), {skipped} skipped -> {outDir}");
return skipped > 0 ? 1 : 0;

// ---------------------------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------------------------

static List<string> ValidateRecord(string file, string repoRoot, bool skipAuditLinks)
{
    var errors = new List<string>();
    var name = Path.GetFileName(file);
    var (front, _, parseErrors) = ParseRecordFile(file);
    foreach (var pe in parseErrors) errors.Add($"{name}: {pe}");
    if (front is null) return errors;

    void Err(string msg) => errors.Add($"{name}: {msg}");

    // The schema version selects the rule set; a missing or unknown version gets ONE error and no
    // further checks, because the other rules cannot be applied to a shape nobody defined.
    var schemaText = AsScalar(front.GetValueOrDefault("schema"));
    if (string.IsNullOrEmpty(schemaText))
    {
        Err("missing required field 'schema'");
        return errors;
    }
    if (!int.TryParse(schemaText, out var schemaVersion) || !RecordSchema.SupportedVersions.Contains(schemaVersion))
    {
        Err($"'schema' must be one of {string.Join(", ", RecordSchema.SupportedVersions)} (found '{schemaText}')");
        return errors;
    }
    var isV2 = schemaVersion == 2;

    // Fields already reported as missing by the loops below are not re-checked for format below:
    // a record missing 'schema' entirely should get ONE error ("missing required field"), not two
    // ("missing required field" followed by "must be 1 (found 'None')" for the same absence).
    var missingScalarFields = new HashSet<string>(StringComparer.Ordinal);
    foreach (var field in RecordSchema.RequiredScalarFields)
    {
        if (field == "schema") continue; // validated above
        if (!front.TryGetValue(field, out var fieldVal) || fieldVal is not string { Length: > 0 })
        {
            // A key with no inline value and no indented block (e.g. `closed:` alone) parses as an
            // empty List<object?> (see ParseMap), not null — without this shape check that silently
            // passes as "present". Only null or an empty/missing string count as genuinely absent.
            Err(fieldVal is null or string
                ? $"missing required field '{field}'"
                : $"'{field}' must be a scalar value");
            missingScalarFields.Add(field);
        }
    }
    foreach (var field in RecordSchema.RequiredListFields)
    {
        if (!front.TryGetValue(field, out var listVal))
            Err($"missing required field '{field}'");
        else if (listVal is not List<object?> || (!isV2 && listVal is FlowList))
            Err($"'{field}' must be a block list (flow syntax like '[]' is only accepted by schema 2; use an empty key)");
    }

    if (front.TryGetValue("nav_exclude", out var nx) && AsScalar(nx) != "true")
        Err("'nav_exclude' must be true");

    if (!missingScalarFields.Contains("issue") && !int.TryParse(AsScalar(front.GetValueOrDefault("issue")), out _))
        Err("'issue' must be an integer");

    if (!missingScalarFields.Contains("state_reason")) CheckEnum(front, "state_reason", RecordSchema.StateReasons, Err);
    if (!missingScalarFields.Contains("type")) CheckEnum(front, "type", RecordSchema.Types, Err);
    if (!missingScalarFields.Contains("area")) CheckEnum(front, "area", RecordSchema.Areas, Err);
    if (!missingScalarFields.Contains("review")) CheckEnum(front, "review", RecordSchema.ReviewValues, Err);

    string? outcome = AsScalar(front.GetValueOrDefault("outcome"));
    if (!missingScalarFields.Contains("outcome") && (outcome is null || !RecordSchema.Outcomes.Contains(outcome)))
        Err($"'outcome' has an unknown value '{outcome}' (expected one of: {string.Join(", ", RecordSchema.Outcomes)})");
    else if (outcome is not null && RecordSchema.OutcomesRequiringLink.Contains(outcome))
    {
        var linkField = outcome == "duplicate" ? "duplicate_of" : "superseded_by";
        if (!front.TryGetValue(linkField, out var linkVal) || string.IsNullOrWhiteSpace(AsScalar(linkVal)))
            Err($"outcome '{outcome}' requires '{linkField}'");
    }

    // audit: a nested map. Required by schema 1; schema 2 records that were never audited omit it.
    if (!front.TryGetValue("audit", out var auditObj) || auditObj is not Dictionary<string, object?> audit)
    {
        if (!isV2 || front.ContainsKey("audit")) Err("missing required field 'audit'");
    }
    else
    {
        // SPEC-003 §3.1: the audit unit is the issue itself (§15.4, DECIDED), so no separate
        // 'unit' field is required (schema 2 allows it); the map is 'checklist', 'date', 'verdict', 'record'.
        if (!audit.ContainsKey("checklist")) Err("'audit.checklist' is required");
        if (!audit.ContainsKey("date")) Err("'audit.date' is required");
        CheckEnumIn(audit, "verdict", RecordSchema.AuditVerdicts, msg => Err($"audit.{msg}"));
        CheckAuditRecordPath(audit, repoRoot, skipAuditLinks, Err);
    }

    // knowledge: list of maps.
    if (front.TryGetValue("knowledge", out var knowledgeObj) && knowledgeObj is List<object?> knowledgeList)
    {
        if (isV2) ValidateKnowledgeV2(knowledgeList, Err);
        else ValidateKnowledgeV1(knowledgeList, repoRoot, Err);
    }

    return errors;
}

// audit.record names the audit result file (REQ-011). An unaudited record (verdict not-audited) names no
// result file; every other verdict needs one, and a named file must exist (#1379, #1735). skipLinks is for the
// pre-publication check inside the audit worktree, where the result is not in docs/knowledge yet.
static void CheckAuditRecordPath(Dictionary<string, object?> audit, string repoRoot, bool skipLinks, Action<string> err)
{
    var verdict = AsScalar(audit.GetValueOrDefault("verdict"));
    var record = AsScalar(audit.GetValueOrDefault("record"));
    // A value without whitespace ending in .md names a file; anything else ("not written yet") names none.
    var namesFile = !string.IsNullOrWhiteSpace(record) && System.Text.RegularExpressions.Regex.IsMatch(record, @"^\S+\.md$");
    if (!namesFile)
    {
        if (verdict != "not-audited")
            err("'audit.record' must name the audit result file (docs/knowledge/audits/issue-<n>.md, REQ-011) unless audit.verdict is 'not-audited'");
        return;
    }
    if (skipLinks) return;
    var full = Path.IsPathRooted(record!) ? record! : Path.Combine(repoRoot, record!);
    if (!File.Exists(full))
        err($"'audit.record' points to '{record}', which does not exist (an unaudited record uses verdict 'not-audited' and names no result file)");
}

// Checks of docs/knowledge/audits next to the records: every issue-<n>.md result and every <n>/ stage folder
// needs the record issues/<n>.md; nothing else belongs there.
static (List<string> Errors, int Checked) ValidateAudits(string auditsDir, string recordsDir)
{
    var errors = new List<string>();
    var checkedFiles = 0;
    foreach (var f in Directory.GetFiles(auditsDir, "*", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.Ordinal))
    {
        var name = Path.GetFileName(f);
        var m = System.Text.RegularExpressions.Regex.Match(name, @"^issue-(\d+)\.md$");
        checkedFiles++;
        if (!m.Success) { errors.Add($"audits/{name}: unexpected file (expected issue-<n>.md or an <n>/ stage folder)"); continue; }
        if (!File.Exists(Path.Combine(recordsDir, $"{m.Groups[1].Value}.md")))
            errors.Add($"audits/{name}: no matching record issues/{m.Groups[1].Value}.md");
    }
    foreach (var d in Directory.GetDirectories(auditsDir).OrderBy(d => d, StringComparer.Ordinal))
    {
        var name = Path.GetFileName(d);
        checkedFiles++;
        if (!name.All(char.IsAsciiDigit)) { errors.Add($"audits/{name}/: unexpected folder (expected <n>/stages)"); continue; }
        if (!File.Exists(Path.Combine(recordsDir, $"{name}.md")))
            errors.Add($"audits/{name}/: no matching record issues/{name}.md");
        var stages = Path.Combine(d, "stages");
        if (!Directory.Exists(stages)) errors.Add($"audits/{name}/: missing stages folder");
    }
    return (errors, checkedFiles);
}

// Schema 2 knowledge items (the audit pipeline's records): structure and enums only. The archived records
// carry prose destination targets, sources without a link or marker, statuses such as 'present' or
// 'planned (#1317)', and 'current: yes' with only a 'none' destination, so the schema 1 content rules
// (REQ-002 source markers, REQ-005, done-target existence) are not applied.
static void ValidateKnowledgeV2(List<object?> knowledgeList, Action<string> err)
{
    for (var i = 0; i < knowledgeList.Count; i++)
    {
        if (knowledgeList[i] is not Dictionary<string, object?> item) { err($"'knowledge[{i}]' must be a map"); continue; }
        var where = $"knowledge[{i}]";
        CheckEnumIn(item, "kind", RecordSchema.KnowledgeKinds, msg => err($"{where}.{msg}"));
        CheckEnumIn(item, "current", RecordSchema.CurrentValues, msg => err($"{where}.{msg}"));
        if (string.IsNullOrWhiteSpace(AsScalar(item.GetValueOrDefault("statement")))) err($"{where}.statement is required");
        var sources = item.GetValueOrDefault("sources") as List<object?>;
        if (sources is null || sources.Count == 0 || sources.Any(s => string.IsNullOrWhiteSpace(AsScalar(s))))
            err($"{where}.sources must be a non-empty list of non-empty strings");
        var destinations = item.GetValueOrDefault("destinations") as List<object?> ?? [];
        for (var j = 0; j < destinations.Count; j++)
        {
            if (destinations[j] is not Dictionary<string, object?> dest) { err($"{where}.destinations[{j}] must be a map"); continue; }
            var dwhere = $"{where}.destinations[{j}]";
            CheckEnumIn(dest, "kind", RecordSchema.DestinationKinds, msg => err($"{dwhere}.{msg}"));
            if (string.IsNullOrWhiteSpace(AsScalar(dest.GetValueOrDefault("status")))) err($"{dwhere}.status is required");
            if (AsScalar(dest.GetValueOrDefault("kind")) != "none" && string.IsNullOrWhiteSpace(AsScalar(dest.GetValueOrDefault("target"))))
                err($"{dwhere}.target is required unless the kind is 'none'");
        }
    }
}

static void ValidateKnowledgeV1(List<object?> knowledgeList, string repoRoot, Action<string> Err)
{
    {
        for (var i = 0; i < knowledgeList.Count; i++)
        {
            if (knowledgeList[i] is not Dictionary<string, object?> item)
            {
                Err($"'knowledge[{i}]' must be a map");
                continue;
            }
            var where = $"knowledge[{i}]";
            CheckEnumIn(item, "kind", RecordSchema.KnowledgeKinds, msg => Err($"{where}.{msg}"));
            CheckEnumIn(item, "current", RecordSchema.CurrentValues, msg => Err($"{where}.{msg}"));
            if (!item.TryGetValue("statement", out var st) || string.IsNullOrWhiteSpace(AsScalar(st)))
                Err($"{where}.statement is required");

            var sources = item.TryGetValue("sources", out var srcObj) ? srcObj as List<object?> : null;
            if (sources is null || sources.Count == 0)
            {
                Err($"{where}.sources must have at least one entry (REQ-002)");
            }
            else
            {
                foreach (var s in sources)
                {
                    if (s is Dictionary<string, object?> unquotedMap)
                    {
                        // An unquoted list item like `- quote: some text with: a colon` parses as a
                        // one-key map (ParseSeq's looksLikeMapEntry), not the plain string the schema
                        // expects. Name the fix instead of failing the marker check with an empty string.
                        var mapKey = unquotedMap.Keys.FirstOrDefault() ?? "?";
                        Err($"{where}.sources entry must be a quoted string, not an unquoted '{mapKey}:' value — wrap the whole source in double quotes (REQ-002)");
                        continue;
                    }

                    var text = AsScalar(s) ?? "";
                    var hasMarker = text.Contains("quote:", StringComparison.Ordinal) || text.Contains("paraphrase:", StringComparison.Ordinal);
                    if (!hasMarker)
                    {
                        Err($"{where}.sources entry must be marked 'quote:' or 'paraphrase:' (REQ-002): '{text}'");
                        continue;
                    }

                    var hasLink = text.Contains("http://", StringComparison.OrdinalIgnoreCase)
                        || text.Contains("https://", StringComparison.OrdinalIgnoreCase)
                        || System.Text.RegularExpressions.Regex.IsMatch(text, @"#\d+");
                    if (!hasLink)
                        Err($"{where}.sources entry must include a link (URL or #issue/#pr) (REQ-002): '{text}'");

                    var hasDate = System.Text.RegularExpressions.Regex.IsMatch(text, @"\d{4}-\d{2}-\d{2}");
                    if (!hasDate)
                        Err($"{where}.sources entry must include the source's date (yyyy-MM-dd) (REQ-002): '{text}'");
                }
            }

            var destinations = item.TryGetValue("destinations", out var destObj) ? destObj as List<object?> : new List<object?>();
            var current = AsScalar(item.GetValueOrDefault("current"));
            var hasNonNoneDestination = false;
            for (var j = 0; j < (destinations?.Count ?? 0); j++)
            {
                if (destinations![j] is not Dictionary<string, object?> dest)
                {
                    Err($"{where}.destinations[{j}] must be a map");
                    continue;
                }
                var dwhere = $"{where}.destinations[{j}]";
                CheckEnumIn(dest, "kind", RecordSchema.DestinationKinds, msg => Err($"{dwhere}.{msg}"));
                CheckEnumIn(dest, "status", RecordSchema.DestinationStatuses, msg => Err($"{dwhere}.{msg}"));
                var dkind = AsScalar(dest.GetValueOrDefault("kind"));
                if (dkind != "none" && dkind is not null) hasNonNoneDestination = true;

                var status = AsScalar(dest.GetValueOrDefault("status"));
                var target = AsScalar(dest.GetValueOrDefault("target")) ?? "";
                if (status == "done" && dkind != "none")
                {
                    if (!DestinationTargetExists(target, repoRoot))
                        Err($"{dwhere}.target '{target}' does not exist (status: done)");
                }
                else if (status == "planned")
                {
                    if (string.IsNullOrWhiteSpace(target))
                        Err($"{dwhere}.target is required when status is 'planned' (names the batch or issue)");
                }
            }

            if (current == "yes" && !hasNonNoneDestination)
                Err($"{where}: current: yes requires at least one destination other than 'none' (REQ-005)");
        }
    }
}

static void CheckEnum(Dictionary<string, object?> map, string field, string[] allowed, Action<string> err)
    => CheckEnumIn(map, field, allowed, err);

static void CheckEnumIn(Dictionary<string, object?> map, string field, string[] allowed, Action<string> err)
{
    var value = AsScalar(map.GetValueOrDefault(field));
    if (value is null || !allowed.Contains(value))
        err($"'{field}' has an unknown value '{value}' (expected one of: {string.Join(", ", allowed)})");
}

static bool DestinationTargetExists(string target, string repoRoot)
{
    if (string.IsNullOrWhiteSpace(target)) return false;
    // An issue reference (#123 or 123) is not checked over the network here; format is enough.
    var stripped = target.TrimStart('#');
    var hashIdx = stripped.IndexOf('#');
    var pathPart = hashIdx >= 0 ? stripped[..hashIdx] : stripped;
    if (int.TryParse(target.TrimStart('#'), out _)) return true;
    if (string.IsNullOrWhiteSpace(pathPart)) return false;
    var full = Path.IsPathRooted(pathPart) ? pathPart : Path.Combine(repoRoot, pathPart);
    return File.Exists(full) || Directory.Exists(full);
}

static string FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(Path.GetFullPath(startDir));
    while (dir is not null)
    {
        // In a git worktree or submodule, .git is a file (pointing at the real gitdir), not a
        // directory — Directory.Exists alone misses it and the search climbs past the worktree
        // into whatever ancestor directory happens to contain a real .git, silently checking
        // 'done' destination targets against the wrong tree.
        var gitPath = Path.Combine(dir.FullName, ".git");
        if (Directory.Exists(gitPath) || File.Exists(gitPath)) return dir.FullName;
        dir = dir.Parent;
    }
    return Directory.GetCurrentDirectory();
}

// ---------------------------------------------------------------------------------------------
// Front matter parsing: a minimal block-YAML subset (2-space indent), sufficient for the fixed
// record schema above. Every value is kept as a string, List<object?> or Dictionary<string,object?>.
// ---------------------------------------------------------------------------------------------

static (Dictionary<string, object?>? Front, string Body, List<string> Errors) ParseRecordFile(string file)
{
    var errors = new List<string>();
    var text = File.ReadAllText(file);
    var lines = text.Replace("\r\n", "\n").Split('\n');
    if (lines.Length == 0 || lines[0].Trim() != "---")
    {
        errors.Add("missing '---' front matter opening delimiter");
        return (null, text, errors);
    }
    var end = -1;
    for (var i = 1; i < lines.Length; i++)
    {
        if (lines[i].Trim() == "---") { end = i; break; }
    }
    if (end < 0)
    {
        errors.Add("missing '---' front matter closing delimiter");
        return (null, text, errors);
    }

    var yamlLines = new List<(int Indent, string Content)>();
    for (var i = 1; i < end; i++)
    {
        var raw = lines[i];
        if (string.IsNullOrWhiteSpace(raw)) continue;
        var trimmedStart = raw.TrimStart(' ');
        var spaces = raw.Length - trimmedStart.Length;
        if (spaces % 2 != 0)
        {
            errors.Add($"front matter line {i + 1}: odd indentation ({spaces} spaces) is not supported");
            continue;
        }
        yamlLines.Add((spaces / 2, trimmedStart.TrimEnd()));
    }

    if (errors.Count > 0) return (null, text, errors);

    var pos = 0;
    var front = ParseMap(yamlLines, ref pos, 0);
    if (pos < yamlLines.Count)
        errors.Add($"front matter: unexpected content at indentation level {yamlLines[pos].Indent} ('{yamlLines[pos].Content}')");

    var body = string.Join('\n', lines.Skip(end + 1));
    return (errors.Count > 0 ? null : front, body, errors);
}

static string? AsScalar(object? value) => value as string;

static Dictionary<string, object?> ParseMap(List<(int Indent, string Content)> lines, ref int pos, int indent)
{
    var map = new Dictionary<string, object?>(StringComparer.Ordinal);
    while (pos < lines.Count && lines[pos].Indent == indent && !lines[pos].Content.StartsWith("- ", StringComparison.Ordinal))
    {
        var (_, content) = lines[pos];
        pos++;
        var colon = content.IndexOf(':');
        if (colon < 0) continue;
        var key = content[..colon].Trim();
        var val = content[(colon + 1)..].Trim();
        if (val.Length > 0)
        {
            map[key] = ParseValue(val, lines, ref pos, indent);
        }
        else if (pos < lines.Count && lines[pos].Indent > indent)
        {
            map[key] = lines[pos].Content.StartsWith("- ", StringComparison.Ordinal)
                ? ParseSeq(lines, ref pos, lines[pos].Indent)
                : ParseMap(lines, ref pos, lines[pos].Indent);
        }
        else
        {
            map[key] = new List<object?>();
        }
    }
    return map;
}

static List<object?> ParseSeq(List<(int Indent, string Content)> lines, ref int pos, int indent)
{
    var list = new List<object?>();
    while (pos < lines.Count && lines[pos].Indent == indent && lines[pos].Content.StartsWith("- ", StringComparison.Ordinal))
    {
        var content = lines[pos].Content[2..].Trim();
        pos++;
        var isQuoted = content.Length > 0 && content[0] == '"';
        var colon = content.IndexOf(':');
        // A colon that only appears inside a quoted scalar (e.g. a source URL) does not make this a
        // "key: value" map entry; only an unquoted item with a top-level ": " separator does.
        var looksLikeMapEntry = !isQuoted && colon > 0 && content.Length > colon + 1 && content[colon + 1] == ' ';
        if (content.Length == 0)
        {
            list.Add(pos < lines.Count && lines[pos].Indent > indent
                ? (lines[pos].Content.StartsWith("- ", StringComparison.Ordinal) ? ParseSeq(lines, ref pos, lines[pos].Indent) : ParseMap(lines, ref pos, lines[pos].Indent))
                : null);
        }
        else if (looksLikeMapEntry)
        {
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);
            var key = content[..colon].Trim();
            var val = content[(colon + 1)..].Trim();
            map[key] = val.Length > 0
                ? ParseValue(val, lines, ref pos, indent + 1)
                : (pos < lines.Count && lines[pos].Indent > indent
                    ? (lines[pos].Content.StartsWith("- ", StringComparison.Ordinal) ? ParseSeq(lines, ref pos, lines[pos].Indent) : ParseMap(lines, ref pos, lines[pos].Indent))
                    : new List<object?>());
            while (pos < lines.Count && lines[pos].Indent == indent + 1 && !lines[pos].Content.StartsWith("- ", StringComparison.Ordinal))
            {
                var line = lines[pos].Content;
                pos++;
                var c2 = line.IndexOf(':');
                if (c2 < 0) continue;
                var k2 = line[..c2].Trim();
                var v2 = line[(c2 + 1)..].Trim();
                map[k2] = v2.Length > 0
                    ? ParseValue(v2, lines, ref pos, indent + 1)
                    : (pos < lines.Count && lines[pos].Indent > indent + 1
                        ? (lines[pos].Content.StartsWith("- ", StringComparison.Ordinal) ? ParseSeq(lines, ref pos, lines[pos].Indent) : ParseMap(lines, ref pos, lines[pos].Indent))
                        : new List<object?>());
            }
            list.Add(map);
        }
        else
        {
            list.Add(Unquote(content));
        }
    }
    return list;
}

// A flow list ([a, b] or []) is parsed into FlowList so schema 1 can still reject it while schema 2
// accepts it; a block scalar (>-, |, ...) is folded into one string. Anything else is an unquoted scalar.
static object? ParseValue(string val, List<(int Indent, string Content)> lines, ref int pos, int keyIndent)
{
    if (val is ">" or ">-" or ">+" or "|" or "|-" or "|+")
    {
        var literal = val[0] == '|';
        var parts = new List<string>();
        while (pos < lines.Count && lines[pos].Indent > keyIndent)
        {
            parts.Add(lines[pos].Content);
            pos++;
        }
        return string.Join(literal ? '\n' : ' ', parts);
    }
    if (val.Length >= 2 && val[0] == '[' && val[^1] == ']')
    {
        var list = new FlowList();
        foreach (var part in SplitFlow(val[1..^1])) list.Add(Unquote(part));
        return list;
    }
    return Unquote(val);
}

// Splits "a, "b, c", d" on the commas outside double quotes; an empty inner text is an empty list.
static List<string> SplitFlow(string inner)
{
    var parts = new List<string>();
    var sb = new StringBuilder();
    var inQuotes = false;
    foreach (var ch in inner)
    {
        if (ch == '"') inQuotes = !inQuotes;
        if (ch == ',' && !inQuotes) { parts.Add(sb.ToString().Trim()); sb.Clear(); continue; }
        sb.Append(ch);
    }
    var last = sb.ToString().Trim();
    if (last.Length > 0 || parts.Count > 0) parts.Add(last);
    return parts.Where(p => p.Length > 0).ToList();
}

static string Unquote(string s)
{
    if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
        return s[1..^1].Replace("\\\"", "\"", StringComparison.Ordinal);
    return s;
}

// ---------------------------------------------------------------------------------------------
// Generation (REQ-015, REQ-016): deterministic, from the records alone, stamped as generated.
// ---------------------------------------------------------------------------------------------

// AppendLine() writes Environment.NewLine, which is \r\n on Windows and \n on Linux CI — the same
// records would then generate byte-different files depending on which OS ran --generate (SPEC-003
// AC-004 requires byte-identical output). Ln() always appends '\n' regardless of platform.
static void Ln(StringBuilder sb, string s = "") => sb.Append(s).Append('\n');

static void WriteIndex(string path, List<Dictionary<string, object?>> records, string[] files)
{
    var sb = new StringBuilder();
    Ln(sb, "<!-- GENERATED FILE — do not edit by hand.");
    Ln(sb, "     Produced by .github/scripts/knowledge-records.cs --generate from docs/knowledge/issues/*.md (SPEC-003). -->");
    Ln(sb);
    Ln(sb, "# Knowledge record index");
    Ln(sb);
    foreach (var area in RecordSchema.Areas)
    {
        var inArea = records.Where(r => AsScalar(r.GetValueOrDefault("area")) == area).ToList();
        if (inArea.Count == 0) continue;
        Ln(sb, $"## {area}");
        Ln(sb);
        foreach (var r in inArea.OrderBy(r => int.TryParse(AsScalar(r.GetValueOrDefault("issue")), out var n) ? n : 0))
        {
            var issue = AsScalar(r.GetValueOrDefault("issue"));
            var title = AsScalar(r.GetValueOrDefault("title"));
            var outcome = AsScalar(r.GetValueOrDefault("outcome"));
            Ln(sb, $"- [#{issue}]({issue}.md) {title} — {outcome}");
        }
        Ln(sb);
    }
    File.WriteAllText(path, sb.ToString());
}

static void WriteProjectHistory(string path, List<Dictionary<string, object?>> records, string[] files)
{
    var sb = new StringBuilder();
    Ln(sb, "<!-- GENERATED FILE — do not edit by hand.");
    Ln(sb, "     Produced by .github/scripts/knowledge-records.cs --generate from docs/knowledge/issues/*.md (SPEC-003, REQ-015/REQ-016). -->");
    Ln(sb);
    Ln(sb, "# Project history");
    Ln(sb);

    foreach (var area in RecordSchema.Areas)
    {
        var inArea = records.Where(r => AsScalar(r.GetValueOrDefault("area")) == area).ToList();
        if (inArea.Count == 0) continue;
        Ln(sb, $"## {area}");
        Ln(sb);
        foreach (var kind in RecordSchema.KnowledgeKinds)
        {
            var items = new List<(int Issue, string Statement)>();
            foreach (var r in inArea)
            {
                if (r.GetValueOrDefault("knowledge") is not List<object?> knowledge) continue;
                var issue = int.TryParse(AsScalar(r.GetValueOrDefault("issue")), out var n) ? n : 0;
                foreach (var k in knowledge)
                {
                    if (k is not Dictionary<string, object?> item) continue;
                    if (AsScalar(item.GetValueOrDefault("kind")) != kind) continue;
                    var statement = AsScalar(item.GetValueOrDefault("statement")) ?? "";
                    items.Add((issue, statement));
                }
            }
            if (items.Count == 0) continue;
            Ln(sb, $"### {ToTitle(kind)}");
            Ln(sb);
            foreach (var (issue, statement) in items.OrderBy(i => i.Issue))
            {
                Ln(sb, $"- {statement} (#{issue})");
            }
            Ln(sb);
        }
    }
    File.WriteAllText(path, sb.ToString());
}

static string ToTitle(string kebab)
    => string.Join(' ', kebab.Split('-').Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));

// ---------------------------------------------------------------------------------------------
// Schema (RecordSchema): the single place an amendment to the front matter touches.
// ---------------------------------------------------------------------------------------------

sealed class FlowList : List<object?>;

static class RecordSchema
{
    // Schema 1: the strict record shape SPEC-003 §3.1 defines (block lists only, REQ-002 sources, REQ-005,
    // done-target existence). Schema 2: the audit pipeline's records (#1735), see the header of this file.
    public static readonly int[] SupportedVersions = [1, 2];

    // SPEC-003 §3.1 required scalar fields, plus the pilot-1 `linked_prs` amendment.
    public static readonly string[] RequiredScalarFields =
    [
        "schema", "nav_exclude", "issue", "title", "closed", "state_reason",
        "outcome", "type", "area", "review"
    ];

    // Required list fields (may be empty lists, but the key must be present).
    public static readonly string[] RequiredListFields =
    [
        "packages", "prs", "linked_prs", "knowledge", "remediation"
    ];

    public static readonly string[] StateReasons = ["completed", "not-planned", "duplicate"];

    // Pilot-1 amendment: `rejected` split into `rejected-reasoned` and `rejected-unexplained`.
    public static readonly string[] Outcomes =
    [
        "delivered", "partial", "rejected-reasoned", "rejected-unexplained",
        "superseded", "duplicate", "moved", "no-evidence"
    ];

    public static readonly string[] OutcomesRequiringLink = ["duplicate", "superseded"];

    public static readonly string[] Types = ["bug", "feature", "debt", "test", "spike", "infra", "refactor", "epic", "other"];

    public static readonly string[] Areas =
    [
        "core", "messaging", "data", "caching", "eventsourcing", "validation", "observability",
        "security-compliance", "testing-quality", "ci-process", "docs-dx", "web-cloud",
        "modules-tenancy", "resilience"
    ];

    public static readonly string[] KnowledgeKinds = ["decision", "rejected-alternative", "rule", "direction-change", "gotcha", "pending-work"];

    public static readonly string[] CurrentValues = ["yes", "no", "unknown"];

    public static readonly string[] DestinationKinds =
    [
        "executable-rule", "rule", "adr", "regression-test", "review-checklist", "benchmark",
        "quality-method", "docs", "backlog", "spec-invariant", "none"
    ];

    public static readonly string[] DestinationStatuses = ["done", "planned"];

    public static readonly string[] AuditVerdicts =
    [
        "conforms", "conforms-with-na", "findings-tracked", "findings-fixed", "code-removed", "not-audited"
    ];

    public static readonly string[] ReviewValues = ["draft", "verified", "sampled"];
}
