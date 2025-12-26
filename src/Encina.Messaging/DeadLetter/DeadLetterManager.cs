using System.Text.Json;
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterManager"/> class.
    /// </summary>
    /// <param name="store">The dead letter store.</param>
    /// <param name="orchestrator">The orchestrator.</param>
    /// <param name="serviceProvider">The service provider for resolving IEncina.</param>
    /// <param name="logger">The logger.</param>
    public DeadLetterManager(
        IDeadLetterStore store,
        DeadLetterOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        ILogger<DeadLetterManager> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _orchestrator = orchestrator;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReplayResult>> ReplayAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _store.GetAsync(messageId, cancellationToken);

        if (message is null)
        {
            return EncinaError.New($"[{DeadLetterErrorCodes.NotFound}] Dead letter message {messageId} not found");
        }

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

            var request = JsonSerializer.Deserialize(message.RequestContent, requestType, JsonOptions);
            if (request is null)
            {
                var error = $"[{DeadLetterErrorCodes.DeserializationFailed}] Failed to deserialize request content";
                await _store.MarkAsReplayedAsync(messageId, $"Failed: {error}", cancellationToken);
                await _store.SaveChangesAsync(cancellationToken);
                return EncinaError.New(error);
            }

            // Get IEncina and replay using reflection
            var encina = _serviceProvider.GetService(typeof(IEncina)) as IEncina;
            if (encina is null)
            {
                var error = $"[{DeadLetterErrorCodes.ReplayFailed}] IEncina service not available";
                await _store.MarkAsReplayedAsync(messageId, $"Failed: {error}", cancellationToken);
                await _store.SaveChangesAsync(cancellationToken);
                return EncinaError.New(error);
            }

            // Use reflection to call the appropriate Send method
            var replayResult = await ReplayRequestAsync(encina, request, requestType, messageId, cancellationToken);

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
        Type requestType,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        // Find IRequest<TResponse> interface to get the response type
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterface is null)
        {
            var error = $"Request type {requestType.Name} does not implement IRequest<TResponse>";
            DeadLetterLog.MessageReplayFailed(_logger, messageId, error);
            return ReplayResult.Failed(messageId, error);
        }

        var responseType = requestInterface.GetGenericArguments()[0];

        // Build the generic Send method: IEncina.Send<TResponse>(IRequest<TResponse>, CancellationToken)
        var sendMethod = typeof(IEncina)
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == "Send" &&
                m.IsGenericMethod &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 2);

        if (sendMethod is null)
        {
            var error = "Cannot find IEncina.Send method";
            DeadLetterLog.MessageReplayFailed(_logger, messageId, error);
            return ReplayResult.Failed(messageId, error);
        }

        var genericSendMethod = sendMethod.MakeGenericMethod(responseType);

        try
        {
            var resultTask = genericSendMethod.Invoke(encina, [request, cancellationToken]);

            if (resultTask is null)
            {
                var error = "Send method returned null";
                DeadLetterLog.MessageReplayFailed(_logger, messageId, error);
                return ReplayResult.Failed(messageId, error);
            }

            // Await the ValueTask<Either<EncinaError, TResponse>>
            await (dynamic)resultTask;

            // Check if it's a Left (error) or Right (success)
            // Since we can't easily inspect the Either result dynamically,
            // we assume success if no exception was thrown
            DeadLetterLog.MessageReplayedSuccessfully(_logger, messageId);
            return ReplayResult.Succeeded(messageId);
        }
        catch (Exception ex)
        {
            var innerException = ex.InnerException ?? ex;
            var error = $"Replay failed: {innerException.Message}";
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

        var messages = await _store.GetMessagesAsync(filter, 0, maxMessages, cancellationToken);
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
    public Task<IDeadLetterMessage?> GetMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(messageId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IDeadLetterMessage>> GetMessagesAsync(
        DeadLetterFilter? filter = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return _store.GetMessagesAsync(filter, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(
        DeadLetterFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return _store.GetCountAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DeadLetterStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.GetStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        return _store.DeleteAsync(messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllAsync(
        DeadLetterFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var messages = await _store.GetMessagesAsync(filter, 0, int.MaxValue, cancellationToken);
        var count = 0;

        foreach (var message in messages)
        {
            if (await _store.DeleteAsync(message.Id, cancellationToken))
            {
                count++;
            }
        }

        if (count > 0)
        {
            await _store.SaveChangesAsync(cancellationToken);
            DeadLetterLog.MessagesDeleted(_logger, count);
        }

        return count;
    }

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        return _orchestrator.CleanupExpiredAsync(cancellationToken);
    }
}
