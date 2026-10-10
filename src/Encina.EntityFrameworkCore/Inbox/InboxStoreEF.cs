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
/// This implementation provides idempotent message processing with EF Core (ADR-048). Writes that must
/// survive the rollback of the business transaction (<see cref="AddAsync"/>, <see cref="MarkAsFailedAsync"/>,
/// <see cref="CacheHandlerErrorAsync"/>) are immediate and run on an isolated context built from the injected
/// context's options, so they are neither flushed with nor rolled back by the business unit of work.
/// <see cref="MarkAsProcessedAsync"/> runs on the injected context, so it commits atomically with the business
/// transaction when one is open. On relational providers the updates are single atomic UPDATE statements
/// (<c>RetryCount + 1</c> included). Requirements and limits: the context type must expose the standard public
/// constructor taking its <c>DbContextOptions&lt;TContext&gt;</c>; the context must be configured with a
/// connection string, not a shared <c>DbConnection</c> instance (refused at the first write); each independent
/// write uses a second pooled connection while the business transaction holds its own; and under a
/// repeatable-read or serializable <c>[Transaction]</c> the lookup lock of the business connection can block
/// an independent write.
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

    /// <summary>
    /// Verifies that <paramref name="contextType"/> can be instantiated from its own
    /// <see cref="DbContextOptions{TContext}"/>, which the inbox needs to create its isolated context.
    /// </summary>
    /// <param name="contextType">The application's DbContext type.</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="contextType"/> has no public constructor taking <c>DbContextOptions&lt;TContext&gt;</c>.
    /// </exception>
    public static void ValidateContextType(Type contextType)
    {
        ArgumentNullException.ThrowIfNull(contextType);

        var optionsType = typeof(DbContextOptions<>).MakeGenericType(contextType);
        if (contextType.GetConstructor([optionsType]) is null)
        {
            throw new InvalidOperationException(
                $"{contextType.Name} must expose a public constructor taking DbContextOptions<{contextType.Name}> to use the inbox: the inbox records attempts on an isolated context created from those options (ADR-048).");
        }
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
            // Immediate write on an isolated context:
            // the inbox record must not depend on, or be rolled back with, the business unit of work.
            await using var isolated = CreateIsolatedContext();
            await isolated.Set<InboxMessage>().AddAsync(efMessage, cancellationToken);
            await isolated.SaveChangesAsync(cancellationToken);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    // The inbox keeps its own record of every attempt in a unit of work of its own: the request's
    // transaction (TransactionPipelineBehavior) rolls back on a Left, and a failed attempt or a cached
    // response must survive that rollback. The isolated context shares the options (and so the model and
    // the database) of the injected one, but opens its own connection from the connection string and never flushes the business changes
    // the injected context may be tracking. It requires the context type to expose the standard public
    // constructor taking its own DbContextOptions<TContext>.
    private DbContext CreateIsolatedContext()
    {
        var options = ((IInfrastructure<IServiceProvider>)_dbContext).GetService<IDbContextOptions>();

        // A DbConnection instance handed to UseSqlServer/UseNpgsql/... is shared by every context built from
        // these options, so the "isolated" writes would join the business transaction: refuse instead of
        // silently losing the inbox record on rollback.
        if (options.Extensions.OfType<RelationalOptionsExtension>().Any(e => e.Connection is not null))
        {
            throw new InvalidOperationException(
                "The inbox cannot isolate its writes from the business transaction because the DbContext is configured with a shared DbConnection instance; configure it with a connection string.");
        }

        return (DbContext)Activator.CreateInstance(_dbContext.GetType(), options)!;
    }

    // Applies one change to a stored message on the given context. Relational providers run a single UPDATE (so
    // RetryCount + 1 is atomic, like the SQL and MongoDB stores) that joins the context's current transaction, if
    // any; any non-relational provider (the in-memory test provider) loads the tracked-or-stored entity and
    // mutates it, which is not atomic and is saved by the caller (the isolated flavour saves immediately).
    private static async Task UpdateAsync(
        DbContext context,
        string messageId,
        Action<UpdateSettersBuilder<InboxMessage>> relational,
        Action<InboxMessage> tracked,
        bool saveImmediately,
        CancellationToken cancellationToken)
    {
        var set = context.Set<InboxMessage>();

        if (context.Database.IsRelational())
        {
            await set.Where(m => m.MessageId == messageId).ExecuteUpdateAsync(relational, cancellationToken);
            return;
        }

        var message = set.Local.FirstOrDefault(m => m.MessageId == messageId)
            ?? await set.FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
        if (message is null)
            return;

        tracked(message);

        if (saveImmediately)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    // Independent write: survives the rollback of the business transaction (own context, own connection).
    private async Task UpdateIsolatedAsync(
        string messageId,
        Action<UpdateSettersBuilder<InboxMessage>> relational,
        Action<InboxMessage> tracked,
        CancellationToken cancellationToken)
    {
        await using var isolated = CreateIsolatedContext();
        await UpdateAsync(isolated, messageId, relational, tracked, saveImmediately: true, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(response);

        return await EitherHelpers.TryAsync(async () =>
        {
            var processedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            // Enlisted: the UPDATE runs on the injected context, so it joins the business transaction when
            // one is open (committed or rolled back with the business effect) and is immediate otherwise.
            await UpdateAsync(
                _dbContext,
                messageId,
                s => ProcessedSetters(s, response, processedAtUtc),
                m => ApplyProcessed(m, response, processedAtUtc),
                saveImmediately: false,
                cancellationToken);
        }, "inbox.mark_processed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> CacheHandlerErrorAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(response);

        return await EitherHelpers.TryAsync(async () =>
        {
            var processedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            await UpdateIsolatedAsync(
                messageId,
                s => ProcessedSetters(s, response, processedAtUtc),
                m => ApplyProcessed(m, response, processedAtUtc),
                cancellationToken);
        }, "inbox.cache_handler_error_failed").ConfigureAwait(false);
    }

    private static void ProcessedSetters(UpdateSettersBuilder<InboxMessage> s, string response, DateTime processedAtUtc) =>
        s.SetProperty(m => m.Response, response)
            .SetProperty(m => m.ProcessedAtUtc, processedAtUtc)
            .SetProperty(m => m.ErrorMessage, (string?)null);

    private static void ApplyProcessed(InboxMessage m, string response, DateTime processedAtUtc)
    {
        m.Response = response;
        m.ProcessedAtUtc = processedAtUtc;
        m.ErrorMessage = null;
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
