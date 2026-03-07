using Encina.Messaging.Sagas;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Sagas;

/// <summary>
/// Entity Framework Core implementation of <see cref="ISagaStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides saga orchestration support using EF Core's
/// change tracking, optimistic concurrency, and transaction capabilities.
/// </para>
/// </remarks>
public sealed class SagaStoreEF : ISagaStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public SagaStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var saga = await _dbContext.Set<SagaState>()
                .FirstOrDefaultAsync(s => s.SagaId == sagaId, cancellationToken);

            return saga is not null
                ? Option<ISagaState>.Some(saga)
                : Option<ISagaState>.None;
        }, "saga.get_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        if (sagaState is not SagaState efSaga)
        {
            return EncinaErrors.Create("saga.invalid_type",
                $"SagaStoreEF requires saga state of type {nameof(SagaState)}, got {sagaState.GetType().Name}");
        }

        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.Set<SagaState>().AddAsync(efSaga, cancellationToken);
        }, "saga.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<Either<EncinaError, Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        if (sagaState is not SagaState efSaga)
        {
            return Task.FromResult<Either<EncinaError, Unit>>(
                EncinaErrors.Create("saga.invalid_type",
                    $"SagaStoreEF requires saga state of type {nameof(SagaState)}, got {sagaState.GetType().Name}"));
        }

        // EF Core tracks changes automatically, no need for explicit Update call
        efSaga.LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var threshold = _timeProvider.GetUtcNow().UtcDateTime.Subtract(olderThan);

            var sagas = await _dbContext.Set<SagaState>()
                .Where(s =>
                    (s.Status == SagaStatus.Running || s.Status == SagaStatus.Compensating) &&
                    s.LastUpdatedAtUtc < threshold)
                .OrderBy(s => s.LastUpdatedAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            return (IEnumerable<ISagaState>)sagas;
        }, "saga.get_stuck_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var sagas = await _dbContext.Set<SagaState>()
                .Where(s =>
                    (s.Status == SagaStatus.Running || s.Status == SagaStatus.Compensating) &&
                    s.TimeoutAtUtc != null &&
                    s.TimeoutAtUtc <= now)
                .OrderBy(s => s.TimeoutAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            return (IEnumerable<ISagaState>)sagas;
        }, "saga.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, "saga.save_failed").ConfigureAwait(false);
    }
}
