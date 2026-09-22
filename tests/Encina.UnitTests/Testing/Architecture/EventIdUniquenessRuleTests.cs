using System.Reflection;

using Encina.Diagnostics;
using Encina.Marten;
using Encina.Testing.Architecture;

using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Tests for <see cref="EventIdUniquenessRule"/> validation methods.
/// </summary>
public sealed class EventIdUniquenessRuleTests
{
    // ========================================================================
    // AssertNoRangeOverlaps
    // ========================================================================

    [Fact]
    public void AssertNoRangeOverlaps_ShouldReturnNoViolations_ForCurrentRegistry()
    {
        // Act
        var violations = EventIdUniquenessRule.AssertNoRangeOverlaps();

        // Assert
        Assert.Empty(violations);
    }

    // ========================================================================
    // GetAllRanges
    // ========================================================================

    [Fact]
    public void GetAllRanges_ShouldReturnNonEmptyList()
    {
        // Act
        var ranges = EventIdRanges.GetAllRanges();

        // Assert
        Assert.NotEmpty(ranges);
    }

    [Fact]
    public void GetAllRanges_ShouldReturnRangesSortedByMin()
    {
        // Act
        var ranges = EventIdRanges.GetAllRanges();

        // Assert
        for (var i = 1; i < ranges.Count; i++)
        {
            Assert.True(ranges[i].Min > ranges[i - 1].Max,
                $"Range '{ranges[i].Name}' (Min={ranges[i].Min}) should start after " +
                $"'{ranges[i - 1].Name}' (Max={ranges[i - 1].Max}).");
        }
    }

    [Fact]
    public void GetAllRanges_AllRangesShouldHavePositiveSpan()
    {
        // Act
        var ranges = EventIdRanges.GetAllRanges();

        // Assert
        foreach (var range in ranges)
        {
            Assert.True(range.Min <= range.Max,
                $"Range '{range.Name}' has Min ({range.Min}) > Max ({range.Max}).");
        }
    }

    // ========================================================================
    // ExtractEventIds
    // ========================================================================

