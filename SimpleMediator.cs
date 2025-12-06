using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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
    private static readonly MethodInfo SendCoreMethod = typeof(SimpleMediator)
        .GetMethod(nameof(SendCore), BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("No se encontró el método genérico SendCore");

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
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            var message = $"No se encontró un IRequestHandler registrado para {requestType.Name} -> {typeof(TResponse).Name}.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        try
        {
            _logger.LogDebug("Procesando {RequestType} con {HandlerType}.", requestType.Name, handler.GetType().Name);

            var method = SendCoreMethod.MakeGenericMethod(requestType, typeof(TResponse));
            var result = method.Invoke(this, new object[] { request, handler, serviceProvider, cancellationToken });
            if (result is not Task<TResponse> typedTask)
            {
                var errorMessage = $"El handler {handler.GetType().Name} devolvió un tipo inesperado al procesar {requestType.Name}.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var response = await typedTask.ConfigureAwait(false);
            _logger.LogDebug("Solicitud {RequestType} completada.", requestType.Name);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Se canceló la solicitud {RequestType}.", requestType.Name);
            throw;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            _logger.LogError(ex.InnerException, "Error procesando {RequestType}.", requestType.Name);
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado procesando {RequestType}.", requestType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        using var scope = _scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var notificationType = notification.GetType();
        using var activity = MediatorDiagnostics.ActivitySource.StartActivity("SimpleMediator.Publish", ActivityKind.Internal);
        activity?.SetTag("mediator.notification_type", notificationType.FullName);

        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
        var handlers = serviceProvider.GetService(enumerableType) as IEnumerable<object>;

        if (handlers is null)
        {
            _logger.LogDebug("No se encontraron handlers para la notificación {NotificationType}.", notificationType.Name);
            return;
        }

        var handlerList = handlers.ToList();

        if (handlerList.Count == 0)
        {
            _logger.LogDebug("No se encontraron handlers para la notificación {NotificationType}.", notificationType.Name);
            return;
        }

        try
        {
            foreach (var handler in handlerList)
            {
                _logger.LogDebug("Enviando notificación {NotificationType} a {HandlerType}.", notificationType.Name, handler.GetType().Name);
                await InvokeNotificationHandler(handler, notification, cancellationToken).ConfigureAwait(false);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Operación cancelada.");
            _logger.LogWarning("Se canceló la publicación de {NotificationType}.", notificationType.Name);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error al publicar la notificación {NotificationType}.", notificationType.Name);
            throw;
        }
    }

    private Task<TResponse> SendCore<TRequest, TResponse>(TRequest request, object handler, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
        RequestHandlerDelegate<TResponse> current = () => typedHandler.Handle(request, cancellationToken);

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()?.ToArray();
        if (behaviors is { Length: > 0 })
        {
            for (var index = behaviors.Length - 1; index >= 0; index--)
            {
                var behavior = behaviors[index];
                var next = current;
                current = () => behavior.Handle(request, cancellationToken, next);
            }
        }

        return ExecuteAsync();

        async Task<TResponse> ExecuteAsync()
        {
            var preProcessors = serviceProvider.GetServices<IRequestPreProcessor<TRequest>>() ?? Array.Empty<IRequestPreProcessor<TRequest>>();
            foreach (var preProcessor in preProcessors)
            {
                await preProcessor.Process(request, cancellationToken).ConfigureAwait(false);
            }

            var response = await current().ConfigureAwait(false);

            var postProcessors = serviceProvider.GetServices<IRequestPostProcessor<TRequest, TResponse>>() ?? Array.Empty<IRequestPostProcessor<TRequest, TResponse>>();
            foreach (var postProcessor in postProcessors)
            {
                await postProcessor.Process(request, response, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
    }

    private static Task InvokeNotificationHandler<TNotification>(object handler, TNotification notification, CancellationToken cancellationToken)
    {
        var method = GetHandleMethod(handler.GetType());
        try
        {
            var result = method.Invoke(handler, new object[] { notification!, cancellationToken });
            return result switch
            {
                Task task => task,
                null => Task.CompletedTask,
                _ => throw new InvalidOperationException($"El handler {handler.GetType().Name} devolvió un tipo inesperado al procesar {notification?.GetType().Name ?? typeof(TNotification).Name}.")
            };
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    private static MethodInfo GetHandleMethod(Type handlerType)
    {
        var method = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public);
        return method ?? throw new InvalidOperationException($"El handler {handlerType.Name} no expone un método Handle esperado.");
    }
}
