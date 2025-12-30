using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.Testing.Respawn.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaRespawnOptions"/>.
/// </summary>
public sealed class RespawnOptionsTests
{
    [Fact]
    public void DefaultOptions_TablesToIgnore_IsEmpty()
    {
        // Arrange & Act
        var options = new EncinaRespawnOptions();

        // Assert
        options.TablesToIgnore.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultOptions_SchemasToInclude_IsEmpty()
    {
        // Arrange & Act
        var options = new EncinaRespawnOptions();

        // Assert
        options.SchemasToInclude.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultOptions_SchemasToExclude_IsEmpty()
    {
        // Arrange & Act
        var options = new EncinaRespawnOptions();

        // Assert
        options.SchemasToExclude.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultOptions_ResetEncinaMessagingTables_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaRespawnOptions();

        // Assert
        options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    [Fact]
    public void DefaultOptions_CheckTemporalTables_IsFalse()
    {
        // Arrange & Act
        var options = new EncinaRespawnOptions();

        // Assert
        options.CheckTemporalTables.ShouldBeFalse();
    }

    [Fact]
    public void DefaultOptions_WithReseed_IsTrue()
    {
        // Arrange & Act
        var options = new EncinaRespawnOptions();

        // Assert
        options.WithReseed.ShouldBeTrue();
    }

    [Fact]
    public void EncinaMessagingTables_ContainsAllExpectedTables()
    {
        // Arrange
        var expectedTables = new[] { "OutboxMessages", "InboxMessages", "SagaStates", "ScheduledMessages" };

        // Act
        var tables = EncinaRespawnOptions.EncinaMessagingTables;

        // Assert
        tables.Length.ShouldBe(4);
        tables.ShouldBe(expectedTables, ignoreOrder: true);
    }

    [Fact]
    public void TablesToIgnore_CanBeSet()
    {
        // Arrange
        var options = new EncinaRespawnOptions();

        // Act
        options.TablesToIgnore = ["__EFMigrationsHistory", "AuditLog"];

        // Assert
        options.TablesToIgnore.Length.ShouldBe(2);
        options.TablesToIgnore.ShouldContain("__EFMigrationsHistory");
        options.TablesToIgnore.ShouldContain("AuditLog");
    }

    [Fact]
    public void SchemasToInclude_CanBeSet()
    {
        // Arrange
        var options = new EncinaRespawnOptions();

        // Act
        options.SchemasToInclude = ["dbo", "public"];

        // Assert
        options.SchemasToInclude.Length.ShouldBe(2);
        options.SchemasToInclude.ShouldContain("dbo");
        options.SchemasToInclude.ShouldContain("public");
    }

    [Fact]
    public void SchemasToExclude_CanBeSet()
    {
        // Arrange
        var options = new EncinaRespawnOptions();

        // Act
        options.SchemasToExclude = ["sys", "information_schema"];

        // Assert
        options.SchemasToExclude.Length.ShouldBe(2);
        options.SchemasToExclude.ShouldContain("sys");
        options.SchemasToExclude.ShouldContain("information_schema");
    }
}
