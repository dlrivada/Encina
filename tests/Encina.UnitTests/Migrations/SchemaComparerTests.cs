using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Migrations;

/// <summary>
/// Unit tests for <see cref="SchemaComparer"/>.
/// Verifies table-level and column-level schema comparison logic.
/// </summary>
public sealed class SchemaComparerTests
{
    #region Test Helpers

    private static ShardSchema CreateSchema(string shardId, params TableSchema[] tables)
        => new(shardId, tables, DateTimeOffset.UtcNow);

    private static TableSchema CreateTable(string name, params ColumnSchema[] columns)
        => new(name, columns);

    private static ColumnSchema CreateColumn(
        string name,
        string dataType = "int",
        bool isNullable = false,
        string? defaultValue = null)
        => new(name, dataType, isNullable, defaultValue);

    #endregion

    #region Compare - Identical Schemas

    [Fact]
    public void Compare_WithIdenticalSchemas_ReturnsEmptyDiffs()
    {
        // Arrange
        var columns = new[] { CreateColumn("Id"), CreateColumn("Name", "nvarchar(255)", true) };
        var baseline = CreateSchema("baseline", CreateTable("Orders", columns));
        var shard = CreateSchema("shard-1", CreateTable("Orders", columns));

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.ShardId.ShouldBe("shard-1");
        diff.BaselineShardId.ShouldBe("baseline");
        diff.TableDiffs.Count.ShouldBe(0);
    }

    #endregion

    #region Compare - Missing Tables

    [Fact]
    public void Compare_WithMissingTable_ReturnsMissingDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline",
            CreateTable("Orders", CreateColumn("Id")),
            CreateTable("Customers", CreateColumn("Id")));
        var shard = CreateSchema("shard-1",
            CreateTable("Orders", CreateColumn("Id")));

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].TableName.ShouldBe("Customers");
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Missing);
    }

    #endregion

    #region Compare - Extra Tables

    [Fact]
    public void Compare_WithExtraTable_ReturnsExtraDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline",
            CreateTable("Orders", CreateColumn("Id")));
        var shard = CreateSchema("shard-1",
            CreateTable("Orders", CreateColumn("Id")),
            CreateTable("TempData", CreateColumn("Id")));

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].TableName.ShouldBe("TempData");
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Extra);
    }

    #endregion

    #region Compare - Modified Columns

    [Fact]
    public void Compare_WithModifiedColumn_ReturnsModifiedDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline",
            CreateTable("Orders",
                CreateColumn("Id", "int"),
                CreateColumn("Status", "nvarchar(50)", false)));

        var shard = CreateSchema("shard-1",
            CreateTable("Orders",
                CreateColumn("Id", "int"),
                CreateColumn("Status", "nvarchar(100)", false))); // different type

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].TableName.ShouldBe("Orders");
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Modified);
        diff.TableDiffs[0].ColumnDiffs.ShouldNotBeNull();
        diff.TableDiffs[0].ColumnDiffs!.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Compare_WithColumnTypeDifference_ReportsTypeDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline",
            CreateTable("Products", CreateColumn("Price", "decimal(18,2)")));
        var shard = CreateSchema("shard-1",
            CreateTable("Products", CreateColumn("Price", "float")));

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Modified);
        diff.TableDiffs[0].ColumnDiffs.ShouldNotBeNull();
        diff.TableDiffs[0].ColumnDiffs!.ShouldContain(d => d.Contains("type differs"));
    }

    [Fact]
    public void Compare_WithColumnNullabilityDifference_ReportsNullabilityDiff()
    {
        // Arrange
        var baseline = CreateSchema("baseline",
            CreateTable("Products", CreateColumn("Name", "nvarchar(255)", false)));
        var shard = CreateSchema("shard-1",
            CreateTable("Products", CreateColumn("Name", "nvarchar(255)", true)));

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: true);

        // Assert
        diff.TableDiffs.Count.ShouldBe(1);
        diff.TableDiffs[0].DiffType.ShouldBe(TableDiffType.Modified);
        diff.TableDiffs[0].ColumnDiffs.ShouldNotBeNull();
        diff.TableDiffs[0].ColumnDiffs!.ShouldContain(d => d.Contains("nullability differs"));
    }

    #endregion

    #region Compare - Without Column Diffs

    [Fact]
    public void Compare_WithoutColumnDiffs_OnlyReportsTableLevelChanges()
    {
        // Arrange
        var baseline = CreateSchema("baseline",
            CreateTable("Orders",
                CreateColumn("Id", "int"),
                CreateColumn("Status", "nvarchar(50)")));

        var shard = CreateSchema("shard-1",
            CreateTable("Orders",
                CreateColumn("Id", "int"),
                CreateColumn("Status", "nvarchar(100)"))); // different type, but columnDiffs disabled

        // Act
        var diff = SchemaComparer.Compare("shard-1", "baseline", shard, baseline, includeColumnDiffs: false);

        // Assert
        // Tables exist in both, and column diffs are disabled, so no Modified diffs should be reported
        diff.TableDiffs.Count.ShouldBe(0);
    }

    #endregion
}
