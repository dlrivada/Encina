using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="TableDiff"/> record constructor validation.
/// TableName validates non-null/non-whitespace with <see cref="ArgumentException"/>.
/// </summary>
public sealed class TableDiffGuardTests
{
    [Fact]
    public void Constructor_NullTableName_ThrowsArgumentException()
    {
        var act = () => new TableDiff(null!, TableDiffType.Missing);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("TableName");
    }

    [Fact]
    public void Constructor_EmptyTableName_ThrowsArgumentException()
    {
        var act = () => new TableDiff(string.Empty, TableDiffType.Missing);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("TableName");
    }

    [Fact]
    public void Constructor_WhitespaceTableName_ThrowsArgumentException()
    {
        var act = () => new TableDiff("   ", TableDiffType.Missing);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("TableName");
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var diff = new TableDiff("orders", TableDiffType.Modified, ["column_a added"]);

        diff.TableName.ShouldBe("orders");
        diff.DiffType.ShouldBe(TableDiffType.Modified);
        diff.ColumnDiffs.ShouldNotBeNull();
        diff.ColumnDiffs!.Count.ShouldBe(1);
    }

    [Fact]
    public void Constructor_ValidParametersWithNullColumnDiffs_CreatesInstance()
    {
        var diff = new TableDiff("users", TableDiffType.Missing);

        diff.TableName.ShouldBe("users");
        diff.DiffType.ShouldBe(TableDiffType.Missing);
        diff.ColumnDiffs.ShouldBeNull();
    }
}
