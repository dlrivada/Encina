using System.Reflection;
using Encina.Diagnostics;
using Encina.Marten;
using Encina.Testing.Architecture;
using Shouldly;

namespace Encina.UnitTests.Marten;

/// <summary>
/// ADR-021 enforcement for <c>Encina.Marten</c> (issue #1115): every <c>[LoggerMessage]</c> EventId
/// of the package is unique and lies inside <see cref="EventIdRanges.Marten"/>.
/// </summary>
public sealed class MartenEventIdAllocationTests
{
    private static readonly Assembly MartenAssembly = typeof(MartenAggregateRepository<>).Assembly;

    [Fact]
    public void EventIds_AreUniqueWithinThePackage()
    {
        var eventIds = EventIdUniquenessRule.ExtractEventIds([MartenAssembly]);

        eventIds.ShouldNotBeEmpty();
        var duplicates = eventIds
            .GroupBy(e => e.EventId)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(e => $"{e.TypeName}.{e.MethodName}"))}")
            .ToList();
        duplicates.ShouldBeEmpty();
    }

    [Fact]
    public void EventIds_LieInsideTheRegisteredMartenRange()
    {
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
            [MartenAssembly],
            new Dictionary<string, string> { [MartenAssembly.GetName().Name!] = nameof(EventIdRanges.Marten) });

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void EventIds_ArePackedFromTheStartOfTheRange()
    {
        var ids = EventIdUniquenessRule.ExtractEventIds([MartenAssembly])
            .Select(e => e.EventId)
            .Order()
            .ToList();

        // CLAUDE.md rule 17: sequential allocation, no gaps.
        ids[0].ShouldBe(EventIdRanges.Marten.Min);
        ids.ShouldBe(Enumerable.Range(EventIdRanges.Marten.Min, ids.Count));
    }

    [Fact]
    public void RegisteredRanges_DoNotOverlap()
    {
        EventIdUniquenessRule.AssertNoRangeOverlaps().ShouldBeEmpty();
    }
}
