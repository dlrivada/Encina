using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static LanguageExt.Prelude;

namespace SimpleMediator;

/// <summary>
/// Default <see cref="IMediator"/> implementation using Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Creates a scope per request, resolves handlers, behaviors, pre/post processors and publishes
/// notifications. Includes instrumentation via <see cref="MediatorDiagnostics"/>.
/// </remarks>
/// <remarks>
/// Creates a mediator instance using the provided scope factory.
/// </remarks>
/// <param name="scopeFactory">Factory used to create scopes per operation.</param>
/// <param name="logger">Optional logger for tracing and diagnostics.</param>
public sealed partial class SimpleMediator(IServiceScopeFactory scopeFactory, ILogger<SimpleMediator>? logger = null) : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), RequestHandlerBase> RequestHandlerCache = new();
    private static readonly ConcurrentDictionary<(Type Handler, Type Notification), Func<object, object?, CancellationToken, Task>> NotificationHandlerInvokerCache = new();

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<SimpleMediator> _logger = logger ?? NullLogger<SimpleMediator>.Instance;

    /// <inheritdoc />
    public ValueTask<Either<MediatorError, TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (!MediatorRequestGuards.TryValidateRequest<TResponse>(request, out var error))
        {
            Log.NullRequest(_logger);
            return new ValueTask<Either<MediatorError, TResponse>>(error);
        }
        return new ValueTask<Either<MediatorError, TResponse>>(RequestDispatcher.ExecuteAsync(this, request, cancellationToken));
    }

    /// <inheritdoc />
    public ValueTask<Either<MediatorError, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (!MediatorRequestGuards.TryValidateNotification(notification, out var error))
        {
            Log.NotificationNull(_logger);
            return new ValueTask<Either<MediatorError, Unit>>(error);
        }
        return new ValueTask<Either<MediatorError, Unit>>(NotificationDispatcher.ExecuteAsync(this, notification, cancellationToken));
    }

    private void LogSendOutcome<TResponse>(Type requestType, Type handlerType, Either<MediatorError, TResponse> outcome)
    {
        var resultInfo = ExtractOutcome(outcome);
        LogSendOutcomeCore(requestType, handlerType, resultInfo.IsSuccess, resultInfo.Error);
    }

    private void LogSendOutcomeCore(Type requestType, Type handlerType, bool isSuccess, MediatorError? error)
    {
        if (isSuccess)
        {
            Log.RequestCompleted(_logger, requestType.Name, handlerType.Name);
            return;
        }

        var effectiveError = error ?? MediatorErrors.Unknown;

        var errorCode = effectiveError.GetMediatorCode();
        var exception = effectiveError.Exception.Match(
            Some: ex => (Exception?)ex,
            None: () => null);

        if (IsCancellationCode(errorCode))
        {
            Log.RequestCancelled(_logger, requestType.Name, errorCode, exception);
            return;
        }

        Log.RequestFailed(_logger, requestType.Name, errorCode, effectiveError.Message, exception);
    }

    private static (bool IsSuccess, MediatorError? Error) ExtractOutcome<TResponse>(Either<MediatorError, TResponse> outcome)
        => outcome.Match(
            Left: err => (IsSuccess: false, Error: (MediatorError?)err),
            Right: _ => (IsSuccess: true, Error: (MediatorError?)null));

    private static RequestHandlerBase CreateRequestHandlerWrapper(Type requestType, Type responseType)
    {
        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
        return (RequestHandlerBase)Activator.CreateInstance(wrapperType)!;
    }

    private abstract class RequestHandlerBase
    {
        public abstract Type HandlerServiceType { get; }
        public abstract object? ResolveHandler(IServiceProvider provider);
        public abstract Task<object> Handle(SimpleMediator mediator, object request, object handler, IServiceProvider provider, CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase
        where TRequest : IRequest<TResponse>
    {
        private static readonly Type HandlerType = typeof(IRequestHandler<TRequest, TResponse>);

        public override Type HandlerServiceType => HandlerType;

        public override object? ResolveHandler(IServiceProvider provider)
            => provider.GetService(HandlerType);

        public override async Task<object> Handle(SimpleMediator mediator, object request, object handler, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
            var pipelineBuilder = new PipelineBuilder<TRequest, TResponse>(typedRequest, typedHandler, cancellationToken);
            var pipeline = pipelineBuilder.Build(provider);
            var outcome = await pipeline().ConfigureAwait(false);
            return outcome;
        }
    }

    // Exposed for tests that reflect on the private pipeline helpers to validate cancellation semantics.
    private static async Task<Option<MediatorError>> ExecutePostProcessorAsync<TRequest, TResponse>(
        IRequestPostProcessor<TRequest, TResponse> postProcessor,
        TRequest request,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken)
    {
        try
        {
            await postProcessor.Process(request, response, cancellationToken).ConfigureAwait(false);
            return Option<MediatorError>.None;
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var message = $"Post-processor {postProcessor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
                return Some(MediatorErrors.Create(MediatorErrorCodes.PostProcessorCancelled, message, ex));
            }

            var error = MediatorErrors.FromException(MediatorErrorCodes.PostProcessorException, ex, $"Error running {postProcessor.GetType().Name} for {typeof(TRequest).Name}.");
            return Some(error);
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException(MediatorErrorCodes.PostProcessorException, ex, $"Error running {postProcessor.GetType().Name} for {typeof(TRequest).Name}.");
            return Some(error);
        }
    }

    // Exposed for tests that reflect on the private notification helper to validate handler invocation semantics.
    private static Task<Either<MediatorError, Unit>> InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        return NotificationDispatcher.InvokeNotificationHandler(handler, notification, cancellationToken);
    }

    internal static bool IsCancellationCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return false;
        }

        return errorCode.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
    }

    private static partial class Log
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
