using System.Diagnostics.CodeAnalysis;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.ADO;

/// <summary>
/// xUnit collection definitions for ADO.NET provider fixtures.
/// Collections allow fixtures to be shared across test classes,
/// reducing the number of Docker containers from one-per-class to one-per-collection.
/// </summary>

/// <summary>
/// Collection for SQL Server ADO.NET integration tests.
/// </summary>
[CollectionDefinition("ADO-SqlServer")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ADOSqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}

/// <summary>
/// Collection for PostgreSQL ADO.NET integration tests.
/// </summary>
[CollectionDefinition("ADO-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ADOPostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}

/// <summary>
/// Collection for MySQL ADO.NET integration tests.
/// </summary>
[CollectionDefinition("ADO-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ADOMySqlCollection : ICollectionFixture<MySqlFixture>
{
}

/// <summary>
/// Collection for SQLite ADO.NET integration tests.
/// </summary>
[CollectionDefinition("ADO-Sqlite", DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ADOSqliteCollection : ICollectionFixture<SqliteFixture>
{
}
