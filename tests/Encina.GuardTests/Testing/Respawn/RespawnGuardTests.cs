using Encina.Testing.Respawn;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Encina.GuardTests.Testing.Respawn;

/// <summary>
/// Guard tests for the public entry points of Encina.Testing.Respawn: every respawner
/// constructor, every <c>FromBuilder</c> factory and <see cref="RespawnerFactory.InferAdapter"/>.
/// </summary>
public class RespawnGuardTests
{
    #region Constructors

    [Fact]
    public void SqlServerRespawner_NullConnectionString_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new SqlServerRespawner(null!));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SqlServerRespawner_BlankConnectionString_ThrowsArgumentException(string connectionString)
    {
        var ex = Should.Throw<ArgumentException>(() => new SqlServerRespawner(connectionString));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Fact]
    public void PostgreSqlRespawner_NullConnectionString_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new PostgreSqlRespawner(null!));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PostgreSqlRespawner_BlankConnectionString_ThrowsArgumentException(string connectionString)
    {
        var ex = Should.Throw<ArgumentException>(() => new PostgreSqlRespawner(connectionString));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Fact]
    public void MySqlRespawner_NullConnectionString_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new MySqlRespawner(null!));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MySqlRespawner_BlankConnectionString_ThrowsArgumentException(string connectionString)
    {
        var ex = Should.Throw<ArgumentException>(() => new MySqlRespawner(connectionString));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Fact]
    public void SqliteRespawner_NullConnectionString_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new SqliteRespawner(null!));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SqliteRespawner_BlankConnectionString_ThrowsArgumentException(string connectionString)
    {
        var ex = Should.Throw<ArgumentException>(() => new SqliteRespawner(connectionString));
        ex.ParamName.ShouldBe("connectionString");
    }

    #endregion

    #region FromBuilder

    [Fact]
    public void SqlServerRespawner_FromBuilder_NullBuilder_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => SqlServerRespawner.FromBuilder(null!));
        ex.ParamName.ShouldBe("builder");
    }

    [Fact]
    public void PostgreSqlRespawner_FromBuilder_NullBuilder_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => PostgreSqlRespawner.FromBuilder(null!));
        ex.ParamName.ShouldBe("builder");
    }

    [Fact]
    public void MySqlRespawner_FromBuilder_NullBuilder_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => MySqlRespawner.FromBuilder(null!));
        ex.ParamName.ShouldBe("builder");
    }

    [Fact]
    public void SqlServerRespawner_FromBuilder_ValidBuilder_DoesNotThrow()
    {
        var builder = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = "db" };

        Should.NotThrow(() => SqlServerRespawner.FromBuilder(builder));
    }

    [Fact]
    public void PostgreSqlRespawner_FromBuilder_ValidBuilder_DoesNotThrow()
    {
        var builder = new NpgsqlConnectionStringBuilder { Host = "localhost", Database = "db" };

        Should.NotThrow(() => PostgreSqlRespawner.FromBuilder(builder));
    }

    [Fact]
    public void MySqlRespawner_FromBuilder_ValidBuilder_DoesNotThrow()
    {
        var builder = new MySqlConnectionStringBuilder { Server = "localhost", Database = "db" };

        Should.NotThrow(() => MySqlRespawner.FromBuilder(builder));
    }

    #endregion

    #region RespawnerFactory

    [Fact]
    public void InferAdapter_NullConnectionString_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => RespawnerFactory.InferAdapter(null!));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void InferAdapter_BlankConnectionString_ThrowsArgumentException(string connectionString)
    {
        var ex = Should.Throw<ArgumentException>(() => RespawnerFactory.InferAdapter(connectionString));
        ex.ParamName.ShouldBe("connectionString");
    }

    [Fact]
    public void Create_NullConnectionString_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => RespawnerFactory.Create(RespawnAdapter.SqlServer, null!));
    }

    [Fact]
    public void Create_BlankConnectionString_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => RespawnerFactory.Create(RespawnAdapter.PostgreSql, "  "));
    }

    [Fact]
    public void CreateSqlite_NullConnectionString_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => RespawnerFactory.CreateSqlite(null!));
    }

    [Fact]
    public void CreateSqlite_BlankConnectionString_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => RespawnerFactory.CreateSqlite(string.Empty));
    }

    [Fact]
    public void Create_OracleAdapter_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() => RespawnerFactory.Create(RespawnAdapter.Oracle, "Data Source=db"));
    }

    [Fact]
    public void Create_UndefinedAdapter_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() => RespawnerFactory.Create((RespawnAdapter)999, "Data Source=db"));
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task DatabaseRespawner_InitializeAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        await using var respawner = new SqlServerRespawner("Server=localhost;Database=db;User Id=sa;Password=x");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => respawner.InitializeAsync(cts.Token));
    }

    [Fact]
    public async Task DatabaseRespawner_ResetAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        await using var respawner = new PostgreSqlRespawner("Host=localhost;Database=db;Username=u;Password=x");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => respawner.ResetAsync(cts.Token));
    }

    [Fact]
    public async Task SqliteRespawner_InitializeAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        await using var respawner = new SqliteRespawner("Data Source=:memory:");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => respawner.InitializeAsync(cts.Token));
    }

    [Fact]
    public async Task SqliteRespawner_ResetAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        await using var respawner = new SqliteRespawner("Data Source=:memory:");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => respawner.ResetAsync(cts.Token));
    }

    #endregion
}
