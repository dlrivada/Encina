using Encina.DomainModeling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.SoftDelete;

/// <summary>
/// EF Core interceptor that converts delete operations to soft deletes for entities implementing
/// <see cref="ISoftDeletableEntity"/>.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor is invoked during <c>SavingChanges</c>/<c>SavingChangesAsync</c> (before save)
/// to convert delete operations on soft-deletable entities:
/// <list type="bullet">
/// <item><description><see cref="EntityState.Deleted"/>: Converted to <see cref="EntityState.Modified"/> with <c>IsDeleted = true</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Supported Interfaces</b>: The interceptor only processes entities implementing
/// <see cref="ISoftDeletableEntity"/> (with public setters). Entities implementing only
/// <see cref="ISoftDeletable"/> (getter-only) should handle soft delete via domain methods.
/// </para>
/// <para>
/// <b>User Resolution</b>: The current user is resolved from <see cref="IRequestContext.UserId"/>
/// via the service provider. If no user context is available, the <c>DeletedBy</c> property
/// is left as <c>null</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseSoftDelete = true;
/// });
///
/// // Entities implementing ISoftDeletableEntity get automatic soft delete
/// public class Order : FullyAuditedAggregateRoot&lt;OrderId&gt;
/// {
///     // ISoftDeletableEntity properties are inherited and auto-populated on delete
/// }
///
/// // Usage - delete is converted to soft delete
/// context.Orders.Remove(order);
/// await context.SaveChangesAsync(); // IsDeleted = true, DeletedAtUtc = now, DeletedBy = userId
/// </code>
/// </example>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SoftDeleteInterceptorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SoftDeleteInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving IRequestContext.</param>
    /// <param name="options">The soft delete interceptor options.</param>
    /// <param name="timeProvider">The time provider for consistent timestamps.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public SoftDeleteInterceptor(
        IServiceProvider serviceProvider,
        SoftDeleteInterceptorOptions options,
        TimeProvider timeProvider,
        ILogger<SoftDeleteInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (_options.Enabled && eventData.Context is not null)
        {
            ConvertToSoftDelete(eventData.Context);
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
            ConvertToSoftDelete(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Converts delete operations to soft deletes for entities implementing <see cref="ISoftDeletableEntity"/>.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    private void ConvertToSoftDelete(DbContext context)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = GetCurrentUserId();

        var deletedEntries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletableEntity)
            .ToList();

        var softDeletedCount = 0;

        foreach (var entry in deletedEntries)
        {
            var entity = (ISoftDeletableEntity)entry.Entity;

            // Set soft delete properties
            entity.IsDeleted = true;

            if (_options.TrackDeletedAt)
            {
                entity.DeletedAtUtc = nowUtc;
            }

            if (_options.TrackDeletedBy && userId is not null)
            {
                entity.DeletedBy = userId;
            }

            // Change state from Deleted to Modified to prevent physical deletion
            entry.State = EntityState.Modified;
            softDeletedCount++;
        }

        if (_options.LogSoftDeletes && softDeletedCount > 0)
        {
            Log.SoftDeleteConverted(_logger, softDeletedCount, userId ?? "(anonymous)");
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
}

/// <summary>
/// High-performance logging for the soft delete interceptor using LoggerMessage.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Debug,
        Message = "Soft delete converted: {SoftDeletedCount} entities soft-deleted by user {UserId}")]
    public static partial void SoftDeleteConverted(
        ILogger logger,
        int softDeletedCount,
        string userId);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Failed to resolve user ID for soft delete tracking")]
    public static partial void FailedToResolveUserId(ILogger logger, Exception exception);
}
