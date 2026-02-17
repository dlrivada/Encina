using Encina.Sharding.Migrations;

namespace Encina.ContractTests.Migrations;

/// <summary>
/// Contract tests verifying that <see cref="MigrationScript"/> behaves as a value object
/// with proper record equality semantics.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MigrationScriptContracts
{
    [Fact]
    public void MigrationScript_Equality_ByValueNotReference()
    {
        // Arrange
        var scriptA = new MigrationScript(
            Id: "20260216_add_index",
            UpSql: "CREATE INDEX idx_orders ON orders (status);",
            DownSql: "DROP INDEX idx_orders;",
            Description: "Add index on orders.status",
            Checksum: "sha256:abc123");

        var scriptB = new MigrationScript(
            Id: "20260216_add_index",
            UpSql: "CREATE INDEX idx_orders ON orders (status);",
            DownSql: "DROP INDEX idx_orders;",
            Description: "Add index on orders.status",
            Checksum: "sha256:abc123");

        // Act & Assert — record equality is by value
        scriptA.ShouldBe(scriptB);
        (scriptA == scriptB).ShouldBeTrue();
        ReferenceEquals(scriptA, scriptB).ShouldBeFalse();
    }

    [Fact]
    public void MigrationScript_Inequality_DifferentId()
    {
        // Arrange
        var scriptA = new MigrationScript(
            Id: "20260216_add_index",
            UpSql: "CREATE INDEX idx_orders ON orders (status);",
            DownSql: "DROP INDEX idx_orders;",
            Description: "Add index on orders.status",
            Checksum: "sha256:abc123");

        var scriptB = new MigrationScript(
            Id: "20260217_add_other_index",
            UpSql: "CREATE INDEX idx_orders ON orders (status);",
            DownSql: "DROP INDEX idx_orders;",
            Description: "Add index on orders.status",
            Checksum: "sha256:abc123");

        // Act & Assert
        scriptA.ShouldNotBe(scriptB);
        (scriptA != scriptB).ShouldBeTrue();
    }

    [Fact]
    public void MigrationScript_ToString_ContainsId()
    {
        // Arrange
        var script = new MigrationScript(
            Id: "20260216_add_orders_index",
            UpSql: "CREATE INDEX idx_orders ON orders (status);",
            DownSql: "DROP INDEX idx_orders;",
            Description: "Add index on orders.status",
            Checksum: "sha256:abc123");

        // Act
        var stringRepresentation = script.ToString();

        // Assert — record ToString includes property values
        stringRepresentation.ShouldNotBeNull();
        stringRepresentation.ShouldContain("20260216_add_orders_index");
    }
}
