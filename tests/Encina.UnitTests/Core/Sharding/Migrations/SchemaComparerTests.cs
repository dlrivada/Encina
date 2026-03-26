using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Core.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="SchemaComparer"/>.
/// </summary>
public sealed class SchemaComparerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Compare_IdenticalSchemas_ReturnsNoDiffs()
    {
        // Arrange
        var baseline = CreateSchema("baseline", "Orders", "Customers");
        var shard = CreateSchema("shard-1", "Orders", "Customers");

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: false);

        // Assert
        diff.ShardId.ShouldBe("shard-1");
        diff.BaselineShardId.ShouldBe("baseline");
        diff.TableDiffs.ShouldBeEmpty();
    }

    [Fact]
    public void Compare_MissingTable_ReturnsMissingDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline", "Orders", "Customers");
        var shard = CreateSchema("shard-1", "Orders");

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: false);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].TableName.ShouldBe("Customers");
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Missing);
    }

    [Fact]
    public void Compare_ExtraTable_ReturnsExtraDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline", "Orders");
        var shard = CreateSchema("shard-1", "Orders", "AuditLog");

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: false);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].TableName.ShouldBe("AuditLog");
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Extra);
    }

    [Fact]
    public void Compare_WithColumnDiffsEnabled_DetectsMissingColumn()
    {
        // Arrange
        var baselineTable = new TableSchema("Orders",
            [new ColumnSchema("Id", "int", false), new ColumnSchema("Status", "varchar", false)]);
        var shardTable = new TableSchema("Orders",
            [new ColumnSchema("Id", "int", false)]);

        var baseline = new ShardSchema("baseline", [baselineTable], Now);
        var shard = new ShardSchema("shard-1", [shardTable], Now);

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Modified);
        diff.TableDiffs[0].ColumnDiffs!.ShouldContain(d => d.Contains("Missing column: Status"));
    }

    [Fact]
    public void Compare_WithColumnDiffsEnabled_DetectsExtraColumn()
    {
        // Arrange
        var baselineTable = new TableSchema("Orders",
            [new ColumnSchema("Id", "int", false)]);
        var shardTable = new TableSchema("Orders",
            [new ColumnSchema("Id", "int", false), new ColumnSchema("Extra", "varchar", true)]);

        var baseline = new ShardSchema("baseline", [baselineTable], Now);
        var shard = new ShardSchema("shard-1", [shardTable], Now);

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].ColumnDiffs!.ShouldContain(d => d.Contains("Extra column: Extra"));
    }

    [Fact]
    public void Compare_WithColumnDiffsEnabled_DetectsTypeDifference()
    {
        // Arrange
        var baselineTable = new TableSchema("Orders",
            [new ColumnSchema("Price", "decimal", false)]);
        var shardTable = new TableSchema("Orders",
            [new ColumnSchema("Price", "float", false)]);

        var baseline = new ShardSchema("baseline", [baselineTable], Now);
        var shard = new ShardSchema("shard-1", [shardTable], Now);

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].ColumnDiffs!.ShouldContain(d => d.Contains("type differs"));
    }

    [Fact]
    public void Compare_WithColumnDiffsEnabled_DetectsNullabilityDifference()
    {
        // Arrange
        var baselineTable = new TableSchema("Orders",
            [new ColumnSchema("Status", "varchar", false)]);
        var shardTable = new TableSchema("Orders",
            [new ColumnSchema("Status", "varchar", true)]);

        var baseline = new ShardSchema("baseline", [baselineTable], Now);
        var shard = new ShardSchema("shard-1", [shardTable], Now);

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].ColumnDiffs!.ShouldContain(d => d.Contains("nullability differs"));
    }

    [Fact]
    public void Compare_WithColumnDiffsDisabled_DoesNotCompareColumns()
    {
        // Arrange
        var baselineTable = new TableSchema("Orders",
            [new ColumnSchema("Id", "int", false)]);
        var shardTable = new TableSchema("Orders",
            [new ColumnSchema("Id", "bigint", true)]);

        var baseline = new ShardSchema("baseline", [baselineTable], Now);
        var shard = new ShardSchema("shard-1", [shardTable], Now);

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: false);

        // Assert
        diff.TableDiffs.ShouldBeEmpty();
    }

    [Fact]
    public void Compare_MissingAndExtraAndModified_ReturnsAllDiffs()
    {
        // Arrange
        var baselineTable = new TableSchema("Shared",
            [new ColumnSchema("Id", "int", false)]);
        var shardModified = new TableSchema("Shared",
            [new ColumnSchema("Id", "bigint", false)]);

        var baseline = new ShardSchema("baseline",
            [baselineTable, new TableSchema("Removed", [new ColumnSchema("Id", "int", false)])], Now);
        var shard = new ShardSchema("shard-1",
            [shardModified, new TableSchema("Added", [new ColumnSchema("Id", "int", false)])], Now);

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(3); // Missing(Removed), Extra(Added), Modified(Shared)
    }

    private static ShardSchema CreateSchema(string shardId, params string[] tableNames)
    {
        var tables = tableNames.Select(name =>
            new TableSchema(name, [new ColumnSchema("Id", "int", false)])).ToList();
        return new ShardSchema(shardId, tables, Now);
    }
}
