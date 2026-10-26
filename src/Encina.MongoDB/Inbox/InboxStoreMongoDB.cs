using Encina.Messaging.Inbox;
using Encina.MongoDB.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Inbox;

/// <summary>
/// MongoDB implementation of <see cref="IInboxStore"/>.
/// </summary>
public sealed class InboxStoreMongoDB : IInboxStore
{
    internal const string MarkProcessedNotFoundCode = "inbox.mark_processed_not_found";

    private readonly IMongoCollection<InboxMessage> _collection;
    private readonly ILogger<InboxStoreMongoDB> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMongoSessionAccessor? _sessionAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider. Defaults to <see cref="TimeProvider.System"/> if not specified.</param>
    /// <param name="sessionAccessor">
    /// The scope's transaction accessor. Following ADR-048, <c>GetMessageAsync</c>, the success
    /// <c>MarkAsProcessedAsync</c>, <c>GetExpiredMessagesAsync</c> and <c>RemoveExpiredMessagesAsync</c> run in its
    /// client session when a transaction is active; <c>AddAsync</c>, <c>MarkAsFailedAsync</c> and
    /// <c>CacheHandlerErrorAsync</c> always run without a session, so the inbox record and a cached handler error
    /// survive a rollback of the request.
    /// </param>
    public InboxStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<InboxStoreMongoDB> logger,
        TimeProvider? timeProvider = null,
        IMongoSessionAccessor? sessionAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<InboxMessage>(config.Collections.Inbox);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sessionAccessor = sessionAccessor;
    }

    private IClientSessionHandle? Session => _sessionAccessor?.CurrentSession;

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var message = await _collection
                .FindInSession(Session, Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (message is not null)
            {
                Log.FoundInboxMessage(_logger, messageId);
                return Option<IInboxMessage>.Some(message);
            }

            return Option<IInboxMessage>.None;
        }, "inbox.get_message_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await EitherHelpers.TryAsync(async () =>
        {
            var mongoMessage = message as InboxMessage ?? new InboxMessage
            {
                MessageId = message.MessageId,
                RequestType = message.RequestType,
                Response = message.Response,
                ErrorMessage = message.ErrorMessage,
                ReceivedAtUtc = message.ReceivedAtUtc,
                ProcessedAtUtc = message.ProcessedAtUtc,
                ExpiresAtUtc = message.ExpiresAtUtc,
                RetryCount = message.RetryCount,
                NextRetryAtUtc = message.NextRetryAtUtc
            };

            // Independent (ADR-048): the inbox record is written without the request's session.
            await _collection.InsertOneAsync(mongoMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
            Log.AddedInboxMessage(_logger, message.MessageId);
        }, "inbox.add_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        // Enlisted (ADR-048): the processed mark commits or aborts with the request. A mark that matches no
        // document is an error, never a silent success.
        var marked = await EitherHelpers.TryAsync(
            async () => await SetProcessedAsync(Session, messageId, response, cancellationToken).ConfigureAwait(false),
            "inbox.mark_processed_failed").ConfigureAwait(false);

        return marked.Bind(found => found
            ? Prelude.Right<EncinaError, Unit>(Unit.Default)
            : Prelude.Left<EncinaError, Unit>(EncinaErrors.Create(
                MarkProcessedNotFoundCode,
                "The inbox message to mark as processed was not found.")));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> CacheHandlerErrorAsync(string messageId, string response, CancellationToken cancellationToken = default)
    {
        // MongoDB writes are immediate and have no pipeline transaction to leave, so this is the same write as MarkAsProcessedAsync.
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        // Independent (ADR-048): written without the request's session so it survives a rollback.
        return await EitherHelpers.TryAsync(
            async () => { await SetProcessedAsync(null, messageId, response, cancellationToken).ConfigureAwait(false); },
            "inbox.cache_handler_error_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageId);

        return await EitherHelpers.TryAsync(async () =>
        {
            var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
            var update = Builders<InboxMessage>.Update
                .Set(m => m.ErrorMessage, errorMessage)
                .Set(m => m.NextRetryAtUtc, nextRetryAtUtc)
                .Inc(m => m.RetryCount, 1);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.ModifiedCount == 0)
            {
                Log.InboxMessageNotFoundForFailed(_logger, messageId);
            }
            else
            {
                Log.MarkedInboxMessageAsFailed(_logger, messageId, errorMessage);
            }
        }, "inbox.mark_failed_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await EitherHelpers.TryAsync(async () =>
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var filter = Builders<InboxMessage>.Filter.Lt(m => m.ExpiresAtUtc, now);

            var messages = await _collection
                .FindInSession(Session, filter)
                .Limit(batchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            Log.RetrievedExpiredInboxMessages(_logger, messages.Count);
            return (IEnumerable<IInboxMessage>)messages;
        }, "inbox.get_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        return await EitherHelpers.TryAsync(async () =>
        {
            var idList = messageIds.ToList();
            if (idList.Count == 0)
            {
                return;
            }

            var filter = Builders<InboxMessage>.Filter.In(m => m.MessageId, idList);
            var result = await _collection.DeleteManyInSessionAsync(Session, filter, cancellationToken).ConfigureAwait(false);

            Log.RemovedExpiredInboxMessages(_logger, result.DeletedCount);
        }, "inbox.remove_expired_failed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB operations are immediately persisted, no SaveChanges needed
        return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    /// <summary>
    /// Marks the message as processed with its response and clears any recorded error.
    /// Shared by <see cref="MarkAsProcessedAsync"/> and <see cref="CacheHandlerErrorAsync"/>.
    /// </summary>
    private async Task<bool> SetProcessedAsync(
        IClientSessionHandle? session,
        string messageId,
        string response,
        CancellationToken cancellationToken)
    {
        var filter = Builders<InboxMessage>.Filter.Eq(m => m.MessageId, messageId);
        var update = Builders<InboxMessage>.Update
            .Set(m => m.ProcessedAtUtc, _timeProvider.GetUtcNow().UtcDateTime)
            .Set(m => m.Response, response)
            .Unset(m => m.ErrorMessage);

        var result = await _collection.UpdateOneInSessionAsync(session, filter, update, cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount == 0)
        {
            Log.InboxMessageNotFoundForProcessed(_logger, messageId);
            return false;
        }

        Log.MarkedInboxMessageAsProcessed(_logger, messageId);
        return true;
    }
}
