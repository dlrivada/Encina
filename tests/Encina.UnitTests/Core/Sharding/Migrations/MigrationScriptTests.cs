using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Core.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationScript"/> record validation.
/// </summary>
public sealed class MigrationScriptTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesScript()
    {
        // Act
        var script = new MigrationScript(
            "migration-001",
            "CREATE TABLE Test (Id INT);",
            "DROP TABLE Test;",
            "Add test table",
            "sha256:abc123");

        // Assert
        script.Id.ShouldBe("migration-001");
        script.UpSql.ShouldBe("CREATE TABLE Test (Id INT);");
        script.DownSql.ShouldBe("DROP TABLE Test;");
        script.Description.ShouldBe("Add test table");
        script.Checksum.ShouldBe("sha256:abc123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidId_ThrowsArgumentException(string? id)
    {
        Assert.Throws<ArgumentException>(() =>
            new MigrationScript(id!, "UP", "DOWN", "Desc", "checksum"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidUpSql_ThrowsArgumentException(string? upSql)
    {
        Assert.Throws<ArgumentException>(() =>
            new MigrationScript("id", upSql!, "DOWN", "Desc", "checksum"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDownSql_ThrowsArgumentException(string? downSql)
    {
        Assert.Throws<ArgumentException>(() =>
            new MigrationScript("id", "UP", downSql!, "Desc", "checksum"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDescription_ThrowsArgumentException(string? description)
    {
        Assert.Throws<ArgumentException>(() =>
            new MigrationScript("id", "UP", "DOWN", description!, "checksum"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidChecksum_ThrowsArgumentException(string? checksum)
    {
        Assert.Throws<ArgumentException>(() =>
            new MigrationScript("id", "UP", "DOWN", "Desc", checksum!));
    }
}
