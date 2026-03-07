using Encina.Messaging.Scheduling;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Scheduling;

/// <summary>
/// Entity Framework Core implementation of <see cref="IScheduledMessageStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides message scheduling support using EF Core's
/// query capabilities and transaction support for reliable delayed execution.
/// </para>
/// </remarks>
public sealed class ScheduledMessageStoreEF : IScheduledMessageStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledMessageStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public ScheduledMessageStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not ScheduledMessage efMessage)
        {
            return EncinaErrors.Create("scheduling.invalid_type",
                $"ScheduledMessageStoreEF requires messages of type {nameof(ScheduledMessage)}, got {message.GetType().Name}");
        }

        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.Set<ScheduledMessage>().AddAsync(efMessage, cancellationToken);
        }, "scheduling.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IEnumerable<IScheduledMessage>>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var messages = await _dbContext.Set<ScheduledMessage>()
                .Where(m =>
                    m.ProcessedAtUtc == null &&
                    m.ScheduledAtUtc <= now &&
                    (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now) &&
                    m.RetryCount < maxRetries)
                .OrderBy(m => m.ScheduledAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            return (IEnumerable<IScheduledMessage>)messages;
        }, "scheduling.get_due_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<ScheduledMessage>()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
                return;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            message.ProcessedAtUtc = now;
            message.LastExecutedAtUtc = now;
            message.ErrorMessage = null;
        }, "scheduling.mark_processed_failed").ConfigureAwait(false);
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
            var message = await _dbContext.Set<ScheduledMessage>()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
                return;

            message.ErrorMessage = errorMessage;
            message.RetryCount++;
            message.NextRetryAtUtc = nextRetryAtUtc;
        }, "scheduling.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<ScheduledMessage>()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
                return;

            message.ScheduledAtUtc = nextScheduledAtUtc;
            message.ProcessedAtUtc = null;
            message.ErrorMessage = null;
            message.RetryCount = 0;
            message.NextRetryAtUtc = null;
        }, "scheduling.reschedule_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<ScheduledMessage>()
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message != null)
            {
                _dbContext.Set<ScheduledMessage>().Remove(message);
            }
        }, "scheduling.cancel_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, "scheduling.save_failed").ConfigureAwait(false);
    }
}
