using System.Globalization;

using Encina.Diagnostics;
using Encina.Testing.Architecture;

using Microsoft.Extensions.Logging;

using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Keeps the tracked block registry <c>docs/engineering/eventid-block-registry.md</c> in agreement with
/// <c>src/Encina/Diagnostics/EventIdRanges.cs</c> (issue #4006, ADR-021 "The 10000+ zone").
/// </summary>
/// <remarks>
/// <para>
/// The registry has two parseable tables. The "registered ranges" table (header
/// <c>Name | Block | Area | Zone</c>) lists every field of <see cref="EventIdRanges"/>; its <c>Zone</c> is
/// <c>historical</c> for a range below 10000 and <c>zone</c> for one in the 10000+ zone. The "blocks" table
/// (header <c>Block | Area | Owner | Status</c>) lists every block a plan reserved; its <c>Status</c> is one of
/// <c>registered</c>, <c>reserved</c>, <c>reserved-pre-zone</c>, <c>superseded</c> and <c>free</c>.
/// </para>
/// <para>
/// The five agreement rules are enforced by <see cref="EventIdBlockRegistry.Check"/>: (1) every range of
/// <c>EventIdRanges.cs</c> is in the registry with the same bounds; (2) every registry row that claims to be
/// registered matches a range of <c>EventIdRanges.cs</c> with the same bounds; (3) a reserved block overlaps no
/// registered range; (4) reserved blocks overlap each other nowhere; (5) nothing lies below 10000 except the
/// historical registered ranges (<c>Zone = historical</c>) and the reserved blocks recorded before the zone
/// existed (<c>Status = reserved-pre-zone</c>). A plain <c>reserved</c> block below 10000 is a violation.
/// </para>
/// </remarks>
public sealed class EventIdRegistryAgreementTests
{
    [Fact]
    public void Registry_AgreesWithEventIdRanges()
    {
        var registry = File.ReadAllText(EventIdBlockRegistry.FindRegistryPath());
        var violations = EventIdBlockRegistry.Check(registry, CurrentRanges());

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Check_RegistryWithRangeMissing_NamesTheRange()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199), ("Marten", 2600, 2799)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("Marten");
        violation.ShouldContain("2600-2799");
    }

    [Fact]
    public void Check_RegisteredRowWithDifferentBounds_NamesTheRow()
    {
        var registry = Registry(
            registered: ["| Core | 100-198 | Core and messaging | historical |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        violations.ShouldContain(v => v.Contains("'Core'", StringComparison.Ordinal) && v.Contains("100-198", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_RegisteredRowWithoutRangeInCode_NamesTheRow()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |", "| Ghost | 300-349 | Core and messaging | historical |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("Ghost");
    }

    [Fact]
    public void Check_BlockRowMarkedRegisteredWithoutMatchingRange_NamesTheRow()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"],
            blocks: ["| 10000-10049 | Core and messaging | plan #1 | registered |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("10000-10049");
    }

    [Fact]
    public void Check_ReservedBlockOverlappingRegisteredRange_NamesBoth()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"],
            blocks: ["| 150-170 | Core and messaging | plan #1 | reserved-pre-zone |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("150-170");
        violation.ShouldContain("'Core'");
    }

    [Fact]
    public void Check_ReservedBlocksOverlappingEachOther_NamesBoth()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"],
            blocks:
            [
                "| 10000-10049 | Core and messaging | plan #1 | reserved |",
                "| 10040-10099 | Core and messaging | plan #2 | reserved |",
            ]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("10000-10049");
        violation.ShouldContain("10040-10099");
    }

    [Fact]
    public void Check_ReservedBlockBelowZoneWithoutPreZoneMarker_IsRejected()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"],
            blocks: ["| 5500-5549 | Core and messaging | plan #1 | reserved |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("5500-5549");
        violation.ShouldContain("reserved-pre-zone");
    }

    [Fact]
    public void Check_RegisteredRangeBelowZoneNotMarkedHistorical_IsRejected()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | zone |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        var violation = violations.ShouldHaveSingleItem();
        violation.ShouldContain("'Core'");
        violation.ShouldContain("historical");
    }

    [Fact]
    public void Check_PreZoneMarkerInsideTheZone_IsRejected()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"],
            blocks: ["| 10000-10049 | Core and messaging | plan #1 | reserved-pre-zone |"]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        violations.ShouldHaveSingleItem().ShouldContain("10000-10049");
    }

    [Fact]
    public void Check_RegistryInsideTheZone_AcceptsIdsAbove9999()
    {
        var registry = Registry(
            registered:
            [
                "| Core | 100-199 | Core and messaging | historical |",
                "| MessagingOutbox | 10000-10049 | Core and messaging | zone |",
            ],
            blocks:
            [
                "| 10000-10049 | Core and messaging | plan #1 | registered |",
                "| 10050-10099 | Core and messaging | plan #2 | reserved |",
                "| 20000-20049 | Database | plan #3 | reserved |",
            ]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199), ("MessagingOutbox", 10000, 10049)]);

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Check_RegistryWithoutTheTables_ReportsBoth()
    {
        var violations = EventIdBlockRegistry.Check("# Empty page", [("Core", 100, 199)]);

        violations.ShouldContain(v => v.Contains("registered ranges", StringComparison.Ordinal));
        violations.ShouldContain(v => v.Contains("blocks", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_UnknownStatusOrMalformedBlock_IsRejected()
    {
        var registry = Registry(
            registered: ["| Core | 100-199 | Core and messaging | historical |"],
            blocks:
            [
                "| 10000-10049 | Core and messaging | plan #1 | maybe |",
                "| 10100-10050 | Core and messaging | plan #2 | reserved |",
            ]);

        var violations = EventIdBlockRegistry.Check(registry, [("Core", 100, 199)]);

        violations.ShouldContain(v => v.Contains("maybe", StringComparison.Ordinal));
        violations.ShouldContain(v => v.Contains("10100-10050", StringComparison.Ordinal));
    }

    [Fact]
    public void EventIdAbove9999_IsExtractedAndCheckedByTheUniquenessRule()
    {
        // ADR-021 "The 10000+ zone": the rule has no upper bound. Fixture EventId 10001 is unique, so the
        // rule extracts it and reports no uniqueness violation for it.
        var assemblies = new[] { typeof(ZoneEventIdFixtureLog).Assembly };

        var extracted = EventIdUniquenessRule.ExtractEventIds(assemblies);
        var violations = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(assemblies);

        extracted.ShouldContain(e => e.EventId == 10001 && e.TypeName.EndsWith(nameof(ZoneEventIdFixtureLog), StringComparison.Ordinal));
        violations.ShouldNotContain(v => v.StartsWith("EventId 10001 ", StringComparison.Ordinal));
    }

    private static List<(string Name, int Min, int Max)> CurrentRanges() =>
        [.. EventIdRanges.GetAllRanges().Select(r => (r.Name, r.Min, r.Max))];

    private static string Registry(string[]? registered = null, string[]? blocks = null) =>
        string.Join(
            Environment.NewLine,
            [
                "## Registered ranges",
                "",
                "| Name | Block | Area | Zone |",
                "| --- | --- | --- | --- |",
                .. registered ?? [],
                "",
                "## Blocks",
                "",
                "| Block | Area | Owner | Status |",
                "| --- | --- | --- | --- |",
                .. blocks ?? [],
            ]);
}

/// <summary>Fixture: a [LoggerMessage] with an EventId in the 10000+ zone.</summary>
internal static partial class ZoneEventIdFixtureLog
{
    [LoggerMessage(EventId = 10001, Level = LogLevel.Debug, Message = "Zone fixture")]
    internal static partial void InsideTheZone(ILogger logger);
}

/// <summary>
/// Parses <c>docs/engineering/eventid-block-registry.md</c> and checks it against the ranges of
/// <c>EventIdRanges.cs</c>.
/// </summary>
internal static class EventIdBlockRegistry
{
    private const int ZoneStart = 10000;

    private static readonly string[] RegisteredHeader = ["Name", "Block", "Area", "Zone"];
    private static readonly string[] BlocksHeader = ["Block", "Area", "Owner", "Status"];
    private static readonly string[] Statuses = ["registered", "reserved", "reserved-pre-zone", "superseded", "free"];

    private sealed record RegisteredRow(string Name, int Min, int Max, string Zone);

    private sealed record BlockRow(int Min, int Max, string Owner, string Status)
    {
        public string Describe() => $"block {Min}-{Max} ({Owner}, status '{Status}')";
    }

    internal static string FindRegistryPath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "engineering", "eventid-block-registry.md");
            if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "Encina.slnx")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"docs/engineering/eventid-block-registry.md not found above {AppContext.BaseDirectory}.");
    }

    /// <summary>Returns one message per disagreement; an empty list means the registry agrees.</summary>
    internal static List<string> Check(string registryMarkdown, IReadOnlyList<(string Name, int Min, int Max)> codeRanges)
    {
        var violations = new List<string>();
        var registered = new List<RegisteredRow>();
        var blocks = new List<BlockRow>();
        var sawRegisteredTable = false;
        var sawBlocksTable = false;
        string[]? currentHeader = null;

        foreach (var line in registryMarkdown.Split('\n'))
        {
            var text = line.Trim();
            if (!text.StartsWith('|'))
            {
                currentHeader = null;
                continue;
            }

            var cells = text.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.All(c => c.Length > 0 && c.All(ch => ch is '-' or ':')))
            {
                continue;
            }

            if (cells.SequenceEqual(RegisteredHeader, StringComparer.Ordinal))
            {
                currentHeader = RegisteredHeader;
                sawRegisteredTable = true;
                continue;
            }

            if (cells.SequenceEqual(BlocksHeader, StringComparer.Ordinal))
            {
                currentHeader = BlocksHeader;
                sawBlocksTable = true;
                continue;
            }

            if (currentHeader is null || cells.Length != currentHeader.Length)
            {
                continue;
            }

            if (!TryParseBlock(currentHeader == RegisteredHeader ? cells[1] : cells[0], out var min, out var max))
            {
                if (currentHeader == BlocksHeader && cells[0].StartsWith('('))
                {
                    continue; // prose placeholder such as "(existing ranges)"
                }

                violations.Add($"Registry row '{text}' has no valid 'min-max' block (min <= max).");
                continue;
            }

            if (currentHeader == RegisteredHeader)
            {
                registered.Add(new RegisteredRow(cells[0], min, max, cells[3]));
            }
            else if (!Statuses.Contains(cells[3], StringComparer.Ordinal))
            {
                violations.Add($"Registry block {min}-{max} has unknown status '{cells[3]}'; use one of {string.Join(", ", Statuses)}.");
            }
            else
            {
                blocks.Add(new BlockRow(min, max, cells[2], cells[3]));
            }
        }

        if (!sawRegisteredTable)
        {
            violations.Add("The registry has no 'registered ranges' table (header: Name | Block | Area | Zone).");
        }

        if (!sawBlocksTable)
        {
            violations.Add("The registry has no 'blocks' table (header: Block | Area | Owner | Status).");
        }

        CheckRegisteredAgainstCode(registered, blocks, codeRanges, violations);
        CheckReservedBlocks(blocks, codeRanges, violations);
        CheckZone(registered, blocks, violations);

        return violations;
    }

    private static void CheckRegisteredAgainstCode(
        List<RegisteredRow> registered,
        List<BlockRow> blocks,
        IReadOnlyList<(string Name, int Min, int Max)> codeRanges,
        List<string> violations)
    {
        // Rule 1: every range of EventIdRanges.cs is in the registry with the same bounds.
        foreach (var range in codeRanges)
        {
            var rows = registered.Where(r => r.Name == range.Name).ToList();
            if (rows.Count == 0)
            {
                violations.Add($"Range '{range.Name}' ({range.Min}-{range.Max}) of EventIdRanges.cs is missing from the registry.");
            }
            else if (!rows.Any(r => r.Min == range.Min && r.Max == range.Max))
            {
                violations.Add(
                    $"Range '{range.Name}' is {range.Min}-{range.Max} in EventIdRanges.cs but " +
                    $"{rows[0].Min}-{rows[0].Max} in the registry.");
            }
        }

        // Rule 2: every registry row that claims to be registered matches a range of EventIdRanges.cs.
        foreach (var row in registered.Where(r => !codeRanges.Any(c => c.Name == r.Name)))
        {
            violations.Add($"Registry range '{row.Name}' ({row.Min}-{row.Max}) does not exist in EventIdRanges.cs.");
        }

        foreach (var block in blocks.Where(b => b.Status == "registered"))
        {
            if (!codeRanges.Any(c => c.Min == block.Min && c.Max == block.Max))
            {
                violations.Add($"Registry {block.Describe()} matches no range of EventIdRanges.cs with the same bounds.");
            }
        }
    }

    private static void CheckReservedBlocks(
        List<BlockRow> blocks,
        IReadOnlyList<(string Name, int Min, int Max)> codeRanges,
        List<string> violations)
    {
        var reserved = blocks.Where(b => b.Status is "reserved" or "reserved-pre-zone").ToList();

        // Rule 3: a reserved block overlaps no registered range.
        foreach (var block in reserved)
        {
            foreach (var range in codeRanges.Where(c => block.Min <= c.Max && c.Min <= block.Max))
            {
                violations.Add($"Registry {block.Describe()} overlaps registered range '{range.Name}' ({range.Min}-{range.Max}).");
            }
        }

        // Rule 4: reserved blocks overlap each other nowhere.
        for (var i = 0; i < reserved.Count; i++)
        {
            for (var j = i + 1; j < reserved.Count; j++)
            {
                if (reserved[i].Min <= reserved[j].Max && reserved[j].Min <= reserved[i].Max)
                {
                    violations.Add($"Registry {reserved[i].Describe()} overlaps {reserved[j].Describe()}.");
                }
            }
        }
    }

    private static void CheckZone(List<RegisteredRow> registered, List<BlockRow> blocks, List<string> violations)
    {
        // Rule 5: below 10000 only the historical registered ranges and the reserved-pre-zone blocks.
        foreach (var row in registered)
        {
            var expected = row.Min < ZoneStart ? "historical" : "zone";
            if (row.Zone != expected || row.Max >= ZoneStart != row.Min >= ZoneStart)
            {
                violations.Add(
                    $"Registry range '{row.Name}' ({row.Min}-{row.Max}) must have Zone '{expected}' and not straddle 10000; found '{row.Zone}'.");
            }
        }

        foreach (var block in blocks.Where(b => b.Status is "reserved" or "reserved-pre-zone"))
        {
            if (block.Status == "reserved" && block.Min < ZoneStart)
            {
                violations.Add(
                    $"Registry {block.Describe()} lies below {ZoneStart}: new blocks are born in the 10000+ zone; " +
                    "only a block recorded before the zone existed may carry status 'reserved-pre-zone'.");
            }
            else if (block.Status == "reserved-pre-zone" && block.Min >= ZoneStart)
            {
                violations.Add($"Registry {block.Describe()} is in the 10000+ zone; status 'reserved-pre-zone' is only for blocks below {ZoneStart}.");
            }
        }
    }

    private static bool TryParseBlock(string text, out int min, out int max)
    {
        min = max = 0;
        var parts = text.Split('-');
        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out min)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out max)
            && min <= max;
    }
}
