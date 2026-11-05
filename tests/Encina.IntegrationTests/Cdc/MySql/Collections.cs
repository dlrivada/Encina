using System.Diagnostics.CodeAnalysis;

namespace Encina.IntegrationTests.Cdc.MySql;

/// <summary>
/// Collection for MySQL CDC integration tests. All classes share the servers started with binlog and
/// GTID enabled; the collection serializes them because they share the replication servers.
/// </summary>
[CollectionDefinition("CDC-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class CdcMySqlCollection : ICollectionFixture<MySqlCdcFixture>
{
}
