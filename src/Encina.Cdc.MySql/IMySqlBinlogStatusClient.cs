namespace Encina.Cdc.MySql;

/// <summary>
/// The MySqlConnector-facing operation of <see cref="MySqlCdcConnector"/>: reading the current binary
/// log coordinates of the server with <c>SHOW BINARY LOG STATUS</c> (MySQL 8.4 LTS and 9.x).
/// </summary>
/// <remarks>
/// The seam keeps the database call out of the connector so a test can drive every branch of
/// <see cref="MySqlCdcConnector.GetCurrentPositionAsync"/> with a hand-written client and no server.
/// </remarks>
internal interface IMySqlBinlogStatusClient
{
    /// <summary>
    /// Runs <c>SHOW BINARY LOG STATUS</c> and returns the first row.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The current binlog file name and position, or <see langword="null"/> when the server returned
    /// no row (binary logging is disabled).
    /// </returns>
    Task<(string File, long Position)?> GetBinlogStatusAsync(CancellationToken cancellationToken);
}
