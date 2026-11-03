using System.Data.Common;
using Encina.Testing.Respawn;

namespace Encina.UnitTests.Testing.Respawn;

/// <summary>
/// Verifies that the provider respawners create their provider connection and surface a
/// connection failure, using a loopback port nothing listens on (refused immediately).
/// </summary>
public sealed class RespawnerConnectionFailureTests
{
    private const string SqlServerRefused =
        "Server=127.0.0.1,1;Database=app;User Id=sa;Password=Test@123;Connect Timeout=2;TrustServerCertificate=True;Encrypt=False";

    private const string PostgreSqlRefused =
        "Host=127.0.0.1;Port=1;Database=app;Username=postgres;Password=postgres;Timeout=2";

    private const string MySqlRefused =
        "Server=127.0.0.1;Port=1;Database=app;User=root;Password=mysql;Connection Timeout=2";

    [Fact]
    public async Task SqlServerRespawner_InitializeAsync_WhenServerRefuses_ThrowsProviderException()
    {
        // Arrange
        await using var respawner = new SqlServerRespawner(SqlServerRefused);

        // Act & Assert
        var ex = await Should.ThrowAsync<DbException>(() => respawner.InitializeAsync());
        ex.GetType().Namespace.ShouldBe("Microsoft.Data.SqlClient");
        respawner.GetDeleteCommands().ShouldBeNull();
    }

    [Fact]
    public async Task PostgreSqlRespawner_InitializeAsync_WhenServerRefuses_ThrowsProviderException()
    {
        // Arrange
        await using var respawner = new PostgreSqlRespawner(PostgreSqlRefused);

        // Act & Assert
        var ex = await Should.ThrowAsync<DbException>(() => respawner.InitializeAsync());
        ex.GetType().Namespace.ShouldBe("Npgsql");
        respawner.GetDeleteCommands().ShouldBeNull();
    }

    [Fact]
    public async Task MySqlRespawner_InitializeAsync_WhenServerRefuses_ThrowsProviderException()
    {
        // Arrange
        await using var respawner = new MySqlRespawner(MySqlRefused);

        // Act & Assert
        var ex = await Should.ThrowAsync<DbException>(() => respawner.InitializeAsync());
        ex.GetType().Namespace.ShouldBe("MySqlConnector");
        respawner.GetDeleteCommands().ShouldBeNull();
    }
}
