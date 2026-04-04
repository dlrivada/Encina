using Encina.OpenTelemetry.Migrations;
using Encina.OpenTelemetry.ReferenceTable;
using Encina.OpenTelemetry.Resharding;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for Metrics classes covering constructor null checks and method invocations.
/// Covers <see cref="MigrationMetrics"/>, <see cref="ReshardingMetrics"/>,
/// <see cref="ReferenceTableMetrics"/>, and their callback classes.
/// </summary>
public sealed class MetricsGuardTests
{
    #region MigrationMetricsCallbacks

    [Fact]
    public void MigrationMetricsCallbacks_NullDriftCallback_ThrowsArgumentNullException()
    {
        var act = () => new MigrationMetricsCallbacks(driftDetectedCountCallback: null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("driftDetectedCountCallback");
    }

    [Fact]
    public void MigrationMetricsCallbacks_ValidCallback_DoesNotThrow()
    {
        Should.NotThrow(() => new MigrationMetricsCallbacks(() => 0));
    }

    #endregion

    #region MigrationMetrics

    [Fact]
    public void MigrationMetrics_NullCallbacks_ThrowsArgumentNullException()
    {
        var act = () => new MigrationMetrics(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("callbacks");
    }

    [Fact]
    public void MigrationMetrics_ValidCallbacks_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);

        Should.NotThrow(() => new MigrationMetrics(callbacks));
    }

    [Fact]
    public void MigrationMetrics_RecordShardMigrated_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordShardMigrated("mig-001", "Sequential", "shard-1"));
    }

    [Fact]
    public void MigrationMetrics_RecordShardFailed_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordShardFailed("mig-001", "Sequential", "shard-2"));
    }

    [Fact]
    public void MigrationMetrics_RecordRollback_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordRollback("mig-001"));
    }

    [Fact]
    public void MigrationMetrics_RecordPerShardDuration_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordPerShardDuration("mig-001", "shard-1", 1234.5));
    }

    [Fact]
    public void MigrationMetrics_RecordTotalDuration_DoesNotThrow()
    {
        var callbacks = new MigrationMetricsCallbacks(() => 0);
        var metrics = new MigrationMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordTotalDuration("mig-001", "Sequential", 5678.9));
    }

    #endregion

    #region ReshardingMetricsCallbacks

    [Fact]
    public void ReshardingMetricsCallbacks_NullRowsPerSecondCallback_ThrowsArgumentNullException()
    {
        var act = () => new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: null!,
            cdcLagMsCallback: () => 0.0,
            activeReshardingCountCallback: () => 0);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rowsPerSecondCallback");
    }

    [Fact]
    public void ReshardingMetricsCallbacks_NullCdcLagMsCallback_ThrowsArgumentNullException()
    {
        var act = () => new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: () => 0.0,
            cdcLagMsCallback: null!,
            activeReshardingCountCallback: () => 0);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cdcLagMsCallback");
    }

    [Fact]
    public void ReshardingMetricsCallbacks_NullActiveCountCallback_ThrowsArgumentNullException()
    {
        var act = () => new ReshardingMetricsCallbacks(
            rowsPerSecondCallback: () => 0.0,
            cdcLagMsCallback: () => 0.0,
            activeReshardingCountCallback: null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("activeReshardingCountCallback");
    }

    [Fact]
    public void ReshardingMetricsCallbacks_ValidCallbacks_DoesNotThrow()
    {
        Should.NotThrow(() => new ReshardingMetricsCallbacks(
            () => 100.0,
            () => 50.0,
            () => 2));
    }

    #endregion

    #region ReshardingMetrics

    [Fact]
    public void ReshardingMetrics_NullCallbacks_ThrowsArgumentNullException()
    {
        var act = () => new ReshardingMetrics(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("callbacks");
    }

    [Fact]
    public void ReshardingMetrics_ValidCallbacks_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 100.0, () => 50.0, () => 2);

        Should.NotThrow(() => new ReshardingMetrics(callbacks));
    }

    [Fact]
    public void ReshardingMetrics_RecordPhaseDuration_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 100.0, () => 50.0, () => 2);
        var metrics = new ReshardingMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordPhaseDuration(Guid.NewGuid(), "Copying", 12345.6));
    }

    [Fact]
    public void ReshardingMetrics_RecordRowsCopied_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 100.0, () => 50.0, () => 2);
        var metrics = new ReshardingMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordRowsCopied("shard-1", "shard-3", 5000));
    }

    [Fact]
    public void ReshardingMetrics_RecordVerificationMismatch_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 100.0, () => 50.0, () => 2);
        var metrics = new ReshardingMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordVerificationMismatch(Guid.NewGuid()));
    }

    [Fact]
    public void ReshardingMetrics_RecordCutoverDuration_DoesNotThrow()
    {
        var callbacks = new ReshardingMetricsCallbacks(() => 100.0, () => 50.0, () => 2);
        var metrics = new ReshardingMetrics(callbacks);

        Should.NotThrow(() => metrics.RecordCutoverDuration(Guid.NewGuid(), 250.0));
    }

    #endregion

    #region ReferenceTableMetrics

    [Fact]
    public void ReferenceTableMetrics_NullRegisteredTablesCallback_ThrowsArgumentNullException()
    {
        var act = () => new ReferenceTableMetrics(
            registeredTablesCallback: null!,
            stalenessCallback: () => []);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registeredTablesCallback");
    }

    [Fact]
    public void ReferenceTableMetrics_NullStalenessCallback_ThrowsArgumentNullException()
    {
        var act = () => new ReferenceTableMetrics(
            registeredTablesCallback: () => 0,
            stalenessCallback: null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("stalenessCallback");
    }

    [Fact]
    public void ReferenceTableMetrics_ValidCallbacks_DoesNotThrow()
    {
        Should.NotThrow(() => new ReferenceTableMetrics(
            () => 3,
            () => [("Country", 120.5), ("Currency", 45.0)]));
    }

    [Fact]
    public void ReferenceTableMetrics_RecordReplicationDuration_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 3, () => []);

        Should.NotThrow(() => metrics.RecordReplicationDuration("Country", 250.0));
    }

    [Fact]
    public void ReferenceTableMetrics_RecordRowsSynced_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 3, () => []);

        Should.NotThrow(() => metrics.RecordRowsSynced("Country", 100));
    }

    [Fact]
    public void ReferenceTableMetrics_RecordError_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 3, () => []);

        Should.NotThrow(() => metrics.RecordError("Country", "ConnectionTimeout"));
    }

    [Fact]
    public void ReferenceTableMetrics_IncrementActiveReplications_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 3, () => []);

        Should.NotThrow(() => metrics.IncrementActiveReplications());
    }

    [Fact]
    public void ReferenceTableMetrics_DecrementActiveReplications_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 3, () => []);

        Should.NotThrow(() => metrics.DecrementActiveReplications());
    }

    #endregion
}
