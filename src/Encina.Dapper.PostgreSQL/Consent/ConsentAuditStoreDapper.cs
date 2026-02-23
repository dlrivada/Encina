using System.Data;
using System.Text.Json;
using Dapper;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Consent;

/// <summary>
/// Dapper implementation of <see cref="IConsentAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for GDPR Article 7(1) demonstrability.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses PostgreSQL-specific features:
/// <list type="bullet">
/// <item><description>Lowercase unquoted column identifiers (PostgreSQL folds to lowercase)</description></item>
/// <item><description>Native UUID support for Id column</description></item>
/// <item><description>TIMESTAMP for UTC datetime storage</description></item>
/// </list>
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of consent management and may be required during regulatory audits or data subject
/// access requests (Article 15).
/// </para>
/// </remarks>
public sealed class ConsentAuditStoreDapper : IConsentAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: ConsentAuditEntries).</param>
    public ConsentAuditStoreDapper(
        IDbConnection connection,
        string tableName = "ConsentAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ConsentAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var sql = $@"
                INSERT INTO {_tableName}
                (id, subjectid, purpose, action, occurredatutc, performedby, ipaddress, metadata)
                VALUES
                (@Id, @SubjectId, @Purpose, @Action, @OccurredAtUtc, @PerformedBy, @IpAddress, @Metadata)";

            await _connection.ExecuteAsync(sql, new
            {
                entry.Id,
                entry.SubjectId,
                entry.Purpose,
                Action = (int)entry.Action,
                OccurredAtUtc = entry.OccurredAtUtc.UtcDateTime,
                entry.PerformedBy,
                entry.IpAddress,
                Metadata = JsonSerializer.Serialize(entry.Metadata)
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to record audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = entry.SubjectId, ["purpose"] = entry.Purpose }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>> GetAuditTrailAsync(
        string subjectId,
        string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var sql = purpose is null
                ? $@"
                    SELECT id, subjectid, purpose, action, occurredatutc, performedby, ipaddress, metadata
                    FROM {_tableName}
                    WHERE subjectid = @SubjectId
                    ORDER BY occurredatutc DESC"
                : $@"
                    SELECT id, subjectid, purpose, action, occurredatutc, performedby, ipaddress, metadata
                    FROM {_tableName}
                    WHERE subjectid = @SubjectId AND purpose = @Purpose
                    ORDER BY occurredatutc DESC";

            var rows = await _connection.QueryAsync(sql, new { SubjectId = subjectId, Purpose = purpose });
            var entries = rows.Select(MapToConsentAuditEntry).ToList();

            return Right<EncinaError, IReadOnlyList<ConsentAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to get audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["subjectId"] = subjectId, ["purpose"] = purpose }));
        }
    }

    private static ConsentAuditEntry MapToConsentAuditEntry(dynamic row)
    {
        var metadataJson = (string)row.metadata;
        var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson)
            ?? new Dictionary<string, object?>();

        return new ConsentAuditEntry
        {
            Id = (Guid)row.id,
            SubjectId = (string)row.subjectid,
            Purpose = (string)row.purpose,
            Action = (ConsentAuditAction)(int)row.action,
            OccurredAtUtc = new DateTimeOffset((DateTime)row.occurredatutc, TimeSpan.Zero),
            PerformedBy = (string)row.performedby,
            IpAddress = row.ipaddress is not null and not DBNull ? (string)row.ipaddress : null,
            Metadata = metadata
        };
    }
}
