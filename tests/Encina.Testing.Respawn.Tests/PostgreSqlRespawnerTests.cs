using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.Testing.Respawn.Tests;

/// <summary>
/// Unit tests for <see cref="PostgreSqlRespawner"/>.
/// </summary>
public sealed class PostgreSqlRespawnerTests
{
    private const string ValidConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres";

    [Fact]
    public void Constructor_WithValidConnectionString_Succeeds()
    {
        // Arrange & Act
        var respawner = new PostgreSqlRespawner(ValidConnectionString);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new PostgreSqlRespawner(null!));
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new PostgreSqlRespawner(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new PostgreSqlRespawner("   "));
    }

    [Fact]
    public void Options_DefaultValue_IsNewRespawnOptions()
    {
        // Arrange
        var respawner = new PostgreSqlRespawner(ValidConnectionString);

        // Act & Assert
        respawner.Options.ShouldNotBeNull();
        respawner.Options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    [Fact]
    public void Options_CanBeModified()
    {
        // Arrange
        var respawner = new PostgreSqlRespawner(ValidConnectionString);
        var newOptions = new EncinaRespawnOptions
        {
            SchemasToInclude = ["public"],
            ResetEncinaMessagingTables = false
        };

        // Act
        respawner.Options = newOptions;

        // Assert
        respawner.Options.SchemasToInclude.ShouldContain("public");
        respawner.Options.ResetEncinaMessagingTables.ShouldBeFalse();
    }

    [Fact]
    public void GetDeleteCommands_BeforeInitialization_ReturnsNull()
    {
        // Arrange
        var respawner = new PostgreSqlRespawner(ValidConnectionString);

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldBeNull();
    }

    [Fact]
    public void FromBuilder_WithValidBuilder_CreatesRespawner()
    {
        // Arrange
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Database = "testdb",
            Username = "postgres",
            Password = "postgres"
        };

        // Act
        var respawner = PostgreSqlRespawner.FromBuilder(builder);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void FromBuilder_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            PostgreSqlRespawner.FromBuilder(null!));
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var respawner = new PostgreSqlRespawner(ValidConnectionString);

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(async () =>
        {
            await respawner.DisposeAsync();
            await respawner.DisposeAsync();
        });
        Assert.Null(exception);
    }
}
