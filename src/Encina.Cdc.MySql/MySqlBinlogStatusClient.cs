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

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return (reader.GetString(reader.GetOrdinal("File")), reader.GetInt64(reader.GetOrdinal("Position")));
    }
}
