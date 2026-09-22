using System.Runtime.CompilerServices;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina;

public sealed partial class Encina
{
    /// <inheritdoc />
    public async IAsyncEnumerable<Either<EncinaError, TItem>> Stream<TItem>(
        IStreamRequest<TItem> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!EncinaRequestGuards.TryValidateStreamRequest<TItem>(request, out var error))
        {
            Log.NullStreamRequest(_logger);
            yield return error;
            yield break;
        }

        await foreach (var item in StreamDispatcher.ExecuteAsync(this, request, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    internal static partial class Log
    {
        [LoggerMessage(EventId = 100, Level = LogLevel.Error, Message = "The stream request cannot be null.")]
        public static partial void NullStreamRequest(ILogger logger);

        [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "No registered IStreamRequestHandler was found for {RequestType} -> {ItemType}.")]
        public static partial void StreamHandlerMissing(ILogger logger, string requestType, string itemType);

        [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "Processing stream {RequestType} with {HandlerType}.")]
        public static partial void ProcessingStreamRequest(ILogger logger, string requestType, string handlerType);

        [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "Stream {RequestType} completed by {HandlerType}: {ItemCount} items yielded.")]
        public static partial void StreamCompleted(ILogger logger, string requestType, string handlerType, int itemCount);

        [LoggerMessage(EventId = 104, Level = LogLevel.Warning, Message = "The {RequestType} stream was cancelled.")]
        public static partial void StreamCancelled(ILogger logger, string requestType, Exception? exception);

        [LoggerMessage(EventId = 105, Level = LogLevel.Error, Message = "Unexpected error while processing stream {RequestType}.")]
        public static partial void StreamProcessingError(ILogger logger, string requestType, Exception exception);
    }
}
