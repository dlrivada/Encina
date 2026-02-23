using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Encina.Compliance.Consent;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.Consent;

/// <summary>
/// Dapper implementation of <see cref="IConsentAuditStore"/> for SQLite.
/// Provides immutable audit trail for GDPR Article 7(1) demonstrability.
/// </summary>
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
                (Id, SubjectId, Purpose, Action, OccurredAtUtc, PerformedBy, IpAddress, Metadata)
                VALUES
                (@Id, @SubjectId, @Purpose, @Action, @OccurredAtUtc, @PerformedBy, @IpAddress, @Metadata)";

            await _connection.ExecuteAsync(sql, new
            {
                Id = entry.Id.ToString(),
                entry.SubjectId,
                entry.Purpose,
                Action = (int)entry.Action,
                OccurredAtUtc = entry.OccurredAtUtc.ToString("O"),
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
                details: new Dictionary<string, object?>
                {
                    ["subjectId"] = entry.SubjectId,
                    ["purpose"] = entry.Purpose
                }));
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
                    SELECT Id, SubjectId, Purpose, Action, OccurredAtUtc, PerformedBy, IpAddress, Metadata
                    FROM {_tableName}
                    WHERE SubjectId = @SubjectId
                    ORDER BY OccurredAtUtc DESC"
                : $@"
                    SELECT Id, SubjectId, Purpose, Action, OccurredAtUtc, PerformedBy, IpAddress, Metadata
                    FROM {_tableName}
                    WHERE SubjectId = @SubjectId AND Purpose = @Purpose
                    ORDER BY OccurredAtUtc DESC";

            var rows = purpose is null
                ? await _connection.QueryAsync(sql, new { SubjectId = subjectId })
                : await _connection.QueryAsync(sql, new { SubjectId = subjectId, Purpose = purpose });

            var entries = rows.Select(row =>
            {
                var metadataJson = (string)row.Metadata;
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(metadataJson)
                    ?? new Dictionary<string, object?>();

                return new ConsentAuditEntry
                {
                    Id = Guid.Parse((string)row.Id),
                    SubjectId = (string)row.SubjectId,
                    Purpose = (string)row.Purpose,
                    Action = (ConsentAuditAction)Convert.ToInt32(row.Action),
                    OccurredAtUtc = DateTimeOffset.Parse((string)row.OccurredAtUtc, null, DateTimeStyles.RoundtripKind),
                    PerformedBy = (string)row.PerformedBy,
                    IpAddress = row.IpAddress is null or DBNull ? null : (string)row.IpAddress,
                    Metadata = metadata
                };
            }).ToList();

            return Right<EncinaError, IReadOnlyList<ConsentAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to get audit trail: {ex.Message}",
                details: new Dictionary<string, object?>
                {
                    ["subjectId"] = subjectId,
                    ["purpose"] = purpose
                }));
        }
    }
}
