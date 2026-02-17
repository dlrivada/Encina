using Encina.Sharding.ReferenceTables;
using MySqlConnector;

namespace Encina.Dapper.MySQL.Sharding.ReferenceTables;

/// <summary>
/// MySQL factory that creates <see cref="ReferenceTableStoreDapper"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryDapper : IReferenceTableStoreFactory
{
    /// <inheritdoc />
    public IReferenceTableStore CreateForShard(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        MySqlConnection? connection = null;
        try
        {
            connection = new MySqlConnection(connectionString);
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
