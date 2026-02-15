using Encina.Sharding.ReferenceTables;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Sqlite.Sharding.ReferenceTables;

/// <summary>
/// SQLite factory that creates <see cref="ReferenceTableStoreADO"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryADO : IReferenceTableStoreFactory
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
            var store = new ReferenceTableStoreADO(connection);
            connection = null; // Ownership transferred to store
            return store;
        }
        finally
        {
            connection?.Dispose();
        }
    }
}
