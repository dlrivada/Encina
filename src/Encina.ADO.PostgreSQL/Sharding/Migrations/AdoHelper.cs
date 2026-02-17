using System.Data;
using System.Data.Common;

namespace Encina.ADO.PostgreSQL.Sharding.Migrations;

/// <summary>
/// Shared ADO.NET async helpers for migration infrastructure.
/// </summary>
internal static class AdoHelper
{
    internal static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCmd)
        {
            return await dbCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }

        return command.ExecuteReader();
    }

    internal static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCmd)
        {
            return await dbCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return command.ExecuteNonQuery();
    }

    internal static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbReader)
        {
            return await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        return reader.Read();
    }

    internal static async Task CloseReaderAsync(IDataReader reader)
    {
        if (reader is DbDataReader dbReader)
        {
            await dbReader.CloseAsync().ConfigureAwait(false);
            return;
        }

        reader.Close();
    }
}
