using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.Testing.Respawn.Tests;

/// <summary>
/// Unit tests for <see cref="RespawnerFactory"/>.
/// </summary>
public sealed class RespawnerFactoryTests
{
    #region Create Tests

    [Fact]
    public void Create_SqlServer_ReturnsSqlServerRespawner()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=Test@123";

        // Act
        var respawner = RespawnerFactory.Create(RespawnAdapter.SqlServer, connectionString);

        // Assert
        respawner.ShouldBeOfType<SqlServerRespawner>();
    }

    [Fact]
    public void Create_PostgreSql_ReturnsPostgreSqlRespawner()
    {
        // Arrange
        var connectionString = "Host=localhost;Database=testdb;Username=postgres;Password=postgres";

        // Act
        var respawner = RespawnerFactory.Create(RespawnAdapter.PostgreSql, connectionString);

        // Assert
        respawner.ShouldBeOfType<PostgreSqlRespawner>();
    }

    [Fact]
    public void Create_MySql_ReturnsMySqlRespawner()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=testdb;User=root;Password=mysql";

        // Act
        var respawner = RespawnerFactory.Create(RespawnAdapter.MySql, connectionString);

        // Assert
        respawner.ShouldBeOfType<MySqlRespawner>();
    }

    [Fact]
    public void Create_Oracle_ThrowsNotSupportedException()
    {
        // Arrange
        var connectionString = "Data Source=localhost:1521/ORCLPDB;User Id=system;Password=oracle";

        // Act & Assert
        var exception = Should.Throw<NotSupportedException>(() =>
            RespawnerFactory.Create(RespawnAdapter.Oracle, connectionString));

        exception.Message.ShouldContain("Oracle");
    }

    [Fact]
    public void Create_WithOptions_AppliesOptions()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=Test@123";
        var options = new EncinaRespawnOptions
        {
            TablesToIgnore = ["IgnoredTable"],
            ResetEncinaMessagingTables = false
        };

        // Act
        var respawner = RespawnerFactory.Create(RespawnAdapter.SqlServer, connectionString, options);

        // Assert
        respawner.Options.TablesToIgnore.ShouldContain("IgnoredTable");
        respawner.Options.ResetEncinaMessagingTables.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=Test@123";

        // Act
        var respawner = RespawnerFactory.Create(RespawnAdapter.SqlServer, connectionString, null);

        // Assert
        respawner.Options.ShouldNotBeNull();
        respawner.Options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    #endregion

    #region CreateSqlite Tests

    [Fact]
    public void CreateSqlite_ReturnsSqliteRespawner()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";

        // Act
        var respawner = RespawnerFactory.CreateSqlite(connectionString);

        // Assert
        respawner.ShouldBeOfType<SqliteRespawner>();
    }

    [Fact]
    public void CreateSqlite_WithOptions_AppliesOptions()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";
        var options = new EncinaRespawnOptions
        {
            TablesToIgnore = ["IgnoredTable"],
            WithReseed = false
        };

        // Act
        var respawner = RespawnerFactory.CreateSqlite(connectionString, options);

        // Assert
        respawner.Options.TablesToIgnore.ShouldContain("IgnoredTable");
        respawner.Options.WithReseed.ShouldBeFalse();
    }

    [Fact]
    public void CreateSqlite_WithNullOptions_UsesDefaultOptions()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";

        // Act
        var respawner = RespawnerFactory.CreateSqlite(connectionString, null);

        // Assert
        respawner.Options.ShouldNotBeNull();
        respawner.Options.ResetEncinaMessagingTables.ShouldBeTrue();
    }

    #endregion

    #region InferAdapter Tests

    [Theory]
    [InlineData("Server=localhost;Database=TestDb;User Id=sa;Password=Test@123", RespawnAdapter.SqlServer)]
    [InlineData("Server=localhost;Initial Catalog=TestDb;User Id=sa;Password=Test@123", RespawnAdapter.SqlServer)]
    [InlineData("Data Source=localhost;Initial Catalog=TestDb;Integrated Security=True", RespawnAdapter.SqlServer)]
    public void InferAdapter_SqlServerConnectionStrings_ReturnsSqlServer(string connectionString, RespawnAdapter expected)
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter(connectionString);

        // Assert
        adapter.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Host=localhost;Database=testdb;Username=postgres;Password=postgres", RespawnAdapter.PostgreSql)]
    [InlineData("Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=postgres", RespawnAdapter.PostgreSql)]
    public void InferAdapter_PostgreSqlConnectionStrings_ReturnsPostgreSql(string connectionString, RespawnAdapter expected)
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter(connectionString);

        // Assert
        adapter.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Server=localhost;Port=3306;Database=testdb;User=root;Password=mysql", RespawnAdapter.MySql)]
    [InlineData("Server=mysql-server;Database=testdb;User=root;Password=mysql", RespawnAdapter.MySql)]
    public void InferAdapter_MySqlConnectionStrings_ReturnsMySql(string connectionString, RespawnAdapter expected)
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter(connectionString);

        // Assert
        adapter.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=test.db")]
    [InlineData("some random string")]
    public void InferAdapter_UnrecognizedConnectionStrings_ReturnsNull(string connectionString)
    {
        // Act
        var adapter = RespawnerFactory.InferAdapter(connectionString);

        // Assert
        adapter.ShouldBeNull();
    }

    [Fact]
    public void InferAdapter_NullConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RespawnerFactory.InferAdapter(null!));
    }

    [Fact]
    public void InferAdapter_EmptyConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RespawnerFactory.InferAdapter(string.Empty));
    }

    [Fact]
    public void InferAdapter_WhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => RespawnerFactory.InferAdapter("   "));
    }

    #endregion
}
