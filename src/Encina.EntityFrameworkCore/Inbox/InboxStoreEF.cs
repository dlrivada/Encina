using Encina.Messaging.Inbox;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace Encina.EntityFrameworkCore.Inbox;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides idempotent message processing with EF Core. The writes that record
/// an attempt (<see cref="AddAsync"/>, <see cref="MarkAsProcessedAsync"/>, <see cref="MarkAsFailedAsync"/>)
/// are immediate and run on an isolated context built from the injected context's options, so they are
/// neither flushed with nor rolled back by the request's business transaction, like the ADO.NET, Dapper and
/// MongoDB stores. On relational providers <see cref="MarkAsFailedAsync"/> increments <c>RetryCount</c> in a
/// single atomic UPDATE. The context type must expose the standard public constructor taking its
/// <c>DbContextOptions&lt;TContext&gt;</c>.
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
            // No tracking: the isolated writes above change the row behind the context's back.
            var message = await _dbContext.Set<InboxMessage>()
                .AsNoTracking()
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
            // Immediate write on an isolated context, like the ADO.NET, Dapper and MongoDB stores:
            // the inbox record must not depend on, or be rolled back with, the business unit of work.
            await using var isolated = CreateIsolatedContext();
            await isolated.Set<InboxMessage>().AddAsync(efMessage, cancellationToken);
            await isolated.SaveChangesAsync(cancellationToken);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    // The inbox keeps its own record of every attempt in a unit of work of its own: the request's
    // transaction (TransactionPipelineBehavior) rolls back on a Left, and a failed attempt or a cached
    // response must survive that rollback. The isolated context shares the options (and so the model and
    // the database) of the injected one, but uses its own connection and never flushes the business changes
    // the injected context may be tracking. It requires the context type to expose the standard public
    // constructor taking its own DbContextOptions<TContext>.
    private DbContext CreateIsolatedContext()
    {
        var options = ((IInfrastructure<IServiceProvider>)_dbContext).GetService<IDbContextOptions>();
        return (DbContext)Activator.CreateInstance(_dbContext.GetType(), options)!;
    }

    // Applies one change to a stored message. Relational providers run a single UPDATE (so RetryCount + 1 is
    // atomic, like the SQL and MongoDB stores); the non-relational test provider has no concurrent writers,
    // so it loads, mutates and saves.
    private async Task UpdateIsolatedAsync(
        string messageId,
        Action<UpdateSettersBuilder<InboxMessage>> relational,
        Action<InboxMessage> tracked,
        CancellationToken cancellationToken)
    {
        await using var isolated = CreateIsolatedContext();
        var set = isolated.Set<InboxMessage>();

        if (isolated.Database.IsRelational())
        {
            await set.Where(m => m.MessageId == messageId).ExecuteUpdateAsync(relational, cancellationToken);
            return;
        }

        var message = await set.FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
        if (message is null)
            return;

        tracked(message);
        await isolated.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(response);

        return await EitherHelpers.TryAsync(async () =>
        {
            var processedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await UpdateIsolatedAsync(
                messageId,
                s => s.SetProperty(m => m.Response, response)
                    .SetProperty(m => m.ProcessedAtUtc, processedAtUtc)
                    .SetProperty(m => m.ErrorMessage, (string?)null),
                m =>
                {
                    m.Response = response;
                    m.ProcessedAtUtc = processedAtUtc;
                    m.ErrorMessage = null;
                },
                cancellationToken);
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
            await UpdateIsolatedAsync(
                messageId,
                s => s.SetProperty(m => m.ErrorMessage, errorMessage)
                    .SetProperty(m => m.RetryCount, m => m.RetryCount + 1)
                    .SetProperty(m => m.NextRetryAtUtc, nextRetryAtUtc),
                m =>
                {
                    m.ErrorMessage = errorMessage;
                    m.RetryCount++;
                    m.NextRetryAtUtc = nextRetryAtUtc;
                },
                cancellationToken);
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
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
