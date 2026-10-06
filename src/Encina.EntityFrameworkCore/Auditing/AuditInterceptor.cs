using System.Text.Json;
using Encina.Diagnostics;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// EF Core interceptor that automatically populates audit fields on entities implementing
/// <see cref="IAuditableEntity"/> or its granular interfaces.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor is invoked during <c>SavingChanges</c>/<c>SavingChangesAsync</c> (before save)
/// to set audit properties on tracked entities based on their state:
/// <list type="bullet">
/// <item><description><see cref="EntityState.Added"/>: Sets <c>CreatedAtUtc</c> and <c>CreatedBy</c></description></item>
/// <item><description><see cref="EntityState.Modified"/>: Sets <c>ModifiedAtUtc</c> and <c>ModifiedBy</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Supported Interfaces</b>: The interceptor supports granular interface composition:
/// <list type="bullet">
/// <item><description><see cref="ICreatedAtUtc"/>: Track creation timestamp only</description></item>
/// <item><description><see cref="ICreatedBy"/>: Track creation user only</description></item>
/// <item><description><see cref="IModifiedAtUtc"/>: Track modification timestamp only</description></item>
/// <item><description><see cref="IModifiedBy"/>: Track modification user only</description></item>
/// <item><description><see cref="IAuditableEntity"/>: Combined interface for all four</description></item>
/// </list>
/// </para>
/// <para>
/// <b>User Resolution</b>: The current user is resolved from <see cref="RequestIdentity.UserId"/>
/// via <see cref="IRequestContextAccessor"/>. If no user context is available, the user properties
/// are left as <c>null</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseAuditing = true;
/// });
///
/// // In your DbContext configuration
/// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
/// {
///     // The interceptor is added via AddInterceptors
/// }
///
/// // Entities implementing IAuditableEntity get automatic population
/// public class Order : AuditedAggregateRoot&lt;OrderId&gt;
/// {
///     // CreatedAtUtc, CreatedBy, ModifiedAtUtc, ModifiedBy are auto-populated
/// }
/// </code>
/// </example>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditInterceptorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditInterceptor> _logger;
    private readonly IAuditLogStore? _auditLogStore;

    // Thread-local storage for pending audit entries (to capture before save, persist after)
    private static readonly AsyncLocal<List<PendingAuditEntry>> PendingEntries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditInterceptor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving IRequestContext.</param>
    /// <param name="options">The audit interceptor options.</param>
    /// <param name="timeProvider">The time provider for consistent timestamps.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="auditLogStore">Optional audit log store for persisting detailed audit entries.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public AuditInterceptor(
        IServiceProvider serviceProvider,
        AuditInterceptorOptions options,
        TimeProvider timeProvider,
        ILogger<AuditInterceptor> logger,
        IAuditLogStore? auditLogStore = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
        _auditLogStore = auditLogStore;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (_options.Enabled && eventData.Context is not null)
        {
            PopulateAuditFields(eventData.Context);
        }

        if (_options.LogChangesToStore && _auditLogStore is not null && eventData.Context is not null)
        {
            CaptureChangesForAuditLog(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_options.Enabled && eventData.Context is not null)
        {
            PopulateAuditFields(eventData.Context);
        }

        if (_options.LogChangesToStore && _auditLogStore is not null && eventData.Context is not null)
        {
            CaptureChangesForAuditLog(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_options.LogChangesToStore && _auditLogStore is not null)
        {
            PersistAuditEntriesSync();
        }

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_options.LogChangesToStore && _auditLogStore is not null)
        {
            await PersistAuditEntriesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        // Clear pending entries on failure
        PendingEntries.Value?.Clear();
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc/>
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // Clear pending entries on failure
        PendingEntries.Value?.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>
    /// Populates audit fields on all tracked entities based on their state.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    private void PopulateAuditFields(DbContext context)
    {
        var identity = GetCurrentRequestContext()?.Identity ?? RequestIdentity.Anonymous;
        var (addedCount, modifiedCount) = PopulateEntries(context, _timeProvider.GetUtcNow().UtcDateTime, identity.UserId);

        if (_options.LogAuditChanges && (addedCount > 0 || modifiedCount > 0))
        {
            Log.AuditFieldsPopulated(_logger, addedCount, modifiedCount, identity.Kind);
        }
    }

    // Populates the creation or modification fields of every added or modified entity.
    private (int Added, int Modified) PopulateEntries(DbContext context, DateTime nowUtc, string? userId)
    {
        var addedCount = 0;
        var modifiedCount = 0;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                PopulateCreationFields(entry.Entity, nowUtc, userId);
                addedCount++;
            }
            else if (entry.State == EntityState.Modified)
            {
                PopulateModificationFields(entry.Entity, nowUtc, userId);
                modifiedCount++;
            }
        }

        return (addedCount, modifiedCount);
    }

    /// <summary>
    /// Populates creation audit fields on an entity.
    /// </summary>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="userId">The current user ID.</param>
    private void PopulateCreationFields(object entity, DateTime nowUtc, string? userId)
    {
        if (_options.TrackCreatedAt && entity is ICreatedAtUtc createdAtEntity)
        {
            createdAtEntity.CreatedAtUtc = nowUtc;
        }

        if (_options.TrackCreatedBy && entity is ICreatedBy createdByEntity && userId is not null)
        {
            createdByEntity.CreatedBy = userId;
        }
    }

    /// <summary>
    /// Populates modification audit fields on an entity.
    /// </summary>
    /// <param name="entity">The entity to populate.</param>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="userId">The current user ID.</param>
    private void PopulateModificationFields(object entity, DateTime nowUtc, string? userId)
    {
        if (_options.TrackModifiedAt && entity is IModifiedAtUtc modifiedAtEntity)
        {
            modifiedAtEntity.ModifiedAtUtc = nowUtc;
        }

        if (_options.TrackModifiedBy && entity is IModifiedBy modifiedByEntity && userId is not null)
        {
            modifiedByEntity.ModifiedBy = userId;
        }
    }

    /// <summary>
    /// Resolves the ambient request context: the one <c>IEncina.Send/Publish/Stream</c> or
    /// <c>UseEncinaContext()</c> put on the accessor. There is no other source.
    /// </summary>
    /// <returns>The ambient request context, or <c>null</c> if none is available.</returns>
    private IRequestContext? GetCurrentRequestContext()
    {
        try
        {
            return _serviceProvider.GetService<IRequestContextAccessor>()?.RequestContext;
        }
        catch (Exception ex)
        {
            Log.FailedToResolveUserId(_logger, ex.ForLogging());
            return null;
        }
    }

    /// <summary>
    /// Captures entity changes to pending audit entries before save.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    private void CaptureChangesForAuditLog(DbContext context)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var requestContext = GetCurrentRequestContext();

        var entries = context.ChangeTracker.Entries()
            .Where(static e => ActionOf(e.State) is not null)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var pendingList = PendingEntries.Value ??= [];
        foreach (var entry in entries)
        {
            pendingList.Add(ToPendingEntry(entry, ActionOf(entry.State)!.Value, requestContext, nowUtc));
        }
    }

    // crap-exempt: single-question switch — the audit action of each entity state (null: not audited).
    private static AuditAction? ActionOf(EntityState state) => state switch
    {
        EntityState.Added => AuditAction.Created,
        EntityState.Modified => AuditAction.Updated,
        EntityState.Deleted => AuditAction.Deleted,
        _ => null
    };

    // The actor and correlation id come from the ambient request context only.
    private static PendingAuditEntry ToPendingEntry(EntityEntry entry, AuditAction action, IRequestContext? requestContext, DateTime nowUtc) =>
        new(
            entry.Entity.GetType().Name,
            GetEntityId(entry),
            action,
            requestContext?.Identity?.UserId,
            nowUtc,
            action != AuditAction.Created ? SerializeValues(entry.OriginalValues) : null,
            action != AuditAction.Deleted ? SerializeValues(entry.CurrentValues) : null,
            requestContext?.CorrelationId);

    /// <summary>
    /// Gets the entity ID as a string from the entity entry.
    /// </summary>
    /// <param name="entry">The entity entry.</param>
    /// <returns>The entity ID as string.</returns>
    private static string GetEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties is null || keyProperties.Count == 0)
        {
            return "(no-key)";
        }

        if (keyProperties.Count == 1)
        {
            var keyValue = entry.Property(keyProperties[0].Name).CurrentValue;
            return keyValue?.ToString() ?? "(null)";
        }

        // Composite key: join values with separator
        var keyValues = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "(null)");
        return string.Join("+", keyValues);
    }

    /// <summary>
    /// Serializes property values to JSON.
    /// </summary>
    /// <param name="values">The property values.</param>
    /// <returns>JSON string representation.</returns>
    private static string SerializeValues(PropertyValues values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var property in values.Properties)
        {
            dict[property.Name] = values[property];
        }
        return JsonSerializer.Serialize(dict);
    }

    /// <summary>
    /// Persists pending audit entries synchronously.
    /// </summary>
    private void PersistAuditEntriesSync()
    {
        var pendingList = PendingEntries.Value;
        if (pendingList is null || pendingList.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var pending in pendingList)
            {
                var entry = new AuditLogEntry(
                    Id: Guid.NewGuid().ToString(),
                    EntityType: pending.EntityType,
                    EntityId: pending.EntityId,
                    Action: pending.Action,
                    UserId: pending.UserId,
                    TimestampUtc: pending.TimestampUtc,
                    OldValues: pending.OldValues,
                    NewValues: pending.NewValues,
                    CorrelationId: pending.CorrelationId);

                // Fire-and-forget for sync path - not ideal but matches sync SaveChanges behavior
                _auditLogStore!.LogAsync(entry, CancellationToken.None).GetAwaiter().GetResult();
            }

            if (_options.LogAuditChanges)
            {
                Log.AuditEntriesPersisted(_logger, pendingList.Count);
            }
        }
        catch (Exception ex)
        {
            Log.FailedToPersistAuditEntries(_logger, ex.ForLogging());
        }
        finally
        {
            pendingList.Clear();
        }
    }

    /// <summary>
    /// Persists pending audit entries asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private async Task PersistAuditEntriesAsync(CancellationToken cancellationToken)
    {
        var pendingList = PendingEntries.Value;
        if (pendingList is null || pendingList.Count == 0)
        {
            return;
        }

        try
        {
            foreach (var pending in pendingList)
            {
                var entry = new AuditLogEntry(
                    Id: Guid.NewGuid().ToString(),
                    EntityType: pending.EntityType,
                    EntityId: pending.EntityId,
                    Action: pending.Action,
                    UserId: pending.UserId,
                    TimestampUtc: pending.TimestampUtc,
                    OldValues: pending.OldValues,
                    NewValues: pending.NewValues,
                    CorrelationId: pending.CorrelationId);

                await _auditLogStore!.LogAsync(entry, cancellationToken).ConfigureAwait(false);
            }

            if (_options.LogAuditChanges)
            {
                Log.AuditEntriesPersisted(_logger, pendingList.Count);
            }
        }
        catch (Exception ex)
        {
            Log.FailedToPersistAuditEntries(_logger, ex.ForLogging());
        }
        finally
        {
            pendingList.Clear();
        }
    }

    /// <summary>
    /// Represents a pending audit entry captured before save.
    /// </summary>
    private sealed record PendingAuditEntry(
        string EntityType,
        string EntityId,
        AuditAction Action,
        string? UserId,
        DateTime TimestampUtc,
        string? OldValues,
        string? NewValues,
        string? CorrelationId);
}

/// <summary>
/// High-performance logging for the audit interceptor using LoggerMessage.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Debug,
        Message = "Audit fields populated: {AddedCount} added, {ModifiedCount} modified by a {IdentityKind} identity")]
    public static partial void AuditFieldsPopulated(
        ILogger logger,
        int addedCount,
        int modifiedCount,
        IdentityKind identityKind);

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "Failed to resolve user ID for audit tracking")]
    public static partial void FailedToResolveUserId(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Debug,
        Message = "Audit entries persisted: {Count} entries logged to store")]
    public static partial void AuditEntriesPersisted(ILogger logger, int count);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Error,
        Message = "Failed to persist audit entries to store")]
    public static partial void FailedToPersistAuditEntries(ILogger logger, Exception exception);
}
