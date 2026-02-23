using System.Text.Json;
using Encina.Compliance.Consent;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Consent;

/// <summary>
/// Entity Framework Core implementation of <see cref="IConsentAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides an immutable audit trail for consent-related actions as required by
/// GDPR Article 7(1). Each <see cref="RecordAsync"/> call immediately persists
/// the audit entry to ensure it is never lost.
/// </para>
/// </remarks>
public sealed class ConsentAuditStoreEF : IConsentAuditStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public ConsentAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ConsentAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = MapToEntity(entry);
            await _dbContext.Set<ConsentAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: $"Failed to record consent audit entry: {ex.Message}"));
        }
        catch (OperationCanceledException)
        {
            return Left(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>> GetAuditTrailAsync(
        string subjectId,
        string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        try
        {
            var query = _dbContext.Set<ConsentAuditEntryEntity>()
                .Where(e => e.SubjectId == subjectId);

            if (purpose is not null)
            {
                query = query.Where(e => e.Purpose == purpose);
            }

            var entities = await query
                .OrderByDescending(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<ConsentAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<ConsentAuditEntry>>(EncinaErrors.Create(
                code: "consent.audit_store_error",
                message: "Operation was cancelled"));
        }
    }

    internal static ConsentAuditEntryEntity MapToEntity(ConsentAuditEntry entry) => new()
    {
        Id = entry.Id,
        SubjectId = entry.SubjectId,
        Purpose = entry.Purpose,
        Action = entry.Action,
        OccurredAtUtc = entry.OccurredAtUtc,
        PerformedBy = entry.PerformedBy,
        IpAddress = entry.IpAddress,
        Metadata = SerializeMetadata(entry.Metadata)
    };

    internal static ConsentAuditEntry MapToRecord(ConsentAuditEntryEntity entity) => new()
    {
        Id = entity.Id,
        SubjectId = entity.SubjectId,
        Purpose = entity.Purpose,
        Action = entity.Action,
        OccurredAtUtc = entity.OccurredAtUtc,
        PerformedBy = entity.PerformedBy,
        IpAddress = entity.IpAddress,
        Metadata = DeserializeMetadata(entity.Metadata)
    };

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
            return null;

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private static Dictionary<string, object?> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object?>();

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
