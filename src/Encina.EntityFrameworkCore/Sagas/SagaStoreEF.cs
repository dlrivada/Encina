using Encina.Messaging.Sagas;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public SagaStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<ISagaState?> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<SagaState>()
            .FirstOrDefaultAsync(s => s.SagaId == sagaId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        if (sagaState is not SagaState efSaga)
        {
            throw new InvalidOperationException(
                $"SagaStoreEF requires saga state of type {nameof(SagaState)}, " +
                $"but received {sagaState.GetType().Name}");
        }

        await _dbContext.Set<SagaState>().AddAsync(efSaga, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaState);

        if (sagaState is not SagaState efSaga)
        {
            throw new InvalidOperationException(
                $"SagaStoreEF requires saga state of type {nameof(SagaState)}, " +
                $"but received {sagaState.GetType().Name}");
        }

        // EF Core tracks changes automatically, no need for explicit Update call
        efSaga.LastUpdatedAtUtc = DateTime.UtcNow;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync(
        TimeSpan olderThan,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.Subtract(olderThan);

        var sagas = await _dbContext.Set<SagaState>()
            .Where(s =>
                (s.Status == SagaStatus.Running || s.Status == SagaStatus.Compensating) &&
                s.LastUpdatedAtUtc < threshold)
            .OrderBy(s => s.LastUpdatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return sagas;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ISagaState>> GetExpiredSagasAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var sagas = await _dbContext.Set<SagaState>()
            .Where(s =>
                (s.Status == SagaStatus.Running || s.Status == SagaStatus.Compensating) &&
                s.TimeoutAtUtc != null &&
                s.TimeoutAtUtc <= now)
            .OrderBy(s => s.TimeoutAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return sagas;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
