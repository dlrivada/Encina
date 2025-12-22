using System.Collections.Concurrent;
using Encina.Dispatchers.Strategies;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// Default <see cref="IEncina"/> implementation using Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Creates a scope per request, resolves handlers, behaviors, pre/post processors and publishes
/// notifications. Includes instrumentation via <see cref="EncinaDiagnostics"/>.
/// </remarks>
/// <remarks>
/// Creates a Encina instance using the provided scope factory.
/// </remarks>
/// <param name="scopeFactory">Factory used to create scopes per operation.</param>
/// <param name="logger">Optional logger for tracing and diagnostics.</param>
/// <param name="notificationOptions">Optional notification dispatch options.</param>
public sealed partial class Encina(
    IServiceScopeFactory scopeFactory,
    ILogger<Encina>? logger = null,
    IOptions<NotificationDispatchOptions>? notificationOptions = null) : IEncina
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), RequestHandlerBase> RequestHandlerCache = new();
    private static readonly ConcurrentDictionary<(Type Handler, Type Notification), Func<object, object?, CancellationToken, Task<Either<EncinaError, Unit>>>> NotificationHandlerInvokerCache = new();

    internal readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    internal readonly ILogger<Encina> _logger = logger ?? NullLogger<Encina>.Instance;
    internal readonly NotificationDispatchOptions _notificationOptions = notificationOptions?.Value ?? new NotificationDispatchOptions();
    internal readonly INotificationDispatchStrategy _dispatchStrategy = CreateDispatchStrategy(notificationOptions?.Value ?? new NotificationDispatchOptions());

    private static INotificationDispatchStrategy CreateDispatchStrategy(NotificationDispatchOptions options) =>
        options.Strategy switch
        {
            NotificationDispatchStrategy.Parallel => new ParallelDispatchStrategy(options.MaxDegreeOfParallelism),
            NotificationDispatchStrategy.ParallelWhenAll => new ParallelWhenAllDispatchStrategy(options.MaxDegreeOfParallelism),
            _ => SequentialDispatchStrategy.Instance
        };

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (!EncinaRequestGuards.TryValidateRequest<TResponse>(request, out Either<EncinaError, TResponse> error))
        {
            Log.NullRequest(_logger);
            return new ValueTask<Either<EncinaError, TResponse>>(error);
        }
        return new ValueTask<Either<EncinaError, TResponse>>(RequestDispatcher.ExecuteAsync(this, request, cancellationToken));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (!EncinaRequestGuards.TryValidateNotification(notification, out Either<EncinaError, Unit> error))
        {
            Log.NotificationNull(_logger);
            return new ValueTask<Either<EncinaError, Unit>>(error);
        }
        return new ValueTask<Either<EncinaError, Unit>>(NotificationDispatcher.ExecuteAsync(this, notification, cancellationToken));
    }

    private void LogSendOutcome<TResponse>(Type requestType, Type handlerType, Either<EncinaError, TResponse> outcome)
    {
        (bool IsSuccess, EncinaError? Error) = ExtractOutcome(outcome);
        LogSendOutcomeCore(requestType, handlerType, IsSuccess, Error);
    }

    private void LogSendOutcomeCore(Type requestType, Type handlerType, bool isSuccess, EncinaError? error)
    {
        if (isSuccess)
        {
            Log.RequestCompleted(_logger, requestType.Name, handlerType.Name);
            return;
        }

        EncinaError effectiveError = error ?? EncinaErrors.Unknown;

        string errorCode = effectiveError.GetEncinaCode();
        Exception? exception = effectiveError.Exception.Match(
            Some: ex => (Exception?)ex,
            None: () => null);

        if (IsCancellationCode(errorCode))
        {
            Log.RequestCancelled(_logger, requestType.Name, errorCode, exception);
            return;
        }

        Log.RequestFailed(_logger, requestType.Name, errorCode, effectiveError.Message, exception);
    }

    private static (bool IsSuccess, EncinaError? Error) ExtractOutcome<TResponse>(Either<EncinaError, TResponse> outcome)
        => outcome.Match(
            Left: err => (IsSuccess: false, Error: (EncinaError?)err),
            Right: _ => (IsSuccess: true, Error: null));

    private static RequestHandlerBase CreateRequestHandlerWrapper(Type requestType, Type responseType)
    {
        Type wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
        return (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
    }

    private abstract class RequestHandlerBase
    {
        public abstract Type HandlerServiceType { get; }
        public abstract object? ResolveHandler(IServiceProvider provider);
        public abstract Task<object> Handle(Encina Encina, object request, object handler, IServiceProvider provider, CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase
        where TRequest : IRequest<TResponse>
    {
        private static readonly Type HandlerType = typeof(IRequestHandler<TRequest, TResponse>);

        public override Type HandlerServiceType => HandlerType;

        public override object? ResolveHandler(IServiceProvider provider)
            => provider.GetService(HandlerType);

        public override async Task<object> Handle(Encina Encina, object request, object handler, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
            IRequestContext context = RequestContext.Create();
            var pipelineBuilder = new PipelineBuilder<TRequest, TResponse>(typedRequest, typedHandler, context, cancellationToken);
            RequestHandlerCallback<TResponse> pipeline = pipelineBuilder.Build(provider);
            Either<EncinaError, TResponse> outcome = await pipeline().ConfigureAwait(false);
            return outcome;
        }
    }

    internal static bool IsCancellationCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return false;
        }

        return errorCode.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
    }

    internal static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "The request cannot be null.")]
        public static partial void NullRequest(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "No registered IRequestHandler was found for {RequestType} -> {ResponseType}.")]
        public static partial void HandlerMissing(ILogger logger, string requestType, string responseType);

        [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Processing {RequestType} with {HandlerType}.")]
        public static partial void ProcessingRequest(ILogger logger, string requestType, string handlerType);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Handler {HandlerType} returned an unexpected type while processing {RequestType}.")]
        public static partial void HandlerReturnedUnexpectedType(ILogger logger, string handlerType, string requestType);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "The {RequestType} request was cancelled.")]
        public static partial void RequestCancelledDuringSend(ILogger logger, string requestType);

        [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Unexpected error while processing {RequestType}.")]
        public static partial void RequestProcessingError(ILogger logger, string requestType, Exception exception);

        [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "The notification cannot be null.")]
        public static partial void NotificationNull(ILogger logger);

        [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "No handlers were found for the {NotificationType} notification.")]
        public static partial void NoNotificationHandlers(ILogger logger, string notificationType);

        [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Sending notification {NotificationType} to {HandlerType}.")]
        public static partial void SendingNotification(ILogger logger, string notificationType, string handlerType);

        [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Publishing {NotificationType} with {HandlerType} was cancelled.")]
        public static partial void NotificationCancelled(ILogger logger, string notificationType, string handlerType, Exception? exception);

        [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error while publishing notification {NotificationType} with {HandlerType}.")]
        public static partial void NotificationHandlerException(ILogger logger, string notificationType, string handlerType, Exception exception);

        [LoggerMessage(EventId = 12, Level = LogLevel.Error, Message = "Error while publishing notification {NotificationType} with {HandlerType}: {Message}")]
        public static partial void NotificationHandlerFailure(ILogger logger, string notificationType, string handlerType, string message);

        [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "Request {RequestType} completed by {HandlerType}.")]
        public static partial void RequestCompleted(ILogger logger, string requestType, string handlerType);

        [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "The {RequestType} request was cancelled ({Reason}).")]
        public static partial void RequestCancelled(ILogger logger, string requestType, string reason, Exception? exception);

        [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "The {RequestType} request failed ({Reason}): {Message}")]
        public static partial void RequestFailed(ILogger logger, string requestType, string reason, string message, Exception? exception);
    }
}
