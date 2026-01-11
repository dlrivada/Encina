using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.Testing.Respawn.Tests;

/// <summary>
/// Unit tests for <see cref="SqlServerRespawner"/>.
/// </summary>
public sealed class SqlServerRespawnerTests
{
    private const string ValidConnectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=Test@123;TrustServerCertificate=True";

    [Fact]
    public void Constructor_WithValidConnectionString_Succeeds()
    {
        // Arrange & Act
        var respawner = new SqlServerRespawner(ValidConnectionString);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new SqlServerRespawner(null!));
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new SqlServerRespawner(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentException>(() => new SqlServerRespawner("   "));
    }

    [Fact]
    public void Options_DefaultValue_IsNewRespawnOptions()
    {
        // Arrange
        var respawner = new SqlServerRespawner(ValidConnectionString);

        // Act & Assert
        respawner.Options.ShouldNotBeNull();
        respawner.Options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    [Fact]
    public void Options_CanBeModified()
    {
        // Arrange
        var respawner = new SqlServerRespawner(ValidConnectionString);
        var newOptions = new EncinaRespawnOptions
        {
            TablesToIgnore = ["TestTable"],
            ResetEncinaMessagingTables = false
        };

        // Act
        respawner.Options = newOptions;

        // Assert
        respawner.Options.TablesToIgnore.ShouldContain("TestTable");
        respawner.Options.ResetEncinaMessagingTables.ShouldBeFalse();
    }

    [Fact]
    public void GetDeleteCommands_BeforeInitialization_ReturnsNull()
    {
        // Arrange
        var respawner = new SqlServerRespawner(ValidConnectionString);

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldBeNull();
    }

    [Fact]
    public void FromBuilder_WithValidBuilder_CreatesRespawner()
    {
        // Arrange
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = "localhost",
            InitialCatalog = "TestDb",
            UserID = "sa",
            Password = "Test@123"
        };

        // Act
        var respawner = SqlServerRespawner.FromBuilder(builder);

        // Assert
        respawner.ShouldNotBeNull();
    }

    [Fact]
    public void FromBuilder_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            SqlServerRespawner.FromBuilder(null!));
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var respawner = new SqlServerRespawner(ValidConnectionString);

        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(async () =>
        {
            await respawner.DisposeAsync();
            await respawner.DisposeAsync();
        });
        Assert.Null(exception);
    }
}
