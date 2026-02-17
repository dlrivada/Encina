using Encina.Sharding.Migrations;

namespace Encina.GuardTests.Migrations;

/// <summary>
/// Guard clause tests for <see cref="MigrationScript"/> record constructor validation.
/// Each positional property validates non-null/non-whitespace with <see cref="ArgumentException"/>.
/// </summary>
public sealed class MigrationScriptGuardTests
{
    private const string ValidId = "20260216_add_orders_index";
    private const string ValidUpSql = "CREATE INDEX idx_orders_status ON orders (status);";
    private const string ValidDownSql = "DROP INDEX idx_orders_status;";
    private const string ValidDescription = "Add index on orders.status for faster filtering";
    private const string ValidChecksum = "sha256:a1b2c3d4";

    #region Id Guards

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(null!, ValidUpSql, ValidDownSql, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Id");
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(string.Empty, ValidUpSql, ValidDownSql, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Id");
    }

    [Fact]
    public void Constructor_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => new MigrationScript("   ", ValidUpSql, ValidDownSql, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Id");
    }

    #endregion

    #region UpSql Guards

    [Fact]
    public void Constructor_NullUpSql_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, null!, ValidDownSql, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("UpSql");
    }

    [Fact]
    public void Constructor_EmptyUpSql_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, string.Empty, ValidDownSql, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("UpSql");
    }

    #endregion

    #region DownSql Guards

    [Fact]
    public void Constructor_NullDownSql_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, ValidUpSql, null!, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("DownSql");
    }

    [Fact]
    public void Constructor_EmptyDownSql_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, ValidUpSql, string.Empty, ValidDescription, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("DownSql");
    }

    #endregion

    #region Description Guards

    [Fact]
    public void Constructor_NullDescription_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, ValidUpSql, ValidDownSql, null!, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Description");
    }

    [Fact]
    public void Constructor_EmptyDescription_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, ValidUpSql, ValidDownSql, string.Empty, ValidChecksum);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Description");
    }

    #endregion

    #region Checksum Guards

    [Fact]
    public void Constructor_NullChecksum_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, ValidUpSql, ValidDownSql, ValidDescription, null!);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Checksum");
    }

    [Fact]
    public void Constructor_EmptyChecksum_ThrowsArgumentException()
    {
        var act = () => new MigrationScript(ValidId, ValidUpSql, ValidDownSql, ValidDescription, string.Empty);

        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("Checksum");
    }

    #endregion

    #region Happy Path

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var script = new MigrationScript(ValidId, ValidUpSql, ValidDownSql, ValidDescription, ValidChecksum);

        script.Id.ShouldBe(ValidId);
        script.UpSql.ShouldBe(ValidUpSql);
        script.DownSql.ShouldBe(ValidDownSql);
        script.Description.ShouldBe(ValidDescription);
        script.Checksum.ShouldBe(ValidChecksum);
    }

    #endregion
}
