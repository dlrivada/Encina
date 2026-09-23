using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Default implementation of <see cref="IDeadLetterManager"/>.
/// </summary>
public sealed class DeadLetterManager : IDeadLetterManager
{
    private readonly IDeadLetterStore _store;
    private readonly DeadLetterOrchestrator _orchestrator;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadLetterManager> _logger;
    private readonly IMessageSerializer _messageSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterManager"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="orchestrator">The orchestrator.</param>
    /// <param name="serviceProvider">The service provider for resolving IEncina.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageSerializer">
    /// The message serializer used to read back the persisted request payload, matching
    /// whatever serializer (plain or encrypting) wrote it.
    /// </param>
    public DeadLetterManager(
        IDeadLetterStore store,
        DeadLetterOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        ILogger<DeadLetterManager> logger,
        IMessageSerializer messageSerializer)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageSerializer);

        _store = store;
        _orchestrator = orchestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messageSerializer = messageSerializer;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReplayResult>> ReplayAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var messageResult = await _store.GetAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (messageResult.IsLeft)
            return messageResult.LeftToArray()[0];

        var messageOpt = messageResult.Match(Right: o => o, Left: _ => Option<IDeadLetterMessage>.None);
        if (messageOpt.IsNone)
        {
            return EncinaError.New($"[{DeadLetterErrorCodes.NotFound}] Dead letter message {messageId} not found");
        }

        var message = messageOpt.Match(Some: m => m, None: () => default!);

        if (message.IsReplayed)
        {
            return EncinaError.New($"[{DeadLetterErrorCodes.AlreadyReplayed}] Message {messageId} has already been replayed");
        }

        if (message.IsExpired)
        {
            return EncinaError.New($"[{DeadLetterErrorCodes.Expired}] Message {messageId} has expired");
        }

        DeadLetterLog.ReplayingMessage(_logger, messageId, message.RequestType);

        try
        {
            // Deserialize the request
            var requestType = Type.GetType(message.RequestType);
            if (requestType is null)
            {
                var error = $"[{DeadLetterErrorCodes.DeserializationFailed}] Cannot resolve type: {message.RequestType}";
                await _store.MarkAsReplayedAsync(messageId, $"Failed: {error}", cancellationToken);
                await _store.SaveChangesAsync(cancellationToken);
                return EncinaError.New(error);
            }

            var request = _messageSerializer.Deserialize(message.RequestContent, requestType);
            if (request is null)
            {
                var error = $"[{DeadLetterErrorCodes.DeserializationFailed}] Failed to deserialize request content";
                await _store.MarkAsReplayedAsync(messageId, $"Failed: {error}", cancellationToken);
                await _store.SaveChangesAsync(cancellationToken);
                return EncinaError.New(error);
            }

            // Get IEncina to replay the request
            var encina = _serviceProvider.GetService(typeof(IEncina)) as IEncina;
            if (encina is null)
            {
                var error = $"[{DeadLetterErrorCodes.ReplayFailed}] IEncina service not available";
                await _store.MarkAsReplayedAsync(messageId, $"Failed: {error}", cancellationToken);
                await _store.SaveChangesAsync(cancellationToken);
                return EncinaError.New(error);
            }

            // Replay through IEncina.Send, typed by the request's runtime type
            var replayResult = await ReplayRequestAsync(encina, request, messageId, cancellationToken);

            await _store.MarkAsReplayedAsync(
                messageId,
                replayResult.Success ? "Success" : replayResult.ErrorMessage ?? "Failed",
                cancellationToken);
            await _store.SaveChangesAsync(cancellationToken);

            return replayResult;
        }
        catch (Exception ex)
        {
            DeadLetterLog.MessageReplayException(_logger, ex, messageId);

            var errorMessage = $"[{DeadLetterErrorCodes.ReplayFailed}] Exception during replay: {ex.Message}";
            await _store.MarkAsReplayedAsync(messageId, $"Failed: {errorMessage}", cancellationToken);
            await _store.SaveChangesAsync(cancellationToken);

            return ReplayResult.Failed(messageId, errorMessage);
        }
    }

    private async Task<ReplayResult> ReplayRequestAsync(
        IEncina encina,
        object request,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await RuntimeTypeRequestDispatcher.SendAsync(encina, request, cancellationToken).ConfigureAwait(false);

            // A Left outcome is a failed replay: the request ran and its handler (or a behavior) failed.
            if (outcome.IsLeft)
            {
                var failure = outcome.Match(Right: _ => string.Empty, Left: error => error.Message);
                var error = $"Replay failed: {failure}";
                DeadLetterLog.MessageReplayFailed(_logger, messageId, error);
                return ReplayResult.Failed(messageId, error);
            }

            DeadLetterLog.MessageReplayedSuccessfully(_logger, messageId);
            return ReplayResult.Succeeded(messageId);
        }
        catch (Exception ex)
        {
            var error = $"Replay failed: {ex.Message}";
            DeadLetterLog.MessageReplayFailed(_logger, messageId, error);
            return ReplayResult.Failed(messageId, error);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, BatchReplayResult>> ReplayAllAsync(
        DeadLetterFilter filter,
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Ensure we only get non-replayed messages
        filter.ExcludeReplayed = true;

        var messagesResult = await _store.GetMessagesAsync(filter, 0, maxMessages, cancellationToken).ConfigureAwait(false);
        if (messagesResult.IsLeft)
            return messagesResult.LeftToArray()[0];

        var messages = messagesResult.Match(Right: m => m, Left: _ => Enumerable.Empty<IDeadLetterMessage>());
        var results = new List<ReplayResult>();

        DeadLetterLog.BatchReplayStarted(_logger, messages.Count());

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ReplayAsync(message.Id, cancellationToken);
            result.Match(
                Right: r => results.Add(r),
                Left: error => results.Add(ReplayResult.Failed(message.Id, error.Message)));
        }

        var batchResult = new BatchReplayResult
        {
            TotalProcessed = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            Results = results
        };

        DeadLetterLog.BatchReplayCompleted(_logger, batchResult.TotalProcessed, batchResult.SuccessCount, batchResult.FailureCount);

        return batchResult;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Option<IDeadLetterMessage>>> GetMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetMessagesAsync(filter, skip, take, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await _store.GetCountAsync(filter, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, DeadLetterStatistics>> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.GetStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var deleteResult = await _store.DeleteAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsLeft)
            return deleteResult.LeftToArray()[0];

        var deleted = deleteResult.Match(Right: d => d, Left: _ => false);

        if (!deleted)
        {
            return EncinaErrors.Create(DeadLetterErrorCodes.DeleteFailed, $"Dead letter message {messageId} not found or could not be deleted");
        }

        return Unit.Default;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> DeleteAllAsync(
        DeadLetterFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var messagesResult = await _store.GetMessagesAsync(filter, 0, int.MaxValue, cancellationToken).ConfigureAwait(false);
        if (messagesResult.IsLeft)
            return messagesResult.LeftToArray()[0];

        var messages = messagesResult.Match(Right: m => m, Left: _ => Enumerable.Empty<IDeadLetterMessage>());
        var count = 0;

        foreach (var message in messages)
        {
            var deleteResult = await _store.DeleteAsync(message.Id, cancellationToken).ConfigureAwait(false);
            var deleted = deleteResult.Match(Right: d => d, Left: _ => false);
            if (deleted)
            {
                count++;
            }
        }

        if (count > 0)
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            DeadLetterLog.MessagesDeleted(_logger, count);
        }

        return count;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, int>> CleanupExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.CleanupExpiredAsync(cancellationToken);
    }
}
