using Encina.Sharding.Migrations;

namespace Encina.ContractTests.Migrations;

/// <summary>
/// Contract tests verifying that <see cref="SchemaDriftReport.HasDrift"/> is consistent
/// with the contents of <see cref="SchemaDriftReport.Diffs"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class SchemaDriftReportContracts
{
    [Fact]
    public void HasDrift_WithEmptyDiffs_ReturnsFalse()
    {
        // Arrange
        var report = new SchemaDriftReport(
            Diffs: [],
            DetectedAtUtc: DateTimeOffset.UtcNow);

        // Act & Assert
        report.HasDrift.ShouldBeFalse();
    }

    [Fact]
    public void HasDrift_WithDiffsContainingTableDiffs_ReturnsTrue()
    {
        // Arrange
        var tableDiffs = new List<TableDiff>
        {
            new("orders", TableDiffType.Missing),
            new("customers", TableDiffType.Modified, ["email_column_type_changed"])
        };

        var shardDiff = new ShardSchemaDiff("shard-2", "shard-1", tableDiffs);
        var report = new SchemaDriftReport(
            Diffs: [shardDiff],
            DetectedAtUtc: DateTimeOffset.UtcNow);

        // Act & Assert
        report.HasDrift.ShouldBeTrue();
    }

    [Fact]
    public void HasDrift_WithDiffsButEmptyTableDiffs_ReturnsFalse()
    {
        // Arrange — ShardSchemaDiff entries exist but each has zero table diffs
        var shardDiffA = new ShardSchemaDiff("shard-2", "shard-1", []);
        var shardDiffB = new ShardSchemaDiff("shard-3", "shard-1", []);

        var report = new SchemaDriftReport(
            Diffs: [shardDiffA, shardDiffB],
            DetectedAtUtc: DateTimeOffset.UtcNow);

        // Act & Assert — HasDrift checks both Diffs.Count > 0 AND any TableDiffs.Count > 0
        report.HasDrift.ShouldBeFalse();
    }
}