    [Fact]
    public void ExtractEventIds_NullAssemblies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EventIdUniquenessRule.ExtractEventIds(null!));
    }

    [Fact]
    public void ExtractEventIds_EmptyAssemblies_ReturnsEmptyList()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();

        // Act
        var result = EventIdUniquenessRule.ExtractEventIds(assemblies);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractEventIds_ShouldFindEventIds_InEncinaAssembly()
    {
        // Arrange — Encina.Messaging has [LoggerMessage] attributes
        var assemblies = new[] { typeof(EventIdRanges).Assembly };

        // Act
        var result = EventIdUniquenessRule.ExtractEventIds(assemblies);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.True(r.EventId >= 0));
    }

    // ========================================================================
    // AssertEventIdsAreGloballyUnique
    // ========================================================================

    [Fact]
    public void AssertEventIdsAreGloballyUnique_NullAssemblies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(null!));
    }

    [Fact]
    public void AssertEventIdsAreGloballyUnique_EmptyAssemblies_ReturnsNoViolations()
    {
        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(
            Array.Empty<Assembly>());

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void AssertEventIdsAreGloballyUnique_SingleAssembly_ReturnsNoViolations()
    {
        // Arrange — the core assembly declares every EventId once
        var assemblies = new[] { typeof(EventIdRanges).Assembly };

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(assemblies);

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void AssertEventIdsAreGloballyUnique_DuplicateWithinOneAssembly_ReportsViolation()
    {
        // Arrange — DuplicateEventIdLogA and DuplicateEventIdLogB (below) share EventId 990001
        // in this test assembly; SYSLIB1006 does not catch it because they are different classes.
        var assemblies = new[] { typeof(DuplicateEventIdLogA).Assembly };

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(assemblies);

        // Assert
        var violation = Assert.Single(violations, v => v.StartsWith("EventId 990001 ", StringComparison.Ordinal));
        Assert.Contains("within assembly", violation);
        Assert.Contains(nameof(DuplicateEventIdLogA), violation);
        Assert.Contains(nameof(DuplicateEventIdLogB), violation);
    }

    // ========================================================================
    // AssertEventIdsWithinRegisteredRanges
    // ========================================================================

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_AssemblyMappedToSeveralRanges_AcceptsIdsInAnyOfThem()
    {
        // Arrange — Encina.Marten's EventIds all lie in EventIdRanges.Marten; Sanitization is an extra, unused range
        var assembly = typeof(MartenAggregateRepository<>).Assembly;
        var mapping = new Dictionary<string, IReadOnlyList<string>>
        {
            [assembly.GetName().Name!] = [nameof(EventIdRanges.Sanitization), nameof(EventIdRanges.Marten)],
        };

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges([assembly], mapping);

        // Assert
        Assert.Empty(violations);
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_IdsOutsideEveryMappedRange_ReportsEachOne()
    {
        // Arrange
        var assembly = typeof(MartenAggregateRepository<>).Assembly;
        var mapping = new Dictionary<string, IReadOnlyList<string>>
        {
            [assembly.GetName().Name!] = [nameof(EventIdRanges.Sanitization)],
        };
        var eventIdCount = EventIdUniquenessRule.ExtractEventIds([assembly]).Count;

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges([assembly], mapping);

        // Assert
        Assert.Equal(eventIdCount, violations.Count);
        Assert.All(violations, v => Assert.Contains("'Sanitization' (1-99)", v));
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_UnknownRangeName_ReportsViolation()
    {
        // Arrange
        var assembly = typeof(EventIdRanges).Assembly;
        var mapping = new Dictionary<string, IReadOnlyList<string>>
        {
            [assembly.GetName().Name!] = [nameof(EventIdRanges.Sanitization), "NoSuchRange"],
        };

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges([assembly], mapping);

        // Assert
        var violation = Assert.Single(violations);
        Assert.Contains("'NoSuchRange'", violation);
        Assert.Contains("do not exist", violation);
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_EmptyRangeList_IsTreatedAsUnmapped()
    {
        // Arrange
        var assembly = typeof(EventIdRanges).Assembly;
        var mapping = new Dictionary<string, IReadOnlyList<string>> { [assembly.GetName().Name!] = [] };

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges([assembly], mapping);

        // Assert
        Assert.Contains(violations, v => v.Contains("not mapped"));
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_NullAssemblies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
                null!,
                new Dictionary<string, IReadOnlyList<string>>()));
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_NullMapping_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
                Array.Empty<Assembly>(),
                null!));
    }

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_UnmappedAssembly_ReportsViolation()
    {
        // Arrange
        var assemblies = new[] { typeof(EventIdRanges).Assembly };
        var mapping = new Dictionary<string, IReadOnlyList<string>>(); // empty — no mapping

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
            assemblies, mapping);

        // Assert
        Assert.NotEmpty(violations);
        Assert.Contains(violations, v => v.Contains("not mapped"));
    }

    // ========================================================================
    // GenerateAllocationReport
    // ========================================================================

    [Fact]
    public void GenerateAllocationReport_WithoutAssemblies_ReturnsFormattedTable()
    {
        // Act
        var report = EventIdUniquenessRule.GenerateAllocationReport();

        // Assert
        Assert.Contains("EventId Allocation Report", report);
        Assert.Contains("Sanitization", report);
        Assert.Contains("ComplianceNIS2", report);
    }

    [Fact]
    public void GenerateAllocationReport_WithAssemblies_IncludesUsageStats()
    {
        // Arrange
        var assemblies = new[] { typeof(EventIdRanges).Assembly };

        // Act
        var report = EventIdUniquenessRule.GenerateAllocationReport(assemblies);

        // Assert
        Assert.Contains("EventId Allocation Report", report);
        Assert.Contains("Used", report);
    }

    // ========================================================================
    // Known registry entries
    // ========================================================================

    [Theory]
    [InlineData("Sanitization", 1, 99)]
    [InlineData("ComplianceGDPR", 8100, 8199)]
    [InlineData("ComplianceNIS2", 9200, 9299)]
    [InlineData("ComplianceCrossBorderTransfer", 9300, 9399)]
    [InlineData("ComplianceProcessorAgreements", 9400, 9499)]
    public void GetAllRanges_ShouldContainExpectedRange(string name, int expectedMin, int expectedMax)
    {
        // Act
        var ranges = EventIdRanges.GetAllRanges();

        // Assert
        var match = ranges.FirstOrDefault(r => r.Name == name);
        Assert.Equal(name, match.Name);
        Assert.Equal(expectedMin, match.Min);
        Assert.Equal(expectedMax, match.Max);
    }
}

/// <summary>Fixture: declares EventId 990001, also declared by <see cref="DuplicateEventIdLogB"/>.</summary>
internal static partial class DuplicateEventIdLogA
{
    [LoggerMessage(EventId = 990001, Level = LogLevel.Debug, Message = "Duplicate fixture A")]
    internal static partial void First(ILogger logger);
}

/// <summary>Fixture: declares EventId 990001, also declared by <see cref="DuplicateEventIdLogA"/>.</summary>
internal static partial class DuplicateEventIdLogB
{
    [LoggerMessage(EventId = 990001, Level = LogLevel.Debug, Message = "Duplicate fixture B")]
    internal static partial void Second(ILogger logger);
}
