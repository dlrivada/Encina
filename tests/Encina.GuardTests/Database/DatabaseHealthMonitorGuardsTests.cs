using Encina.ADO.MySQL.Health;
using Encina.ADO.PostgreSQL.Health;
using Encina.ADO.Sqlite.Health;
using Encina.ADO.SqlServer.Health;
using Encina.Dapper.MySQL.Health;
using Encina.Dapper.PostgreSQL.Health;
using Encina.Dapper.Sqlite.Health;
using Encina.Dapper.SqlServer.Health;
using Encina.Database;
using Encina.EntityFrameworkCore.Resilience;
using Encina.MongoDB.Health;

using Shouldly;

namespace Encina.GuardTests.Database;

/// <summary>
/// Guard clause tests for all <see cref="IDatabaseHealthMonitor"/> implementations.
/// Verifies null argument validation on constructors for all 10 concrete monitors
/// (4 ADO.NET, 4 Dapper, 1 EF Core, 1 MongoDB).
/// </summary>
public sealed class DatabaseHealthMonitorGuardsTests
{
    #region ADO.NET Providers

    [Fact]
    public void SqliteDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new SqliteDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    [Fact]
    public void SqlServerDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new SqlServerDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    [Fact]
    public void PostgreSqlDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new PostgreSqlDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    [Fact]
    public void MySqlDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new MySqlDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    #endregion

    #region Dapper Providers

    [Fact]
    public void DapperSqliteDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DapperSqliteDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    [Fact]
    public void DapperSqlServerDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DapperSqlServerDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    [Fact]
    public void DapperPostgreSqlDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DapperPostgreSqlDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    [Fact]
    public void DapperMySqlDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new DapperMySqlDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    #endregion

    #region EF Core

    [Fact]
    public void EfCoreDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new EfCoreDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    #endregion

    #region MongoDB

    [Fact]
    public void MongoDbDatabaseHealthMonitor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new MongoDbDatabaseHealthMonitor(null!));
        ex.ParamName.ShouldNotBeNull();
    }

    #endregion
}
