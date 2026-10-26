using NpgsqlTypes;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// The Npgsql-facing operations of <see cref="PostgresCdcConnector"/>: reading the current WAL
/// position, ensuring the publication exists and streaming logical-replication messages already
/// decoded into <see cref="ReplicationRecord"/> values.
/// </summary>
/// <remarks>
/// The Npgsql replication message types have no public constructors, so the connector's mapping
/// to change events could not be unit tested while it read them directly. This seam keeps every
/// Npgsql type behind <see cref="NpgsqlReplicationClient"/> and lets a test drive the connector with
/// hand-written records.
/// </remarks>
internal interface IPostgresReplicationClient
{
    /// <summary>
    /// Reads the current WAL LSN of the server.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The LSN in its text form (for example <c>0/16B3748</c>), or <see langword="null"/> when the server returned no value.</returns>
    Task<string?> GetCurrentWalLsnAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates the publication when it does not exist yet.
    /// </summary>
    /// <param name="publicationName">The publication name.</param>
    /// <param name="tables">The tables to publish; an empty array publishes all tables.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the publication exists.</returns>
    Task EnsurePublicationAsync(string publicationName, string[] tables, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the replication connection and streams the decoded messages, acknowledging the WAL
    /// position of each record after the consumer has taken it.
    /// </summary>
    /// <param name="startLsn">The position to resume from, or <see langword="null"/> to start from the slot position.</param>
    /// <param name="cancellationToken">A token to cancel the stream.</param>
    /// <returns>The decoded records, in WAL order.</returns>
    IAsyncEnumerable<ReplicationRecord> StreamAsync(
        NpgsqlLogSequenceNumber? startLsn,
        CancellationToken cancellationToken);
}
