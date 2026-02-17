using Encina.OpenTelemetry.Migrations;
using Encina.Sharding.Migrations;

namespace Encina.ContractTests.Migrations;

/// <summary>
/// Contract tests verifying that default option values are consistent between
/// <see cref="MigrationOptions"/> and <see cref="MigrationCoordinationOptions"/>,
/// and that related option classes have expected defaults.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MigrationOptionsDefaultContracts
{
    // ── MigrationOptions defaults ─────────────────────────────────

    [Fact]
    public void MigrationOptions_DefaultStrategy_IsSequential()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act & Assert
        options.Strategy.ShouldBe(MigrationStrategy.Sequential);
    }

    [Fact]
    public void MigrationOptions_DefaultMaxParallelism_IsFour()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act & Assert
        options.MaxParallelism.ShouldBe(4);
    }

    [Fact]
    public void MigrationOptions_DefaultStopOnFirstFailure_IsTrue()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act & Assert
        options.StopOnFirstFailure.ShouldBeTrue();
    }

    [Fact]
    public void MigrationOptions_DefaultPerShardTimeout_IsFiveMinutes()
    {
        // Arrange
        var options = new MigrationOptions();

        // Act & Assert
        options.PerShardTimeout.ShouldBe(TimeSpan.FromMinutes(5));
    }

    // ── MigrationCoordinationOptions defaults match MigrationOptions ──

    [Fact]
    public void MigrationCoordinationOptions_DefaultsMatch_MigrationOptionsDefaults()
    {
        // Arrange
        var migrationOptions = new MigrationOptions();
        var coordinationOptions = new MigrationCoordinationOptions();

        // Act & Assert — verify that coordination defaults match migration defaults
        coordinationOptions.DefaultStrategy.ShouldBe(migrationOptions.Strategy,
            "DefaultStrategy should match MigrationOptions.Strategy default");

        coordinationOptions.MaxParallelism.ShouldBe(migrationOptions.MaxParallelism,
            "MaxParallelism should match MigrationOptions.MaxParallelism default");

        coordinationOptions.StopOnFirstFailure.ShouldBe(migrationOptions.StopOnFirstFailure,
            "StopOnFirstFailure should match MigrationOptions.StopOnFirstFailure default");

        coordinationOptions.PerShardTimeout.ShouldBe(migrationOptions.PerShardTimeout,
            "PerShardTimeout should match MigrationOptions.PerShardTimeout default");

        coordinationOptions.ValidateBeforeApply.ShouldBe(migrationOptions.ValidateBeforeApply,
            "ValidateBeforeApply should match MigrationOptions.ValidateBeforeApply default");
    }

    // ── SchemaDriftHealthCheckOptions defaults ─────────────────────

    [Fact]
    public void SchemaDriftHealthCheckOptions_DefaultTimeout_IsThirtySeconds()
    {
        // Arrange
        var options = new SchemaDriftHealthCheckOptions();

        // Act & Assert
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void SchemaDriftHealthCheckOptions_DefaultCriticalTables_IsEmpty()
    {
        // Arrange
        var options = new SchemaDriftHealthCheckOptions();

        // Act & Assert
        options.CriticalTables.ShouldBeEmpty();
    }

    // ── DriftDetectionOptions defaults ─────────────────────────────

    [Fact]
    public void DriftDetectionOptions_DefaultIncludeColumnDiffs_IsTrue()
    {
        // Arrange
        var options = new DriftDetectionOptions();

        // Act & Assert
        options.IncludeColumnDiffs.ShouldBeTrue();
    }

    [Fact]
    public void DriftDetectionOptions_DefaultComparisonDepth_IsTablesAndColumns()
    {
        // Arrange
        var options = new DriftDetectionOptions();

        // Act & Assert
        options.ComparisonDepth.ShouldBe(SchemaComparisonDepth.TablesAndColumns);
    }
}
