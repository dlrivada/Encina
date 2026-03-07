using System.Diagnostics;
using Encina.Messaging.Sagas;
using LanguageExt;

namespace Encina.OpenTelemetry.MessagingStores;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="ISagaStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner store and creates <see cref="Activity"/> spans for state-changing operations
/// (add, update, get, get stuck sagas, get expired sagas). All activity creation is guarded by
/// <see cref="ActivitySource.HasListeners()"/> for zero-cost when no trace collector is configured.
/// </para>
/// <para>
/// The activity source name <c>"Encina.Messaging.Saga"</c> must be registered with the
/// OpenTelemetry tracer, which is done automatically by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedSagaStore : ISagaStore
{
    private static readonly ActivitySource Source = new("Encina.Messaging.Saga", "1.0");

    private readonly ISagaStore _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedSagaStore"/> class.
    /// </summary>
    /// <param name="inner">The inner saga store to decorate.</param>
    public InstrumentedSagaStore(ISagaStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        using var activity = StartSagaGet(sagaId);
        var result = await _inner.GetAsync(sagaId, cancellationToken).ConfigureAwait(false);
        result.IfRight(opt => opt.IfSome(saga =>
        {
            activity?.SetTag("saga.type", saga.SagaType);
            activity?.SetTag("saga.status", saga.Status);
        }));
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        using var activity = StartSagaCreate(sagaState.SagaType, sagaState.SagaId);
        var result = await _inner.AddAsync(sagaState, cancellationToken).ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        using var activity = StartSagaUpdate(sagaState.SagaType, sagaState.SagaId, sagaState.Status);
        var result = await _inner.UpdateAsync(sagaState, cancellationToken).ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartGetStuckSagas(batchSize, olderThan);
        var result = await _inner.GetStuckSagasAsync(olderThan, batchSize, cancellationToken)
            .ConfigureAwait(false);
        result.IfRight(sagas =>
        {
            var count = sagas is ICollection<ISagaState> col ? col.Count : sagas.Count();
            CompleteBatch(activity, count);
        });
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartGetExpiredSagas(batchSize);
        var result = await _inner.GetExpiredSagasAsync(batchSize, cancellationToken)
            .ConfigureAwait(false);
        result.IfRight(sagas =>
        {
            var count = sagas is ICollection<ISagaState> col ? col.Count : sagas.Count();
            CompleteBatch(activity, count);
        });
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _inner.SaveChangesAsync(cancellationToken);

    private static Activity? StartSagaGet(Guid sagaId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.saga.get", ActivityKind.Internal);
        activity?.SetTag("saga.id", sagaId.ToString());
        return activity;
    }

    private static Activity? StartSagaCreate(string sagaType, Guid sagaId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.saga.create", ActivityKind.Internal);
        activity?.SetTag("saga.type", sagaType);
        activity?.SetTag("saga.id", sagaId.ToString());
        return activity;
    }

    private static Activity? StartSagaUpdate(string sagaType, Guid sagaId, string status)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.saga.update", ActivityKind.Internal);
        activity?.SetTag("saga.type", sagaType);
        activity?.SetTag("saga.id", sagaId.ToString());
        activity?.SetTag("saga.status", status);
        return activity;
    }

    private static Activity? StartGetStuckSagas(int batchSize, TimeSpan olderThan)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.saga.get_stuck", ActivityKind.Internal);
        activity?.SetTag("saga.batch_size", batchSize);
        activity?.SetTag("saga.older_than_seconds", olderThan.TotalSeconds);
        return activity;
    }

    private static Activity? StartGetExpiredSagas(int batchSize)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.saga.get_expired", ActivityKind.Internal);
        activity?.SetTag("saga.batch_size", batchSize);
        return activity;
    }

    private static void Complete(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteBatch(Activity? activity, int count)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("saga.result_count", count);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void Failed(Activity? activity, string? errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }
}
