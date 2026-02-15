using Encina.Sharding.ReferenceTables.Health;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableHealthCheckOptions"/>.
/// </summary>
public sealed class ReferenceTableHealthCheckOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Default Values
    // ────────────────────────────────────────────────────────────

    #region Default Values

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new ReferenceTableHealthCheckOptions();

        // Assert
        options.UnhealthyThreshold.ShouldBe(TimeSpan.FromMinutes(5));
        options.DegradedThreshold.ShouldBe(TimeSpan.FromMinutes(1));
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Property Setters
    // ────────────────────────────────────────────────────────────

    #region Property Setters

    [Fact]
    public void UnhealthyThreshold_CanBeSet()
    {
        // Act
        var options = new ReferenceTableHealthCheckOptions
        {
            UnhealthyThreshold = TimeSpan.FromMinutes(10)
        };

        // Assert
        options.UnhealthyThreshold.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void DegradedThreshold_CanBeSet()
    {
        // Act
        var options = new ReferenceTableHealthCheckOptions
        {
            DegradedThreshold = TimeSpan.FromMinutes(2)
        };

        // Assert
        options.DegradedThreshold.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Act
        var options = new ReferenceTableHealthCheckOptions
        {
            UnhealthyThreshold = TimeSpan.FromMinutes(15),
            DegradedThreshold = TimeSpan.FromSeconds(30)
        };

        // Assert
        options.UnhealthyThreshold.ShouldBe(TimeSpan.FromMinutes(15));
        options.DegradedThreshold.ShouldBe(TimeSpan.FromSeconds(30));
    }

    #endregion
}
