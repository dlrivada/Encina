using Encina.OpenTelemetry.Migrations;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationMetrics"/>.
/// </summary>
public sealed class MigrationMetricsTests
{
    [Fact]
    public void Constructor_NullCallbacks_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new MigrationMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);

        var ex = Record.Exception(() => new MigrationMetrics(callbacks));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordShardMigrated_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordShardMigrated("migration-001", "Sequential", "shard-1"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordShardFailed_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordShardFailed("migration-001", "Parallel", "shard-2"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordRollback_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordRollback("migration-001"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordPerShardDuration_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordPerShardDuration("migration-001", "shard-1", 1234.5));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordTotalDuration_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        var ex = Record.Exception(() =>
            metrics.RecordTotalDuration("migration-001", "Sequential", 5678.9));
        ex.ShouldBeNull();
    }
}
