using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableOptions"/>.
/// </summary>
public sealed class ReferenceTableOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Default Values
    // ────────────────────────────────────────────────────────────

    #region Default Values

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new ReferenceTableOptions();

        // Assert
        options.RefreshStrategy.ShouldBe(RefreshStrategy.Polling);
        options.PrimaryShardId.ShouldBeNull();
        options.PollingInterval.ShouldBe(TimeSpan.FromMinutes(5));
        options.BatchSize.ShouldBe(1000);
        options.SyncOnStartup.ShouldBeTrue();
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Property Setters
    // ────────────────────────────────────────────────────────────

    #region Property Setters

    [Fact]
    public void RefreshStrategy_CanBeSet()
    {
        // Act
        var options = new ReferenceTableOptions { RefreshStrategy = RefreshStrategy.CdcDriven };

        // Assert
        options.RefreshStrategy.ShouldBe(RefreshStrategy.CdcDriven);
    }

    [Fact]
    public void RefreshStrategy_Manual_CanBeSet()
    {
        // Act
        var options = new ReferenceTableOptions { RefreshStrategy = RefreshStrategy.Manual };

        // Assert
        options.RefreshStrategy.ShouldBe(RefreshStrategy.Manual);
    }

    [Fact]
    public void PrimaryShardId_CanBeSet()
    {
        // Act
        var options = new ReferenceTableOptions { PrimaryShardId = "shard-0" };

        // Assert
        options.PrimaryShardId.ShouldBe("shard-0");
    }

    [Fact]
    public void PrimaryShardId_CanBeSetToNull()
    {
        // Act
        var options = new ReferenceTableOptions { PrimaryShardId = null };

        // Assert
        options.PrimaryShardId.ShouldBeNull();
    }

    [Fact]
    public void PollingInterval_CanBeSet()
    {
        // Act
        var options = new ReferenceTableOptions { PollingInterval = TimeSpan.FromMinutes(10) };

        // Assert
        options.PollingInterval.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void BatchSize_CanBeSet()
    {
        // Act
        var options = new ReferenceTableOptions { BatchSize = 500 };

        // Assert
        options.BatchSize.ShouldBe(500);
    }

    [Fact]
    public void SyncOnStartup_CanBeSetToFalse()
    {
        // Act
        var options = new ReferenceTableOptions { SyncOnStartup = false };

        // Assert
        options.SyncOnStartup.ShouldBeFalse();
    }

    #endregion
}
