using Encina.Sharding.ReferenceTables;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Sharding.ReferenceTables;

/// <summary>
/// PostgreSQL factory that creates <see cref="ReferenceTableStoreADO"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryADO : IReferenceTableStoreFactory
{
    /// <inheritdoc />
    public IReferenceTableStore CreateForShard(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        NpgsqlConnection? connection = null;
        try
        {
            connection = new NpgsqlConnection(connectionString);
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
