using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
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
        if (request is null)
        {
            const string message = "The request cannot be null.";
            Log.NullRequest(_logger);
            return new ValueTask<Either<MediatorError, TResponse>>(Left<MediatorError, TResponse>(MediatorErrors.Create("mediator.request.null", message)));
        }
        return new ValueTask<Either<MediatorError, TResponse>>(RequestOperation<TResponse>.ExecuteAsync(this, request, cancellationToken));
    }

    /// <inheritdoc />
    public ValueTask<Either<MediatorError, Unit>> Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null)
        {
            const string message = "The notification cannot be null.";
            Log.NotificationNull(_logger);
            return new ValueTask<Either<MediatorError, Unit>>(Left<MediatorError, Unit>(MediatorErrors.Create("mediator.notification.null", message)));
        }
        return new ValueTask<Either<MediatorError, Unit>>(NotificationOperation<TNotification>.ExecuteAsync(this, notification, cancellationToken));
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

    private static class RequestOperation<TResponse>
    {
        public static async Task<Either<MediatorError, TResponse>> ExecuteAsync(SimpleMediator mediator, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            using var scope = mediator._scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var requestType = request.GetType();
            var dispatcher = RequestHandlerCache.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => CreateRequestHandlerWrapper(key.Request, key.Response));

            var handler = dispatcher.ResolveHandler(serviceProvider);

            if (handler is null)
            {
                var message = $"No registered IRequestHandler was found for {requestType.Name} -> {typeof(TResponse).Name}.";
                Log.HandlerMissing(mediator._logger, requestType.Name, typeof(TResponse).Name);
                return Left<MediatorError, TResponse>(MediatorErrors.Create("mediator.handler.missing", message));
            }

            try
            {
                var handlerType = handler.GetType();
                Log.ProcessingRequest(mediator._logger, requestType.Name, handlerType.Name);
                var boxedOutcome = await dispatcher
                    .Handle(mediator, request, handler, serviceProvider, cancellationToken)
                    .ConfigureAwait(false);

                if (boxedOutcome is not Either<MediatorError, TResponse> outcome)
                {
                    var message = $"Handler {handlerType.Name} returned an unexpected type while processing {requestType.Name}.";
                    Log.HandlerReturnedUnexpectedType(mediator._logger, handlerType.Name, requestType.Name);
                    return Left<MediatorError, TResponse>(MediatorErrors.Create("mediator.handler.invalid_result", message));
                }

                mediator.LogSendOutcome(requestType, handler.GetType(), outcome);
                return outcome;
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                var message = $"The {requestType.Name} request was cancelled.";
                Log.RequestCancelledDuringSend(mediator._logger, requestType.Name);
                return Left<MediatorError, TResponse>(MediatorErrors.Create("mediator.request.cancelled", message, ex));
            }
            catch (Exception ex)
            {
                var error = MediatorErrors.FromException("mediator.pipeline.exception", ex, $"Unexpected error while processing {requestType.Name}.");
                Log.RequestProcessingError(mediator._logger, requestType.Name, ex);
                return Left<MediatorError, TResponse>(error);
            }
        }
    }

    private static class NotificationOperation<TNotification>
        where TNotification : INotification
    {
        public static async Task<Either<MediatorError, Unit>> ExecuteAsync(SimpleMediator mediator, TNotification notification, CancellationToken cancellationToken)
        {
            using var scope = mediator._scopeFactory.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var notificationType = notification.GetType();
            using var activity = MediatorDiagnostics.ActivitySource.HasListeners()
                ? MediatorDiagnostics.ActivitySource.StartActivity("SimpleMediator.Publish", ActivityKind.Internal)
                : null;
            activity?.SetTag("mediator.notification_type", notificationType.FullName);

            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlers = serviceProvider.GetServices(handlerType)?.Cast<object>().ToList() ?? new List<object>();

            if (handlers.Count == 0)
            {
                Log.NoNotificationHandlers(mediator._logger, notificationType.Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Right<MediatorError, Unit>(Unit.Default);
            }

            foreach (var handler in handlers)
            {
                Log.SendingNotification(mediator._logger, notificationType.Name, handler.GetType().Name);
                var result = await InvokeNotificationHandler(handler, notification, cancellationToken).ConfigureAwait(false);
                if (TryHandleNotificationFailure(mediator, notificationType.Name, activity, result, handler))
                {
                    return result;
                }
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Right<MediatorError, Unit>(Unit.Default);
        }

        private static bool TryHandleNotificationFailure(SimpleMediator mediator, string notificationName, Activity? activity, Either<MediatorError, Unit> result, object handlerInstance)
        {
            if (result.IsRight)
            {
                return false;
            }

            var error = result.Match(
                Left: err => err,
                Right: _ => MediatorErrors.Unknown);

            activity?.SetStatus(ActivityStatusCode.Error, error.Message);

            var errorCode = error.GetMediatorCode();
            var exception = error.Exception.Match(
                Some: ex => (Exception?)ex,
                None: () => null);
            var handlerTypeName = handlerInstance.GetType().Name;

            if (IsCancellationCode(errorCode))
            {
                Log.NotificationCancelled(mediator._logger, notificationName, handlerTypeName, exception);
            }
            else if (exception is not null)
            {
                Log.NotificationHandlerException(mediator._logger, notificationName, handlerTypeName, exception);
            }
            else
            {
                Log.NotificationHandlerFailure(mediator._logger, notificationName, handlerTypeName, error.Message);
            }

            return true;
        }
    }

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
            var outcome = await SendCore(typedRequest, typedHandler, provider, cancellationToken).ConfigureAwait(false);
            return outcome;
        }
    }

    private static ValueTask<Either<MediatorError, TResponse>> SendCore<TRequest, TResponse>(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        RequestHandlerCallback<TResponse> current = () => ExecuteHandlerAsync(handler, request, cancellationToken);

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToArray();
        if (behaviors is { Length: > 0 })
        {
            for (var index = behaviors.Length - 1; index >= 0; index--)
            {
                var behavior = behaviors[index];
                var nextStep = current;
                current = () => ExecuteBehaviorAsync(behavior, request, nextStep, cancellationToken);
            }
        }

        return ExecuteAsync();

        async ValueTask<Either<MediatorError, TResponse>> ExecuteAsync()
        {
            var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>() ?? System.Array.Empty<IRequestPreProcessor<TRequest>>();
            foreach (var preProcessor in preProcessors)
            {
                var failure = await ExecutePreProcessorAsync<TRequest, TResponse>(preProcessor, request, cancellationToken).ConfigureAwait(false);
                if (failure.IsSome)
                {
                    var error = failure.Match(err => err, () => MediatorErrors.Unknown);
                    return Left<MediatorError, TResponse>(error);
                }
            }

            var response = await current().ConfigureAwait(false);

            var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>() ?? System.Array.Empty<IRequestPostProcessor<TRequest, TResponse>>();
            foreach (var postProcessor in postProcessors)
            {
                var failure = await ExecutePostProcessorAsync<TRequest, TResponse>(postProcessor, request, response, cancellationToken).ConfigureAwait(false);
                var hasFailure = false;
                MediatorError capturedError = default;

                failure.IfSome(error =>
                {
                    hasFailure = true;
                    capturedError = error;
                });

                if (hasFailure)
                {
                    return Left<MediatorError, TResponse>(capturedError);
                }
            }

            return response;
        }
    }

    private static async ValueTask<Either<MediatorError, TResponse>> ExecuteHandlerAsync<TRequest, TResponse>(
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
                return Left<MediatorError, TResponse>(error);
            }

            var result = await task.ConfigureAwait(false);
            return Right<MediatorError, TResponse>(result);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Handler {handler.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            return Left<MediatorError, TResponse>(MediatorErrors.Create("mediator.handler.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.handler.exception", ex, $"Error running {handler.GetType().Name} for {typeof(TRequest).Name}.");
            return Left<MediatorError, TResponse>(error);
        }
    }

    private static async ValueTask<Either<MediatorError, TResponse>> ExecuteBehaviorAsync<TRequest, TResponse>(
        IPipelineBehavior<TRequest, TResponse> behavior,
        TRequest request,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            return await behavior.Handle(request, nextStep, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Behavior {behavior.GetType().Name} cancelled the {typeof(TRequest).Name} request.";
            return Left<MediatorError, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error running {behavior.GetType().Name} for {typeof(TRequest).Name}.");
            return Left<MediatorError, TResponse>(error);
        }
    }

    private static async Task<Option<MediatorError>> ExecutePreProcessorAsync<TRequest, TResponse>(
        IRequestPreProcessor<TRequest> preProcessor,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            await preProcessor.Process(request, cancellationToken).ConfigureAwait(false);
            return Option<MediatorError>.None;
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

    private static async Task<Option<MediatorError>> ExecutePostProcessorAsync<TRequest, TResponse>(
        IRequestPostProcessor<TRequest, TResponse> postProcessor,
        TRequest request,
        Either<MediatorError, TResponse> response,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
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

    private static async Task<Either<MediatorError, Unit>> InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var handlerType = handler.GetType();
        var runtimeNotificationType = notification?.GetType();
        var desiredNotificationType = runtimeNotificationType
            ?? ResolveHandledNotificationType(handlerType)
            ?? typeof(TNotification);

        var notificationName = notification?.GetType().Name ?? typeof(TNotification).Name;

        if (!NotificationHandlerInvokerCache.TryGetValue((handlerType, desiredNotificationType), out var executor))
        {
            var method = ResolveHandleMethod(handlerType, desiredNotificationType, runtimeNotificationType);
            if (method is null)
            {
                var message = $"Handler {handlerType.Name} does not expose a compatible Handle method.";
                return Left<MediatorError, Unit>(MediatorErrors.Create("mediator.notification.missing_handle", message));
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 2 || parameters[1].ParameterType != typeof(CancellationToken))
            {
                var exception = new TargetParameterCountException("The Handle method must accept the notification and a CancellationToken.");
                var error = MediatorErrors.FromException("mediator.notification.invoke_exception", exception, $"Error invoking {handlerType.Name}.Handle.");
                return Left<MediatorError, Unit>(error);
            }

            var handledType = parameters[0].ParameterType;
            if (!typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                var message = $"Handler {handlerType.Name} returned an unexpected type while processing {notificationName}.";
                return Left<MediatorError, Unit>(MediatorErrors.Create("mediator.notification.invalid_return", message));
            }

            executor = NotificationHandlerInvokerCache.GetOrAdd((handlerType, handledType), _ => CreateNotificationInvoker(method, handlerType, handledType));
        }

        Task? execution;
        try
        {
            execution = executor(handler, notification, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var message = $"Publishing {notificationName} was cancelled by {handlerType.Name}.";
                return Left<MediatorError, Unit>(MediatorErrors.Create("mediator.notification.cancelled", message, ex));
            }

            var error = MediatorErrors.FromException("mediator.notification.exception", ex, $"Error processing {notificationName} with {handlerType.Name}.");
            return Left<MediatorError, Unit>(error);
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.notification.exception", ex, $"Error processing {notificationName} with {handlerType.Name}.");
            return Left<MediatorError, Unit>(error);
        }

        if (execution is null)
        {
            return Right<MediatorError, Unit>(Unit.Default);
        }

        try
        {
            await execution.ConfigureAwait(false);
            return Right<MediatorError, Unit>(Unit.Default);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var message = $"Publishing {notificationName} was cancelled by {handlerType.Name}.";
                return Left<MediatorError, Unit>(MediatorErrors.Create("mediator.notification.cancelled", message, ex));
            }

            var error = MediatorErrors.FromException("mediator.notification.exception", ex, $"Error processing {notificationName} with {handlerType.Name}.");
            return Left<MediatorError, Unit>(error);
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.notification.invoke_exception", ex, $"Error invoking {handlerType.Name}.Handle.");
            return Left<MediatorError, Unit>(error);
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

    private static Func<object, object?, CancellationToken, Task> CreateNotificationInvoker(MethodInfo method, Type handlerType, Type notificationType)
    {
        var handlerParameter = Expression.Parameter(typeof(object), "handler");
        var notificationParameter = Expression.Parameter(typeof(object), "notification");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var castHandler = Expression.Convert(handlerParameter, handlerType);
        Expression castNotification;
        if (notificationType.IsValueType)
        {
            var tempVariable = Expression.Variable(notificationType, "typedNotification");
            var assignExpression = Expression.Block(
                new[] { tempVariable },
                Expression.IfThenElse(
                    Expression.Equal(notificationParameter, Expression.Constant(null, typeof(object))),
                    Expression.Assign(tempVariable, Expression.Default(notificationType)),
                    Expression.Assign(tempVariable, Expression.Convert(notificationParameter, notificationType))),
                tempVariable);
            castNotification = assignExpression;
        }
        else
        {
            castNotification = Expression.Convert(notificationParameter, notificationType);
        }

        Expression call;
        if (method.IsStatic)
        {
            call = Expression.Call(method, castNotification, cancellationTokenParameter);
        }
        else
        {
            call = Expression.Call(castHandler, method, castNotification, cancellationTokenParameter);
        }

        Expression body = method.ReturnType == typeof(Task)
            ? call
            : Expression.Convert(call, typeof(Task));

        var lambda = Expression.Lambda<Func<object, object?, CancellationToken, Task>>(
            body,
            handlerParameter,
            notificationParameter,
            cancellationTokenParameter);

        return lambda.Compile();
    }

    private static Type? ResolveHandledNotificationType(Type handlerType)
    {
        var interfaceType = handlerType
            .GetInterfaces()
            .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
            .Select(@interface => @interface.GetGenericArguments()[0])
            .FirstOrDefault();

        return interfaceType;
    }

    private static MethodInfo? ResolveHandleMethod(Type handlerType, Type desiredNotificationType, Type? runtimeNotificationType)
    {
        var candidateMethods = handlerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, "Handle", StringComparison.Ordinal))
            .ToArray();

        if (candidateMethods.Length == 0)
        {
            return null;
        }

        if (runtimeNotificationType is not null)
        {
            var runtimeMatch = candidateMethods.FirstOrDefault(method => HasCompatibleFirstParameter(method, runtimeNotificationType));
            if (runtimeMatch is not null)
            {
                return runtimeMatch;
            }
        }

        var desiredMatch = candidateMethods.FirstOrDefault(method => HasCompatibleFirstParameter(method, desiredNotificationType));
        if (desiredMatch is not null)
        {
            return desiredMatch;
        }

        return candidateMethods.First();
    }

    private static bool HasCompatibleFirstParameter(MethodInfo method, Type candidateType)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return false;
        }

        var parameterType = parameters[0].ParameterType;
        return parameterType == candidateType
            || parameterType.IsAssignableFrom(candidateType)
            || candidateType.IsAssignableFrom(parameterType);
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
