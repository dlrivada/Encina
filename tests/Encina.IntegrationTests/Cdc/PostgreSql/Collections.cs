using System.Diagnostics.CodeAnalysis;

namespace Encina.IntegrationTests.Cdc.PostgreSql;

/// <summary>
/// Collection for PostgreSQL CDC integration tests. All classes share one container started with
/// <c>wal_level=logical</c>; the collection serializes them because they share one replication slot.
/// </summary>
[CollectionDefinition("CDC-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class CdcPostgreSqlCollection : ICollectionFixture<PostgreSqlCdcFixture>
{
}
