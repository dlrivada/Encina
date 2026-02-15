using Encina.Sharding.ReferenceTables;
using Npgsql;

namespace Encina.Dapper.PostgreSQL.Sharding.ReferenceTables;

/// <summary>
/// PostgreSQL factory that creates <see cref="ReferenceTableStoreDapper"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryDapper : IReferenceTableStoreFactory
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
