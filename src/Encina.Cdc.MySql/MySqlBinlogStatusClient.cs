namespace Encina.Cdc.MySql;

/// <summary>
/// MySqlConnector-backed <see cref="IMySqlBinlogStatusClient"/>: opens a connection and runs
/// <c>SHOW BINARY LOG STATUS</c>.
/// </summary>
internal sealed class MySqlBinlogStatusClient : IMySqlBinlogStatusClient
{
    private readonly MySqlCdcOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlBinlogStatusClient"/> class.
    /// </summary>
    /// <param name="options">MySQL CDC options.</param>
    public MySqlBinlogStatusClient(MySqlCdcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <inheritdoc />
    public async Task<(string File, long Position)?> GetBinlogStatusAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnector.MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "SHOW BINARY LOG STATUS";

        await using var reader = await command
            .ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow | System.Data.CommandBehavior.SingleResult, cancellationToken)
            .ConfigureAwait(false);

        return await ReadPositionAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the binlog file and position from the first row of a <c>SHOW BINARY LOG STATUS</c> result.
    /// </summary>
    /// <param name="reader">The reader positioned before the first row.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file and position, or <see langword="null"/> when the result is empty.</returns>
    internal static async Task<(string File, long Position)?> ReadPositionAsync(
        System.Data.Common.DbDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var fileOrdinal = reader.GetOrdinal("File");
        var positionOrdinal = reader.GetOrdinal("Position");

        return (reader.GetString(fileOrdinal), reader.GetInt64(positionOrdinal));
    }
}
