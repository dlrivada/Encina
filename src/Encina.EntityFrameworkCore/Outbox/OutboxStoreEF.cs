using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Outbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOutboxStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses EF Core's change tracking and query capabilities
/// to manage outbox messages. It provides:
/// <list type="bullet">
/// <item><description>Transactional consistency with domain operations</description></item>
/// <item><description>Optimized queries with proper indexing</description></item>
/// <item><description>Automatic change tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OutboxStoreEF : IOutboxStore
{
    /// <summary>
    /// Maximum number of message identifiers sent in one requeue query, well below the
    /// 2,100-parameter limit of SQL Server.
    /// </summary>
    private const int RequeueIdBatchSize = 1000;

    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public OutboxStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not OutboxMessage efMessage)
        {
            return EncinaErrors.Create("outbox.invalid_type",
                $"OutboxStoreEF requires messages of type {nameof(OutboxMessage)}, got {message.GetType().Name}");
        }

        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.Set<OutboxMessage>().AddAsync(efMessage, cancellationToken);
        }, "outbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var messages = await _dbContext.Set<OutboxMessage>()
                .Where(m =>
                    m.ProcessedAtUtc == null &&
                    (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now) &&
                    m.RetryCount < maxRetries)
                .OrderBy(m => m.CreatedAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            return (IEnumerable<IOutboxMessage>)messages;
        }, "outbox.get_pending_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<OutboxMessage>()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
                return;

            message.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            message.ErrorMessage = null;
        }, "outbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<OutboxMessage>()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
                return;

            message.ErrorMessage = errorMessage;
            message.RetryCount++;
            message.NextRetryAtUtc = nextRetryAtUtc;
        }, "outbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> GetPendingCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
            await _dbContext.Set<OutboxMessage>()
                .CountAsync(m => m.ProcessedAtUtc == null && m.RetryCount < maxRetries, cancellationToken),
            "outbox.get_pending_count_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> GetExhaustedCountAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        return await EitherHelpers.TryAsync(async () =>
            await _dbContext.Set<OutboxMessage>()
                .CountAsync(m => m.ProcessedAtUtc == null && m.RetryCount >= maxRetries, cancellationToken),
            "outbox.get_exhausted_count_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The requeued messages are loaded and changed on the tracked context, like the other state changes of
    /// this store, so the change is persisted by <see cref="SaveChangesAsync"/> within the caller's unit of work.
    /// </remarks>
    public async Task<Either<EncinaError, int>> RequeueExhaustedAsync(
        int maxRetries,
        IReadOnlyCollection<Guid>? messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        if (messageIds is { Count: 0 })
            return 0;

        return await EitherHelpers.TryAsync(async () =>
        {
            var exhausted = _dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedAtUtc == null && m.RetryCount >= maxRetries);

            var messages = new List<OutboxMessage>();
            if (messageIds is null)
            {
                messages.AddRange(await exhausted.ToListAsync(cancellationToken));
            }
            else
            {
                foreach (var chunk in messageIds.Distinct().Chunk(RequeueIdBatchSize))
                {
                    // A List<Guid> keeps Contains bound to the instance method; with C# 14 an array would bind
                    // to the span-based MemoryExtensions.Contains inside the expression tree.
                    var ids = chunk.ToList();
                    messages.AddRange(await exhausted.Where(m => ids.Contains(m.Id)).ToListAsync(cancellationToken));
                }
            }

            foreach (var message in messages)
            {
                message.RetryCount = 0;
                message.NextRetryAtUtc = null;
                message.ErrorMessage = null;
            }

            return messages.Count;
        }, "outbox.requeue_exhausted_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, "outbox.save_failed").ConfigureAwait(false);
    }
}
