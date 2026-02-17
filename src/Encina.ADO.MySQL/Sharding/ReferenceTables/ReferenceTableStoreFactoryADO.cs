using Encina.Sharding.ReferenceTables;
using MySqlConnector;

namespace Encina.ADO.MySQL.Sharding.ReferenceTables;

/// <summary>
/// MySQL factory that creates <see cref="ReferenceTableStoreADO"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryADO : IReferenceTableStoreFactory
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
