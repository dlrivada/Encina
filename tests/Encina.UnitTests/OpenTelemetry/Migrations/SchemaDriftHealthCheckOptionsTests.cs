using Encina.OpenTelemetry.Migrations;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Migrations;

/// <summary>
/// Unit tests for <see cref="SchemaDriftHealthCheckOptions"/>.
/// </summary>
public sealed class SchemaDriftHealthCheckOptionsTests
{
    [Fact]
    public void Constructor_Timeout_DefaultsToThirtySeconds()
    {
        var options = new SchemaDriftHealthCheckOptions();
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Constructor_BaselineShardId_DefaultsToNull()
    {
        var options = new SchemaDriftHealthCheckOptions();
        options.BaselineShardId.ShouldBeNull();
    }

    [Fact]
    public void Constructor_CriticalTables_DefaultsToEmptyList()
    {
        var options = new SchemaDriftHealthCheckOptions();
        options.CriticalTables.ShouldNotBeNull();
        options.CriticalTables.ShouldBeEmpty();
    }

    [Fact]
    public void Timeout_ShouldBeSettable()
    {
        var options = new SchemaDriftHealthCheckOptions
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void BaselineShardId_ShouldBeSettable()
    {
        var options = new SchemaDriftHealthCheckOptions
        {
            BaselineShardId = "shard-primary"
        };
        options.BaselineShardId.ShouldBe("shard-primary");
    }

    [Fact]
    public void CriticalTables_ShouldBeSettable()
    {
        var options = new SchemaDriftHealthCheckOptions
        {
            CriticalTables = ["orders", "payments"]
        };
        options.CriticalTables.Count.ShouldBe(2);
        options.CriticalTables.ShouldContain("orders");
        options.CriticalTables.ShouldContain("payments");
    }
}
