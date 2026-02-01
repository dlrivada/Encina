using Encina.DomainModeling.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core implementation of <see cref="IAuditLogStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses EF Core to persist audit log entries to the database.
/// It provides:
/// <list type="bullet">
/// <item><description>Immediate persistence via SaveChangesAsync for durability</description></item>
/// <item><description>Optimized queries with proper indexing</description></item>
/// <item><description>Provider-agnostic support for SQLite, SQL Server, PostgreSQL, and MySQL</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Design Decision</b>: Each LogAsync call immediately persists the audit entry
/// to the database. This ensures audit entries are never lost, even if subsequent
/// operations fail. This aligns with the existing pattern used by OutboxStoreEF
/// and InboxStoreEF.
/// </para>
/// </remarks>
public sealed class AuditLogStoreEF : IAuditLogStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public AuditLogStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var entity = MapToEntity(entry);
        await _dbContext.Set<AuditLogEntryEntity>().AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(entityId);

        var entities = await _dbContext.Set<AuditLogEntryEntity>()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToRecord);
    }

    /// <summary>
    /// Maps an <see cref="AuditLogEntry"/> record to an <see cref="AuditLogEntryEntity"/>.
    /// </summary>
    internal static AuditLogEntryEntity MapToEntity(AuditLogEntry entry) => new()
    {
        Id = entry.Id,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        Action = entry.Action,
        UserId = entry.UserId,
        TimestampUtc = entry.TimestampUtc,
        OldValues = entry.OldValues,
        NewValues = entry.NewValues,
        CorrelationId = entry.CorrelationId
    };

    /// <summary>
    /// Maps an <see cref="AuditLogEntryEntity"/> to an <see cref="AuditLogEntry"/> record.
    /// </summary>
    internal static AuditLogEntry MapToRecord(AuditLogEntryEntity entity) => new(
        Id: entity.Id,
        EntityType: entity.EntityType,
        EntityId: entity.EntityId,
        Action: entity.Action,
        UserId: entity.UserId,
        TimestampUtc: entity.TimestampUtc,
        OldValues: entity.OldValues,
        NewValues: entity.NewValues,
        CorrelationId: entity.CorrelationId);
}
