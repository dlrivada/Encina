using System.Reflection;

using Encina.Diagnostics;
using Encina.Testing.Architecture;

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
        // Arrange — a single assembly can't have cross-assembly duplicates
        var assemblies = new[] { typeof(EventIdRanges).Assembly };

        // Act
        var violations = EventIdUniquenessRule.AssertEventIdsAreGloballyUnique(assemblies);

        // Assert
        Assert.Empty(violations);
    }

    // ========================================================================
    // AssertEventIdsWithinRegisteredRanges
    // ========================================================================

    [Fact]
    public void AssertEventIdsWithinRegisteredRanges_NullAssemblies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EventIdUniquenessRule.AssertEventIdsWithinRegisteredRanges(
                null!,
                new Dictionary<string, string>()));
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
        var mapping = new Dictionary<string, string>(); // empty — no mapping

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
