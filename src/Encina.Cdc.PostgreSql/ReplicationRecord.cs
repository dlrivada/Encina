using NpgsqlTypes;

namespace Encina.Cdc.PostgreSql;

/// <summary>
/// A logical-replication message decoded by <see cref="IPostgresReplicationClient"/>, free of Npgsql
/// message types so the connector can map it to a change event without a live server.
/// </summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
internal abstract record ReplicationRecord(NpgsqlLogSequenceNumber WalEnd);

/// <summary>A relation (table metadata) message; it carries no change.</summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
internal sealed record RelationRecord(NpgsqlLogSequenceNumber WalEnd) : ReplicationRecord(WalEnd);

/// <summary>A message kind the connector does not map (begin, commit, truncate, ...).</summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
internal sealed record SkippedRecord(NpgsqlLogSequenceNumber WalEnd) : ReplicationRecord(WalEnd);

/// <summary>A row insert.</summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
/// <param name="SchemaName">The schema (namespace) of the table.</param>
/// <param name="RelationName">The table name.</param>
/// <param name="After">The new row, column name to value in column order.</param>
internal sealed record InsertRecord(
    NpgsqlLogSequenceNumber WalEnd,
    string? SchemaName,
    string RelationName,
    IReadOnlyDictionary<string, string?> After) : ReplicationRecord(WalEnd);

/// <summary>A row update.</summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
/// <param name="SchemaName">The schema (namespace) of the table.</param>
/// <param name="RelationName">The table name.</param>
/// <param name="Before">The old row or key, or <see langword="null"/> when the replica identity sends none.</param>
/// <param name="After">The new row, column name to value in column order.</param>
internal sealed record UpdateRecord(
    NpgsqlLogSequenceNumber WalEnd,
    string? SchemaName,
    string RelationName,
    IReadOnlyDictionary<string, string?>? Before,
    IReadOnlyDictionary<string, string?> After) : ReplicationRecord(WalEnd);

/// <summary>A row delete.</summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
/// <param name="SchemaName">The schema (namespace) of the table.</param>
/// <param name="RelationName">The table name.</param>
/// <param name="Before">The old row or key, or <see langword="null"/> when the replica identity sends none.</param>
internal sealed record DeleteRecord(
    NpgsqlLogSequenceNumber WalEnd,
    string? SchemaName,
    string RelationName,
    IReadOnlyDictionary<string, string?>? Before) : ReplicationRecord(WalEnd);

/// <summary>
/// A data message whose row could not be decoded. The stream continues after it, as the connector
/// reports the failure for that message only.
/// </summary>
/// <param name="WalEnd">The WAL position at the end of the message.</param>
/// <param name="Error">The decoding failure.</param>
internal sealed record DecodeFailureRecord(NpgsqlLogSequenceNumber WalEnd, Exception Error) : ReplicationRecord(WalEnd);
