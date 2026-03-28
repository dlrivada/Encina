using Encina.Sharding.ReferenceTables;
using Microsoft.Data.Sqlite;

namespace Encina.Dapper.Sqlite.Sharding.ReferenceTables;

/// <summary>
/// SQLite factory that creates <see cref="ReferenceTableStoreDapper"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryDapper : IReferenceTableStoreFactory
{
    /// <inheritdoc />
    public IReferenceTableStore CreateForShard(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        SqliteConnection? connection = null;
        try
        {
            connection = new SqliteConnection(connectionString);
            connection.Open();
            var store = new ReferenceTableStoreDapper(connection);
            connection = null; // Ownership transferred to store
            return store;
        }
        finally
        {
            connection?.Dispose();
        }
    }
}
