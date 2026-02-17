using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableGlobalOptions"/>.
/// </summary>
public sealed class ReferenceTableGlobalOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Default Values
    // ────────────────────────────────────────────────────────────

    #region Default Values

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new ReferenceTableGlobalOptions();

        // Assert
        options.MaxParallelShards.ShouldBe(Environment.ProcessorCount);
        options.DefaultRefreshStrategy.ShouldBe(RefreshStrategy.Polling);
        options.HealthCheckUnhealthyThreshold.ShouldBe(TimeSpan.FromMinutes(5));
        options.HealthCheckDegradedThreshold.ShouldBe(TimeSpan.FromMinutes(1));
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Property Setters
    // ────────────────────────────────────────────────────────────

    #region Property Setters

    [Fact]
    public void MaxParallelShards_CanBeSet()
    {
        // Act
        var options = new ReferenceTableGlobalOptions { MaxParallelShards = 4 };

        // Assert
        options.MaxParallelShards.ShouldBe(4);
    }

    [Fact]
    public void DefaultRefreshStrategy_CanBeSet()
    {
        // Act
        var options = new ReferenceTableGlobalOptions { DefaultRefreshStrategy = RefreshStrategy.CdcDriven };

        // Assert
        options.DefaultRefreshStrategy.ShouldBe(RefreshStrategy.CdcDriven);
    }

    [Fact]
    public void HealthCheckUnhealthyThreshold_CanBeSet()
    {
        // Act
        var options = new ReferenceTableGlobalOptions
        {
            HealthCheckUnhealthyThreshold = TimeSpan.FromMinutes(10)
        };

        // Assert
        options.HealthCheckUnhealthyThreshold.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void HealthCheckDegradedThreshold_CanBeSet()
    {
        // Act
        var options = new ReferenceTableGlobalOptions
        {
            HealthCheckDegradedThreshold = TimeSpan.FromMinutes(2)
        };

        // Assert
        options.HealthCheckDegradedThreshold.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Act
        var options = new ReferenceTableGlobalOptions
        {
            MaxParallelShards = 8,
            DefaultRefreshStrategy = RefreshStrategy.Manual,
            HealthCheckUnhealthyThreshold = TimeSpan.FromMinutes(15),
            HealthCheckDegradedThreshold = TimeSpan.FromMinutes(3)
        };

        // Assert
        options.MaxParallelShards.ShouldBe(8);
        options.DefaultRefreshStrategy.ShouldBe(RefreshStrategy.Manual);
        options.HealthCheckUnhealthyThreshold.ShouldBe(TimeSpan.FromMinutes(15));
        options.HealthCheckDegradedThreshold.ShouldBe(TimeSpan.FromMinutes(3));
    }

    #endregion
}
