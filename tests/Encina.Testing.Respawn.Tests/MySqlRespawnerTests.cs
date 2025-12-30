using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.Testing.Respawn.Tests;

/// <summary>
/// Unit tests for <see cref="MySqlRespawner"/>.
/// </summary>
public sealed class MySqlRespawnerTests
{
    private const string ValidConnectionString = "Server=localhost;Database=testdb;User=root;Password=mysql";

    [Fact]
    public void Constructor_WithValidConnectionString_Succeeds()
    {
        // Arrange & Act
        var respawner = new MySqlRespawner(ValidConnectionString);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new MySqlRespawner(null!));
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new MySqlRespawner(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new MySqlRespawner("   "));
    }

    [Fact]
    public void Options_DefaultValue_IsNewRespawnOptions()
    {
        // Arrange
        var respawner = new MySqlRespawner(ValidConnectionString);

        // Act & Assert
        respawner.Options.ShouldNotBeNull();
        respawner.Options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    [Fact]
    public void Options_CanBeModified()
    {
        // Arrange
        var respawner = new MySqlRespawner(ValidConnectionString);
        var newOptions = new EncinaRespawnOptions
        {
            TablesToIgnore = ["AuditLog"],
            WithReseed = false
        };

        // Act
        respawner.Options = newOptions;

        // Assert
        respawner.Options.TablesToIgnore.ShouldContain("AuditLog");
        respawner.Options.WithReseed.ShouldBeFalse();
    }

    [Fact]
    public void GetDeleteCommands_BeforeInitialization_ReturnsNull()
    {
        // Arrange
        var respawner = new MySqlRespawner(ValidConnectionString);

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldBeNull();
    }

    [Fact]
    public void FromBuilder_WithValidBuilder_CreatesRespawner()
    {
        // Arrange
        var builder = new MySqlConnector.MySqlConnectionStringBuilder
        {
            Server = "localhost",
            Database = "testdb",
            UserID = "root",
            Password = "mysql"
        };

        // Act
        var respawner = MySqlRespawner.FromBuilder(builder);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void FromBuilder_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            MySqlRespawner.FromBuilder(null!));
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var respawner = new MySqlRespawner(ValidConnectionString);

        // Act & Assert - should not throw
        await respawner.DisposeAsync();
        await respawner.DisposeAsync();
    }
}
