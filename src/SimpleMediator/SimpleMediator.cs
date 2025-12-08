using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SimpleMediator;

/// <summary>
/// Default <see cref="IMediator"/> implementation using Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Creates a scope per request, resolves handlers, behaviors, pre/post processors and publishes
/// notifications. Includes instrumentation via <see cref="MediatorDiagnostics"/>.
/// </remarks>
public sealed class SimpleMediator : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), RequestHandlerBase> RequestHandlerCache = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimpleMediator> _logger;

    /// <summary>
    /// Creates a mediator instance using the provided scope factory.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create scopes per operation.</param>
    /// <param name="logger">Optional logger for tracing and diagnostics.</param>
    public SimpleMediator(IServiceScopeFactory scopeFactory, ILogger<SimpleMediator>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? NullLogger<SimpleMediator>.Instance;
    }

    /// <inheritdoc />
    public async Task<Either<Error, TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            const string message = "The request cannot be null.";
            _logger.LogError(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.request.null", message));
        }

        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var requestType = request.GetType();
        var dispatcher = RequestHandlerCache.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => CreateRequestHandlerWrapper(key.Request, key.Response));

        var handler = dispatcher.ResolveHandler(serviceProvider);

        if (handler is null)
        {
            var message = $"No registered IRequestHandler was found for {requestType.Name} -> {typeof(TResponse).Name}.";
            _logger.LogError(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.handler.missing", message));
        }

        try
        {
            _logger.LogDebug("Processing {RequestType} with {HandlerType}.", requestType.Name, handler.GetType().Name);
            var boxedOutcome = await dispatcher
                .Handle(this, request, handler, serviceProvider, cancellationToken)
                .ConfigureAwait(false);

            if (boxedOutcome is not Either<Error, TResponse> outcome)
            {
                var message = $"Handler {handler.GetType().Name} returned an unexpected type while processing {requestType.Name}.";
                _logger.LogError(message);
                return Left<Error, TResponse>(MediatorErrors.Create("mediator.handler.invalid_result", message));
            }

            var resultInfo = ExtractOutcome(outcome);
            LogSendOutcomeCore(requestType, handler.GetType(), resultInfo.IsSuccess, resultInfo.Error);
            return outcome;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"The {requestType.Name} request was cancelled.";
            _logger.LogWarning(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.request.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.pipeline.exception", ex, $"Unexpected error while processing {requestType.Name}.");
            _logger.LogError(ex, error.Message);
            return Left<Error, TResponse>(error);
        }
    }

    /// <inheritdoc />
    public async Task<Either<Error, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null)
        {
            const string message = "The notification cannot be null.";
            _logger.LogError(message);
            return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.null", message));
        }

        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var notificationType = notification.GetType();
        using var activity = MediatorDiagnostics.ActivitySource.StartActivity("SimpleMediator.Publish", ActivityKind.Internal);
        activity?.SetTag("mediator.notification_type", notificationType.FullName);

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = serviceProvider.GetServices(handlerType)?.Cast<object>().ToList() ?? new List<object>();

        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handlers were found for the {NotificationType} notification.", notificationType.Name);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Right<Error, Unit>(Unit.Default);
        }

        foreach (var handler in handlers)
        {
            _logger.LogDebug("Sending notification {NotificationType} to {HandlerType}.", notificationType.Name, handler.GetType().Name);
            var result = await InvokeNotificationHandler(handler, notification, cancellationToken).ConfigureAwait(false);
            if (result.IsLeft)
            {
                var error = result.Match(
                    Left: err => err,
                    Right: _ => MediatorErrors.Unknown);

                activity?.SetStatus(ActivityStatusCode.Error, error.Message);

                var errorCode = error.GetMediatorCode();
                var exception = error.Exception.Match(
                    Some: ex => (Exception?)ex,
                    None: () => null);

                if (IsCancellationCode(errorCode))
                {
                    _logger.LogWarning(exception, "Publishing {NotificationType} was cancelled.", notificationType.Name);
                }
                else if (exception is not null)
                {
                    _logger.LogError(exception, "Error while publishing notification {NotificationType} with {HandlerType}.", notificationType.Name, handler.GetType().Name);
                }
                else
                {
                    _logger.LogError("Error while publishing notification {NotificationType} with {HandlerType}: {Message}", notificationType.Name, handler.GetType().Name, error.Message);
                }

                return result;
            }
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return Right<Error, Unit>(Unit.Default);
    }

    private void LogSendOutcome<TResponse>(Type requestType, Type handlerType, Either<Error, TResponse> outcome)
    {
        var resultInfo = ExtractOutcome(outcome);
        LogSendOutcomeCore(requestType, handlerType, resultInfo.IsSuccess, resultInfo.Error);
    }

    private void LogSendOutcomeCore(Type requestType, Type handlerType, bool isSuccess, Error? error)
    {
        if (isSuccess)
        {
            _logger.LogDebug("Request {RequestType} completed by {HandlerType}.", requestType.Name, handlerType.Name);
            return;
        }

        var effectiveError = error ?? MediatorErrors.Unknown;

        var errorCode = effectiveError.GetMediatorCode();
        var exception = effectiveError.Exception.Match(
            Some: ex => (Exception?)ex,
            None: () => null);

        if (IsCancellationCode(errorCode))
        {
            _logger.LogWarning(exception, "The {RequestType} request was cancelled ({Reason}).", requestType.Name, errorCode);
            return;
        }

        _logger.LogError(exception, "The {RequestType} request failed ({Reason}): {Message}", requestType.Name, errorCode, effectiveError.Message);
    }

    private static (bool IsSuccess, Error? Error) ExtractOutcome<TResponse>(Either<Error, TResponse> outcome)
        => outcome.Match(
            Left: err => (IsSuccess: false, Error: (Error?)err),
            Right: _ => (IsSuccess: true, Error: (Error?)null));

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
            var outcome = await mediator.SendCore(typedRequest, typedHandler, provider, cancellationToken).ConfigureAwait(false);
            return outcome;
        }
    }

    private Task<Either<Error, TResponse>> SendCore<TRequest, TResponse>(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        RequestHandlerDelegate<TResponse> current = () => ExecuteHandlerAsync(handler, request, cancellationToken);

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToArray();
        if (behaviors?.Any() == true)
        {
            for (var index = behaviors.Length - 1; index >= 0; index--)
            {
                var behavior = behaviors[index];
                var next = current;
                current = () => ExecuteBehaviorAsync(behavior, request, cancellationToken, next);
            }
        }

        return ExecuteAsync();

        async Task<Either<Error, TResponse>> ExecuteAsync()
        {
            var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>() ?? System.Array.Empty<IRequestPreProcessor<TRequest>>();
            foreach (var preProcessor in preProcessors)
            {
                var failure = await ExecutePreProcessorAsync<TRequest, TResponse>(preProcessor, request, cancellationToken).ConfigureAwait(false);
                if (failure.IsSome)
                {
                    var error = failure.Match(err => err, () => MediatorErrors.Unknown);
                    return Left<Error, TResponse>(error);
                }
            }

            var response = await current().ConfigureAwait(false);

            var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>() ?? System.Array.Empty<IRequestPostProcessor<TRequest, TResponse>>();
            foreach (var postProcessor in postProcessors)
            {
                var failure = await ExecutePostProcessorAsync<TRequest, TResponse>(postProcessor, request, response, cancellationToken).ConfigureAwait(false);
                var hasFailure = false;
                Error capturedError = default;

                failure.IfSome(error =>
                {
                    hasFailure = true;
                    capturedError = error;
                });

                if (hasFailure)
                {
                    return Left<Error, TResponse>(capturedError);
                }
            }

            return response;
        }
    }

    private static async Task<Either<Error, TResponse>> ExecuteHandlerAsync<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            var task = handler.Handle(request, cancellationToken);
            if (task is null)
            {
                var message = $"Handler {handler.GetType().Name} returned a null task while processing {typeof(TRequest).Name}.";
                var exception = new InvalidOperationException(message);
                var error = MediatorErrors.FromException("mediator.handler.exception", exception, message);
                return Left<Error, TResponse>(error);
            }

            var result = await task.ConfigureAwait(false);
            return Right<Error, TResponse>(result);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Handler {handler.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.handler.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.handler.exception", ex, $"Error running {handler.GetType().Name} for {typeof(TRequest).Name}.");
            return Left<Error, TResponse>(error);
        }
    }

    private static async Task<Either<Error, TResponse>> ExecuteBehaviorAsync<TRequest, TResponse>(
        IPipelineBehavior<TRequest, TResponse> behavior,
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            return await behavior.Handle(request, cancellationToken, next).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Behavior {behavior.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error running {behavior.GetType().Name} for {typeof(TRequest).Name}.");
            return Left<Error, TResponse>(error);
        }
    }

    private static async Task<Option<Error>> ExecutePreProcessorAsync<TRequest, TResponse>(
        IRequestPreProcessor<TRequest> preProcessor,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            await preProcessor.Process(request, cancellationToken).ConfigureAwait(false);
            return Option<Error>.None;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Pre-processor {preProcessor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            return Some(MediatorErrors.Create("mediator.preprocessor.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.preprocessor.exception", ex, $"Error running {preProcessor.GetType().Name} for {typeof(TRequest).Name}.");
            return Some(error);
        }
    }

    private static async Task<Option<Error>> ExecutePostProcessorAsync<TRequest, TResponse>(
        IRequestPostProcessor<TRequest, TResponse> postProcessor,
        TRequest request,
        Either<Error, TResponse> response,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            await postProcessor.Process(request, response, cancellationToken).ConfigureAwait(false);
            return Option<Error>.None;
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var message = $"Post-processor {postProcessor.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
                return Some(MediatorErrors.Create("mediator.postprocessor.cancelled", message, ex));
            }

            var error = MediatorErrors.FromException("mediator.postprocessor.exception", ex, $"Error running {postProcessor.GetType().Name} for {typeof(TRequest).Name}.");
            return Some(error);
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.postprocessor.exception", ex, $"Error running {postProcessor.GetType().Name} for {typeof(TRequest).Name}.");
            return Some(error);
        }
    }

    private static async Task<Either<Error, Unit>> InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
    {
        var handlerType = handler.GetType();
        var method = GetHandleMethod(handlerType);
        if (method is null)
        {
            var message = $"Handler {handlerType.Name} does not expose a compatible Handle method.";
            return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.missing_handle", message));
        }

        try
        {
            var result = method.Invoke(handler, new object[] { notification!, cancellationToken });
            switch (result)
            {
                case Task task:
                    await task.ConfigureAwait(false);
                    return Right<Error, Unit>(Unit.Default);
                case null:
                    return Right<Error, Unit>(Unit.Default);
                default:
                    var message = $"Handler {handlerType.Name} returned an unexpected type while processing {notification?.GetType().Name ?? typeof(TNotification).Name}.";
                    return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.invalid_return", message));
            }
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            if (ex.InnerException is OperationCanceledException cancelled && cancellationToken.IsCancellationRequested)
            {
                var message = $"Publishing {notification?.GetType().Name ?? typeof(TNotification).Name} was cancelled by {handlerType.Name}.";
                return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.cancelled", message, cancelled));
            }

            var error = MediatorErrors.FromException("mediator.notification.exception", ex.InnerException, $"Error processing {notification?.GetType().Name ?? typeof(TNotification).Name} with {handlerType.Name}.");
            return Left<Error, Unit>(error);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Publishing {notification?.GetType().Name ?? typeof(TNotification).Name} was cancelled by {handlerType.Name}.";
            return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.notification.invoke_exception", ex, $"Error invoking {handlerType.Name}.Handle.");
            return Left<Error, Unit>(error);
        }
    }

    internal static bool IsCancellationCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return false;
        }

        return errorCode.IndexOf("cancelled", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static MethodInfo? GetHandleMethod(Type handlerType)
    {
        var method = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public);
        return method;
    }
}
