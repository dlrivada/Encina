using System.Data;
using Dapper;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;

namespace Encina.Dapper.PostgreSQL.Auditing;

/// <summary>
/// Dapper implementation of <see cref="IAuditLogStore"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses PostgreSQL-specific syntax:
/// <list type="bullet">
/// <item><description>Double-quote identifier quoting (e.g., "EntityType")</description></item>
/// <item><description>Native UUID support</description></item>
/// <item><description>TIMESTAMPTZ for timezone-aware timestamps</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="LogAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class AuditLogStoreDapper : IAuditLogStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly string _insertSql;
    private readonly string _selectSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit log table name (default: AuditLogs).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public AuditLogStoreDapper(IDbConnection connection, string tableName = "AuditLogs")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);

        // Build and cache SQL statements
        _insertSql = $@"
            INSERT INTO ""{_tableName}""
            (""Id"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""TimestampUtc"", ""OldValues"", ""NewValues"", ""CorrelationId"")
            VALUES
            (@Id, @EntityType, @EntityId, @Action, @UserId, @TimestampUtc, @OldValues, @NewValues, @CorrelationId)";

        _selectSql = $@"
            SELECT ""Id"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""TimestampUtc"", ""OldValues"", ""NewValues"", ""CorrelationId""
            FROM ""{_tableName}""
            WHERE ""EntityType"" = @EntityType AND ""EntityId"" = @EntityId
            ORDER BY ""TimestampUtc"" DESC";
    }

    /// <inheritdoc/>
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var parameters = new
        {
            entry.Id,
            entry.EntityType,
            entry.EntityId,
            Action = (int)entry.Action,
            entry.UserId,
            entry.TimestampUtc,
            entry.OldValues,
            entry.NewValues,
            entry.CorrelationId
        };

        await _connection.ExecuteAsync(_insertSql, parameters);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(entityId);

        var rows = await _connection.QueryAsync<AuditLogEntryRow>(
            _selectSql,
            new { EntityType = entityType, EntityId = entityId });

        return rows.Select(MapToEntry);
    }

    private static AuditLogEntry MapToEntry(AuditLogEntryRow row) => new(
        Id: row.Id,
        EntityType: row.EntityType,
        EntityId: row.EntityId,
        Action: (AuditAction)row.Action,
        UserId: row.UserId,
        TimestampUtc: row.TimestampUtc,
        OldValues: row.OldValues,
        NewValues: row.NewValues,
        CorrelationId: row.CorrelationId);

    /// <summary>
    /// Internal row type for Dapper mapping.
    /// </summary>
    private sealed class AuditLogEntryRow
    {
        public required string Id { get; init; }
        public required string EntityType { get; init; }
        public required string EntityId { get; init; }
        public int Action { get; init; }
        public string? UserId { get; init; }
        public DateTime TimestampUtc { get; init; }
        public string? OldValues { get; init; }
        public string? NewValues { get; init; }
        public string? CorrelationId { get; init; }
    }
}
