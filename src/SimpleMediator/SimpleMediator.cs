using System;
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
/// Implementación por defecto de <see cref="IMediator"/> basada en Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// Crea un ámbito por operación, resuelve handlers, behaviors, pre/post procesadores y publica
/// notificaciones. Incluye instrumentación mediante <see cref="MediatorDiagnostics"/>.
/// </remarks>
public sealed class SimpleMediator : IMediator
{
    private static readonly MethodInfo? SendCoreMethod = typeof(SimpleMediator)
        .GetMethod(nameof(SendCore), BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimpleMediator> _logger;

    /// <summary>
    /// Crea una instancia del mediador utilizando la factoría de scopes proporcionada.
    /// </summary>
    /// <param name="scopeFactory">Factoría utilizada para crear ámbitos por operación.</param>
    /// <param name="logger">Logger opcional para traza y diagnóstico.</param>
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
            const string message = "La solicitud no puede ser nula.";
            _logger.LogError(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.request.null", message));
        }

        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            var message = $"No se encontró un IRequestHandler registrado para {requestType.Name} -> {typeof(TResponse).Name}.";
            _logger.LogError(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.handler.missing", message));
        }

        if (SendCoreMethod is null)
        {
            const string message = "No se localizó la operación interna SendCore.";
            _logger.LogCritical(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.internal_missing", message));
        }

        MethodInfo typedMethod;
        try
        {
            typedMethod = SendCoreMethod.MakeGenericMethod(requestType, typeof(TResponse));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.reflection_failure", ex, $"No se pudo preparar el pipeline para {requestType.Name}.");
            _logger.LogError(ex, error.Message);
            return Left<Error, TResponse>(error);
        }

        try
        {
            _logger.LogDebug("Procesando {RequestType} con {HandlerType}.", requestType.Name, handler.GetType().Name);

            var result = typedMethod.Invoke(this, new object[] { request, handler, serviceProvider, cancellationToken });
            if (result is Task<Either<Error, TResponse>> task)
            {
                var outcome = await task.ConfigureAwait(false);
                LogSendOutcome(requestType, handler.GetType(), outcome);
                return outcome;
            }

            var message = $"El handler {handler.GetType().Name} devolvió un tipo inesperado al procesar {requestType.Name}.";
            _logger.LogError(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.handler.invalid_result", message));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            var error = MediatorErrors.FromException("mediator.pipeline.exception", ex.InnerException, $"Error procesando {requestType.Name}.");
            _logger.LogError(ex.InnerException, error.Message);
            return Left<Error, TResponse>(error);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"Se canceló la solicitud {requestType.Name}.";
            _logger.LogWarning(message);
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.request.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.pipeline.exception", ex, $"Error inesperado procesando {requestType.Name}.");
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
            const string message = "La notificación no puede ser nula.";
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
            _logger.LogDebug("No se encontraron handlers para la notificación {NotificationType}.", notificationType.Name);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Right<Error, Unit>(Unit.Default);
        }

        foreach (var handler in handlers)
        {
            _logger.LogDebug("Enviando notificación {NotificationType} a {HandlerType}.", notificationType.Name, handler.GetType().Name);
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
                    _logger.LogWarning(exception, "Se canceló la publicación de {NotificationType}.", notificationType.Name);
                }
                else if (exception is not null)
                {
                    _logger.LogError(exception, "Error al publicar la notificación {NotificationType} con {HandlerType}.", notificationType.Name, handler.GetType().Name);
                }
                else
                {
                    _logger.LogError("Error al publicar la notificación {NotificationType} con {HandlerType}: {Message}", notificationType.Name, handler.GetType().Name, error.Message);
                }

                return result;
            }
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return Right<Error, Unit>(Unit.Default);
    }

    private void LogSendOutcome<TResponse>(Type requestType, Type handlerType, Either<Error, TResponse> outcome)
    {
        if (outcome.IsRight)
        {
            _logger.LogDebug("Solicitud {RequestType} completada por {HandlerType}.", requestType.Name, handlerType.Name);
            return;
        }

        var error = outcome.Match(
            Left: err => err,
            Right: _ => MediatorErrors.Unknown);

        var errorCode = error.GetMediatorCode();
        var exception = error.Exception.Match(
            Some: ex => (Exception?)ex,
            None: () => null);

        if (IsCancellationCode(errorCode))
        {
            _logger.LogWarning(exception, "Se canceló la solicitud {RequestType} ({Reason}).", requestType.Name, errorCode);
            return;
        }

        _logger.LogError(exception, "La solicitud {RequestType} falló ({Reason}): {Message}", requestType.Name, errorCode, error.Message);
    }

    private Task<Either<Error, TResponse>> SendCore<TRequest, TResponse>(TRequest request, object handler, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
        RequestHandlerDelegate<TResponse> current = () => ExecuteHandlerAsync(typedHandler, request, cancellationToken);

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
                var message = $"El handler {handler.GetType().Name} devolvió una tarea nula al procesar {typeof(TRequest).Name}.";
                var exception = new InvalidOperationException(message);
                var error = MediatorErrors.FromException("mediator.handler.exception", exception, message);
                return Left<Error, TResponse>(error);
            }

            var result = await task.ConfigureAwait(false);
            return Right<Error, TResponse>(result);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"El handler {handler.GetType().Name} canceló la solicitud {typeof(TRequest).Name}.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.handler.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.handler.exception", ex, $"Error ejecutando {handler.GetType().Name} para {typeof(TRequest).Name}.");
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
            var message = $"El behavior {behavior.GetType().Name} canceló la solicitud {typeof(TRequest).Name}.";
            return Left<Error, TResponse>(MediatorErrors.Create("mediator.behavior.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.behavior.exception", ex, $"Error ejecutando {behavior.GetType().Name} para {typeof(TRequest).Name}.");
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
            var message = $"El pre-procesador {preProcessor.GetType().Name} canceló la solicitud {typeof(TRequest).Name}.";
            return Some(MediatorErrors.Create("mediator.preprocessor.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.preprocessor.exception", ex, $"Error ejecutando {preProcessor.GetType().Name} para {typeof(TRequest).Name}.");
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
                var message = $"El post-procesador {postProcessor.GetType().Name} canceló la solicitud {typeof(TRequest).Name}.";
                return Some(MediatorErrors.Create("mediator.postprocessor.cancelled", message, ex));
            }

            var error = MediatorErrors.FromException("mediator.postprocessor.exception", ex, $"Error ejecutando {postProcessor.GetType().Name} para {typeof(TRequest).Name}.");
            return Some(error);
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.postprocessor.exception", ex, $"Error ejecutando {postProcessor.GetType().Name} para {typeof(TRequest).Name}.");
            return Some(error);
        }
    }

    private static async Task<Either<Error, Unit>> InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
    {
        var handlerType = handler.GetType();
        var method = GetHandleMethod(handlerType);
        if (method is null)
        {
            var message = $"El handler {handlerType.Name} no expone un método Handle esperado.";
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
                    var message = $"El handler {handlerType.Name} devolvió un tipo inesperado al procesar {notification?.GetType().Name ?? typeof(TNotification).Name}.";
                    return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.invalid_return", message));
            }
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            if (ex.InnerException is OperationCanceledException cancelled && cancellationToken.IsCancellationRequested)
            {
                var message = $"La publicación de {notification?.GetType().Name ?? typeof(TNotification).Name} fue cancelada por {handlerType.Name}.";
                return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.cancelled", message, cancelled));
            }

            var error = MediatorErrors.FromException("mediator.notification.exception", ex.InnerException, $"Error procesando {notification?.GetType().Name ?? typeof(TNotification).Name} con {handlerType.Name}.");
            return Left<Error, Unit>(error);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            var message = $"La publicación de {notification?.GetType().Name ?? typeof(TNotification).Name} fue cancelada por {handlerType.Name}.";
            return Left<Error, Unit>(MediatorErrors.Create("mediator.notification.cancelled", message, ex));
        }
        catch (Exception ex)
        {
            var error = MediatorErrors.FromException("mediator.notification.invoke_exception", ex, $"Error invocando {handlerType.Name}.Handle.");
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
