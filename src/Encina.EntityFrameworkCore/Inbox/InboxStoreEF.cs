using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Inbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides idempotent message processing using EF Core's
/// change tracking and transaction support.
/// </para>
/// </remarks>
public sealed class InboxStoreEF : IInboxStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public InboxStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<InboxMessage>()
                .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

            return message is not null
                ? Option<IInboxMessage>.Some(message)
                : Option<IInboxMessage>.None;
        }, "inbox.get_message_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message is not InboxMessage efMessage)
        {
            return EncinaErrors.Create("inbox.invalid_type",
                $"InboxStoreEF requires messages of type {nameof(InboxMessage)}, got {message.GetType().Name}");
        }

        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.Set<InboxMessage>().AddAsync(efMessage, cancellationToken);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(response);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<InboxMessage>()
                .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

            if (message == null)
                return;

            message.Response = response;
            message.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            message.ErrorMessage = null;
        }, "inbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(errorMessage);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<InboxMessage>()
                .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

            if (message == null)
                return;

            message.ErrorMessage = errorMessage;
            message.RetryCount++;
            message.NextRetryAtUtc = nextRetryAtUtc;
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _dbContext.Set<InboxMessage>()
                .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

            if (message != null)
            {
                message.RetryCount++;
            }
        }, "inbox.increment_retry_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var messages = await _dbContext.Set<InboxMessage>()
                .Where(m => m.ExpiresAtUtc <= now)
                .OrderBy(m => m.ExpiresAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            return (IEnumerable<IInboxMessage>)messages;
        }, "inbox.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        return await EitherHelpers.TryAsync(async () =>
        {
            var messages = await _dbContext.Set<InboxMessage>()
                .Where(m => messageIds.Contains(m.MessageId))
                .ToListAsync(cancellationToken);

            _dbContext.Set<InboxMessage>().RemoveRange(messages);
        }, "inbox.remove_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }, "inbox.save_failed").ConfigureAwait(false);
    }
}
