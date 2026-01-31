using System.Text.Json;
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
/// <b>User Resolution</b>: The current user is resolved from <see cref="IRequestContext.UserId"/>
/// via <c>IRequestContextAccessor</c>. If no user context is available, the user properties
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
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = GetCurrentUserId();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var addedCount = 0;
        var modifiedCount = 0;

        foreach (var entry in entries)
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

        if (_options.LogAuditChanges && (addedCount > 0 || modifiedCount > 0))
        {
            Log.AuditFieldsPopulated(_logger, addedCount, modifiedCount, userId ?? "(anonymous)");
        }
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
    /// Resolves the current user ID from the request context.
    /// </summary>
    /// <returns>The current user ID, or <c>null</c> if not available.</returns>
    private string? GetCurrentUserId()
    {
        try
        {
            // Try to resolve IRequestContext from the service provider
            // This works when the interceptor is invoked within an Encina pipeline
            var requestContext = _serviceProvider.GetService<IRequestContext>();
            if (requestContext is not null)
            {
                return requestContext.UserId;
            }

            // For ASP.NET Core applications, try to get context from accessor
            // This pattern is used by EncinaContextMiddleware
            var accessorType = Type.GetType("Encina.AspNetCore.IRequestContextAccessor, Encina.AspNetCore");
            if (accessorType is not null)
            {
                var accessor = _serviceProvider.GetService(accessorType);
                if (accessor is not null)
                {
                    var requestContextProperty = accessorType.GetProperty("RequestContext");
                    var context = requestContextProperty?.GetValue(accessor) as IRequestContext;
                    return context?.UserId;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.FailedToResolveUserId(_logger, ex);
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
        var userId = GetCurrentUserId();
        var correlationId = GetCorrelationId();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        PendingEntries.Value ??= [];
        var pendingList = PendingEntries.Value;

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => (AuditAction?)null
            };

            if (action is null)
            {
                continue;
            }

            var entityType = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);
            var oldValues = action.Value != AuditAction.Created ? SerializeValues(entry.OriginalValues) : null;
            var newValues = action.Value != AuditAction.Deleted ? SerializeValues(entry.CurrentValues) : null;

            pendingList.Add(new PendingAuditEntry(
                entityType,
                entityId,
                action.Value,
                userId,
                nowUtc,
                oldValues,
                newValues,
                correlationId));
        }
    }

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
    /// Gets the correlation ID from the request context if available.
    /// </summary>
    /// <returns>The correlation ID, or <c>null</c> if not available.</returns>
    private string? GetCorrelationId()
    {
        try
        {
            var requestContext = _serviceProvider.GetService<IRequestContext>();
            return requestContext?.CorrelationId;
        }
        catch
        {
            return null;
        }
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
            Log.FailedToPersistAuditEntries(_logger, ex);
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
            Log.FailedToPersistAuditEntries(_logger, ex);
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
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Audit fields populated: {AddedCount} added, {ModifiedCount} modified by user {UserId}")]
    public static partial void AuditFieldsPopulated(
        ILogger logger,
        int addedCount,
        int modifiedCount,
        string userId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to resolve user ID for audit tracking")]
    public static partial void FailedToResolveUserId(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Audit entries persisted: {Count} entries logged to store")]
    public static partial void AuditEntriesPersisted(ILogger logger, int count);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Failed to persist audit entries to store")]
    public static partial void FailedToPersistAuditEntries(ILogger logger, Exception exception);
}
