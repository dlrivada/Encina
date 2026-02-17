using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="ShardSchemaDiff"/> record constructor validation.
/// ShardId and BaselineShardId validate non-null/non-whitespace with <see cref="ArgumentException"/>.
/// </summary>
public sealed class ShardSchemaDiffGuardTests
{
    private static readonly IReadOnlyList<TableDiff> EmptyDiffs = [];

    #region ShardId Guards

    [Fact]
    public void Constructor_NullShardId_ThrowsArgumentException()
    {
        var act = () => new ShardSchemaDiff(null!, "baseline-shard", EmptyDiffs);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("ShardId");
    }

    [Fact]
    public void Constructor_EmptyShardId_ThrowsArgumentException()
    {
        var act = () => new ShardSchemaDiff(string.Empty, "baseline-shard", EmptyDiffs);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("ShardId");
    }

    [Fact]
    public void Constructor_WhitespaceShardId_ThrowsArgumentException()
    {
        var act = () => new ShardSchemaDiff("   ", "baseline-shard", EmptyDiffs);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("ShardId");
    }

    #endregion

    #region BaselineShardId Guards

    [Fact]
    public void Constructor_NullBaselineShardId_ThrowsArgumentException()
    {
        var act = () => new ShardSchemaDiff("shard-1", null!, EmptyDiffs);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("BaselineShardId");
    }

    [Fact]
    public void Constructor_EmptyBaselineShardId_ThrowsArgumentException()
    {
        var act = () => new ShardSchemaDiff("shard-1", string.Empty, EmptyDiffs);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("BaselineShardId");
    }

    [Fact]
    public void Constructor_WhitespaceBaselineShardId_ThrowsArgumentException()
    {
        var act = () => new ShardSchemaDiff("shard-1", "   ", EmptyDiffs);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("BaselineShardId");
    }

    #endregion

    #region Happy Path

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var tableDiffs = new List<TableDiff>
        {
            new("orders", TableDiffType.Modified, ["status column type changed"])
        };

        var diff = new ShardSchemaDiff("shard-1", "shard-0", tableDiffs);

        diff.ShardId.ShouldBe("shard-1");
        diff.BaselineShardId.ShouldBe("shard-0");
        diff.TableDiffs.Count.ShouldBe(1);
    }

    #endregion
}
