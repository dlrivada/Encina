using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.Testing.Respawn.Tests;

/// <summary>
/// Unit tests for <see cref="SqliteRespawner"/>.
/// </summary>
public sealed class SqliteRespawnerTests
{
    private const string ValidConnectionString = "Data Source=:memory:";

    [Fact]
    public void Constructor_WithValidConnectionString_Succeeds()
    {
        // Arrange & Act
        var respawner = new SqliteRespawner(ValidConnectionString);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new SqliteRespawner(null!));
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new SqliteRespawner(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new SqliteRespawner("   "));
    }

    [Fact]
    public void Options_DefaultValue_IsNewRespawnOptions()
    {
        // Arrange
        var respawner = new SqliteRespawner(ValidConnectionString);

        // Act & Assert
        respawner.Options.ShouldNotBeNull();
        respawner.Options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    [Fact]
    public void Options_CanBeModified()
    {
        // Arrange
        var respawner = new SqliteRespawner(ValidConnectionString);
        var newOptions = new EncinaRespawnOptions
        {
            TablesToIgnore = ["MigrationHistory"],
            ResetEncinaMessagingTables = false
        };

        // Act
        respawner.Options = newOptions;

        // Assert
        respawner.Options.TablesToIgnore.ShouldContain("MigrationHistory");
        respawner.Options.ResetEncinaMessagingTables.ShouldBeFalse();
    }

    [Fact]
    public void GetDeleteCommands_BeforeInitialization_ReturnsNull()
    {
        // Arrange
        var respawner = new SqliteRespawner(ValidConnectionString);

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldBeNull();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var respawner = new SqliteRespawner(ValidConnectionString);

        // Act & Assert - should not throw
        await respawner.DisposeAsync();
        await respawner.DisposeAsync();
    }
}
