using System.Diagnostics.CodeAnalysis;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Dapper;

/// <summary>
/// xUnit collection definitions for Dapper provider fixtures.
/// Collections allow fixtures to be shared across test classes,
/// reducing the number of Docker containers from one-per-class to one-per-collection.
/// </summary>

/// <summary>
/// Collection for SQL Server Dapper integration tests.
/// </summary>
[CollectionDefinition("Dapper-SqlServer")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class DapperSqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}

/// <summary>
/// Collection for PostgreSQL Dapper integration tests.
/// </summary>
[CollectionDefinition("Dapper-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class DapperPostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}

/// <summary>
/// Collection for MySQL Dapper integration tests.
/// </summary>
[CollectionDefinition("Dapper-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class DapperMySqlCollection : ICollectionFixture<MySqlFixture>
{
}

/// <summary>
/// Collection for SQLite Dapper integration tests.
/// </summary>
[CollectionDefinition("Dapper-Sqlite", DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class DapperSqliteCollection : ICollectionFixture<SqliteFixture>
{
}
