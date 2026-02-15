using Encina.Sharding.ReferenceTables;
using Microsoft.Data.SqlClient;

namespace Encina.Dapper.SqlServer.Sharding.ReferenceTables;

/// <summary>
/// SQL Server factory that creates <see cref="ReferenceTableStoreDapper"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryDapper : IReferenceTableStoreFactory
{
    /// <inheritdoc />
    public IReferenceTableStore CreateForShard(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        SqlConnection? connection = null;
        try
        {
            connection = new SqlConnection(connectionString);
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
