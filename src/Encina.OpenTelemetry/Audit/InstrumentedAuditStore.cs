using System.Diagnostics;
using Encina.Security.Audit;
using LanguageExt;

namespace Encina.OpenTelemetry.Audit;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="IAuditStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner store and creates <see cref="Activity"/> spans for all audit operations
/// (record, query by entity/user/correlation, flexible query, purge). All activity creation
/// is guarded by <see cref="ActivitySource.HasListeners()"/> for zero-cost when no trace
/// collector is configured.
/// </para>
/// <para>
/// The activity source name <c>"Encina.Audit"</c> must be registered with the OpenTelemetry
/// tracer, which is done automatically by <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedAuditStore : IAuditStore
{
    private static readonly ActivitySource Source = new("Encina.Audit", "1.0");

    private readonly IAuditStore _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedAuditStore"/> class.
    /// </summary>
    /// <param name="inner">The inner audit store to decorate.</param>
    public InstrumentedAuditStore(IAuditStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartRecord(entry.EntityType, entry.EntityId);

        try
        {
            var result = await _inner.RecordAsync(entry, cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: _ => Complete(activity),
                Left: e => Failed(activity, e.Message));

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartGetByEntity(entityType, entityId);

        try
        {
            var result = await _inner.GetByEntityAsync(entityType, entityId, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: entries => CompleteWithCount(activity, entries.Count),
                Left: e => Failed(activity, e.Message));

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartGetByUser(userId);

        try
        {
            var result = await _inner.GetByUserAsync(userId, fromUtc, toUtc, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: entries => CompleteWithCount(activity, entries.Count),
                Left: e => Failed(activity, e.Message));

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartGetByCorrelationId(correlationId);

        try
        {
            var result = await _inner.GetByCorrelationIdAsync(correlationId, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: entries => CompleteWithCount(activity, entries.Count),
                Left: e => Failed(activity, e.Message));

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartQuery();

        try
        {
            var result = await _inner.QueryAsync(query, cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: paged => CompleteWithCount(activity, paged.Items.Count),
                Left: e => Failed(activity, e.Message));

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartPurge();

        try
        {
            var result = await _inner.PurgeEntriesAsync(olderThanUtc, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: count => CompletePurge(activity, count),
                Left: e => Failed(activity, e.Message));

            return result;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    #region Activity Helpers

    private static Activity? StartRecord(string entityType, string? entityId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.audit.record", ActivityKind.Internal);
        activity?.SetTag("audit.entity_type", entityType);
        activity?.SetTag("audit.entity_id", entityId);
        return activity;
    }

    private static Activity? StartGetByEntity(string entityType, string? entityId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.audit.get_by_entity", ActivityKind.Internal);
        activity?.SetTag("audit.entity_type", entityType);
        activity?.SetTag("audit.entity_id", entityId);
        return activity;
    }

    private static Activity? StartGetByUser(string userId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.audit.get_by_user", ActivityKind.Internal);
        activity?.SetTag("audit.user_id", userId);
        return activity;
    }

    private static Activity? StartGetByCorrelationId(string correlationId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.audit.get_by_correlation_id", ActivityKind.Internal);
        activity?.SetTag("audit.correlation_id", correlationId);
        return activity;
    }

    private static Activity? StartQuery()
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        return Source.StartActivity("encina.audit.query", ActivityKind.Internal);
    }

    private static Activity? StartPurge()
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        return Source.StartActivity("encina.audit.purge", ActivityKind.Internal);
    }

    private static void Complete(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteWithCount(Activity? activity, int count)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("audit.result_count", count);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompletePurge(Activity? activity, int purgedCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("audit.purged_count", purgedCount);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void Failed(Activity? activity, string? errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    #endregion
}
