using System.Diagnostics.CodeAnalysis;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Sagas;

/// <summary>
/// Default implementation of <see cref="ISagaNotFoundDispatcher"/>.
/// </summary>
internal sealed partial class SagaNotFoundDispatcher : ISagaNotFoundDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaNotFoundDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaNotFoundDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    /// <param name="logger">The logger.</param>
    public SagaNotFoundDispatcher(
        IServiceProvider serviceProvider,
        ILogger<SagaNotFoundDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DispatchAsync<TMessage>(
        TMessage message,
        SagaNotFoundContext context,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);

        var handler = _serviceProvider.GetService<IHandleSagaNotFound<TMessage>>();

        if (handler == null)
        {
            // No handler registered - this is acceptable, return success
            Log.NoSagaNotFoundHandler(_logger, typeof(TMessage).Name);
            return unit;
        }

        try
        {
            Log.InvokingSagaNotFoundHandler(_logger, typeof(TMessage).Name, context.SagaId);

            await handler.HandleAsync(message, context, cancellationToken).ConfigureAwait(false);

            Log.SagaNotFoundHandlerCompleted(_logger, typeof(TMessage).Name, context.SagaId, context.Action);

            return unit;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.SagaNotFoundHandlerCancelled(_logger, typeof(TMessage).Name, context.SagaId);
            return EncinaErrors.Create(
                SagaErrorCodes.HandlerCancelled,
                $"Saga not found handler for {typeof(TMessage).Name} was cancelled");
        }
        catch (Exception ex)
        {
            Log.SagaNotFoundHandlerFailed(_logger, typeof(TMessage).Name, context.SagaId, ex);
            return EncinaErrors.FromException(
                SagaErrorCodes.HandlerFailed,
                ex,
                $"Saga not found handler for {typeof(TMessage).Name} failed");
        }
    }

    [ExcludeFromCodeCoverage]
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 220,
            Level = LogLevel.Debug,
            Message = "No saga not found handler registered for message type {MessageType}")]
        public static partial void NoSagaNotFoundHandler(ILogger logger, string messageType);

        [LoggerMessage(
            EventId = 221,
            Level = LogLevel.Debug,
            Message = "Invoking saga not found handler for {MessageType}, SagaId: {SagaId}")]
        public static partial void InvokingSagaNotFoundHandler(ILogger logger, string messageType, Guid sagaId);

        [LoggerMessage(
            EventId = 222,
            Level = LogLevel.Debug,
            Message = "Saga not found handler completed for {MessageType}, SagaId: {SagaId}, Action: {Action}")]
        public static partial void SagaNotFoundHandlerCompleted(
            ILogger logger, string messageType, Guid sagaId, SagaNotFoundAction action);

        [LoggerMessage(
            EventId = 223,
            Level = LogLevel.Warning,
            Message = "Saga not found handler cancelled for {MessageType}, SagaId: {SagaId}")]
        public static partial void SagaNotFoundHandlerCancelled(ILogger logger, string messageType, Guid sagaId);

        [LoggerMessage(
            EventId = 224,
            Level = LogLevel.Error,
            Message = "Saga not found handler failed for {MessageType}, SagaId: {SagaId}")]
        public static partial void SagaNotFoundHandlerFailed(
            ILogger logger, string messageType, Guid sagaId, Exception exception);
    }
}
