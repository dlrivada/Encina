using System.Diagnostics.CodeAnalysis;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// xUnit collection definitions for EF Core provider fixtures.
/// Collections allow fixtures to be shared across test classes.
/// </summary>
/// <remarks>
/// xUnit requires collection definitions to end with "Collection" by convention,
/// hence CA1711 is suppressed for these types.
/// </remarks>

/// <summary>
/// Collection for SQL Server EF Core integration tests.
/// </summary>
[CollectionDefinition("EFCore-SqlServer")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class EFCoreSqlServerCollection : ICollectionFixture<EFCoreSqlServerFixture>
{
}

/// <summary>
/// Collection for PostgreSQL EF Core integration tests.
/// </summary>
[CollectionDefinition("EFCore-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class EFCorePostgreSqlCollection : ICollectionFixture<EFCorePostgreSqlFixture>
{
}

/// <summary>
/// Collection for MySQL EF Core integration tests.
/// </summary>
[CollectionDefinition("EFCore-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class EFCoreMySqlCollection : ICollectionFixture<EFCoreMySqlFixture>
{
}

/// <summary>
/// Collection for Oracle EF Core integration tests.
/// </summary>
[CollectionDefinition("EFCore-Oracle")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class EFCoreOracleCollection : ICollectionFixture<EFCoreOracleFixture>
{
}

/// <summary>
/// Collection for SQLite EF Core integration tests.
/// </summary>
[CollectionDefinition("EFCore-Sqlite")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class EFCoreSqliteCollection : ICollectionFixture<EFCoreSqliteFixture>
{
}
