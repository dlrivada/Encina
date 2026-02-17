using Encina.Sharding.ReferenceTables;
using Microsoft.Data.SqlClient;

namespace Encina.ADO.SqlServer.Sharding.ReferenceTables;

/// <summary>
/// SQL Server factory that creates <see cref="ReferenceTableStoreADO"/> instances
/// bound to a specific shard connection string.
/// </summary>
public sealed class ReferenceTableStoreFactoryADO : IReferenceTableStoreFactory
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
