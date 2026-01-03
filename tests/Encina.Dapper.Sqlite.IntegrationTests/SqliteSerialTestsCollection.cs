using System.Diagnostics.CodeAnalysis;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.Sqlite.IntegrationTests;

/// <summary>
/// Collection definition for tests that must run serially and share SQLite fixture.
/// SQLite in-memory databases don't support concurrent access
/// from multiple connections, so these tests cannot run in parallel.
/// </summary>
[CollectionDefinition("SqliteSerialTests", DisableParallelization = true)]
[SuppressMessage("Design", "CA1711:Identifiers should not have incorrect suffix", Justification = "Required by xUnit collection definition pattern")]
public sealed class SqliteSerialTestsCollection : ICollectionFixture<SqliteFixture>
{
}
