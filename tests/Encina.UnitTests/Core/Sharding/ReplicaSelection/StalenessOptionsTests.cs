using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class StalenessOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Defaults_MaxLagIsNull()
    {
        var options = new StalenessOptions();
        options.MaxAcceptableReplicationLag.ShouldBeNull();
    }

    [Fact]
    public void Defaults_FallbackToPrimaryWhenStaleIsTrue()
    {
        var options = new StalenessOptions();
        options.FallbackToPrimaryWhenStale.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_IsEnabledIsFalse()
    {
        var options = new StalenessOptions();
        options.IsEnabled.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  IsEnabled
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsEnabled_WhenMaxLagSet_ReturnsTrue()
    {
        var options = new StalenessOptions
        {
            MaxAcceptableReplicationLag = TimeSpan.FromSeconds(5),
        };

        options.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void IsEnabled_WhenMaxLagNull_ReturnsFalse()
    {
        var options = new StalenessOptions
        {
            MaxAcceptableReplicationLag = null,
        };

        options.IsEnabled.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  Properties
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MaxAcceptableReplicationLag_CanBeSet()
    {
        var options = new StalenessOptions
        {
            MaxAcceptableReplicationLag = TimeSpan.FromSeconds(10),
        };

        options.MaxAcceptableReplicationLag.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void FallbackToPrimaryWhenStale_CanBeSetToFalse()
    {
        var options = new StalenessOptions
        {
            FallbackToPrimaryWhenStale = false,
        };

        options.FallbackToPrimaryWhenStale.ShouldBeFalse();
    }
}
