using System.Diagnostics.CodeAnalysis;
using Encina.Diagnostics;
using Encina.Messaging.DeadLetter;
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
    private readonly DeadLetterSourceCapture? _deadLetterCapture;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaNotFoundDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="deadLetterCapture">
    /// Optional dead letter capture, registered with the dead letter queue: while
    /// <c>DeadLetterOptions.IntegrateWithSagas</c> is on, <see cref="SagaNotFoundContext.MoveToDeadLetterAsync"/>
    /// stores the message keyed by <see cref="SagaNotFoundContext.SourceMessageId"/>.
    /// </param>
    /// <param name="timeProvider">Optional time provider for the dead letter's failure instant.</param>
    public SagaNotFoundDispatcher(
        IServiceProvider serviceProvider,
        ILogger<SagaNotFoundDispatcher> logger,
        DeadLetterSourceCapture? deadLetterCapture = null,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deadLetterCapture = deadLetterCapture;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // With the flag on, MoveToDeadLetterAsync captures the message itself (serialized through IMessageSerializer)
    // under source pattern Saga, keyed by the caller's message id; the record keeps the code saga.not_found,
    // never the handler's reason text. With the flag off the context stays unwired (dlq.not_configured).
    private void WireDeadLetter<TMessage>(TMessage message, SagaNotFoundContext context)
        where TMessage : class
    {
        if (_deadLetterCapture is null || !_deadLetterCapture.IsEnabledFor(DeadLetterSourcePatterns.Saga))
        {
            // A context reused from an earlier dispatch must not keep that dispatch's capture.
            context.UseDeadLetter(null);
            return;
        }

        var capture = _deadLetterCapture;
        context.UseDeadLetter(cancellationToken => capture.CaptureAsync(
            message,
            new DeadLetterContext(
                EncinaErrors.Create(SagaErrorCodes.NotFound, "Saga not found"),
                Exception: null,
                DeadLetterSourcePatterns.Saga,
                TotalRetryAttempts: 0,
                _timeProvider.GetUtcNow().UtcDateTime,
                SourceMessageId: context.SourceMessageId),
            cancellationToken));
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

        WireDeadLetter(message, context);

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

            // A move the handler asked for and that failed is not success: the caller must not acknowledge a
            // message that is in no dead letter queue.
            return CompletedResult(context);
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
            Log.SagaNotFoundHandlerFailed(_logger, typeof(TMessage).Name, context.SagaId, ex.ForLogging());
            return EncinaErrors.FromException(
                SagaErrorCodes.HandlerFailed,
                ex,
                $"Saga not found handler for {typeof(TMessage).Name} failed");
        }
    }

    private static Either<EncinaError, Unit> CompletedResult(SagaNotFoundContext context)
        => context.FailedMove is { } failed ? failed : unit;

    [ExcludeFromCodeCoverage]
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2899,
            Level = LogLevel.Debug,
            Message = "No saga not found handler registered for message type {MessageType}")]
        public static partial void NoSagaNotFoundHandler(ILogger logger, string messageType);

        [LoggerMessage(
            EventId = 2900,
            Level = LogLevel.Debug,
            Message = "Invoking saga not found handler for {MessageType}, SagaId: {SagaId}")]
        public static partial void InvokingSagaNotFoundHandler(ILogger logger, string messageType, Guid sagaId);

        [LoggerMessage(
            EventId = 2901,
            Level = LogLevel.Debug,
            Message = "Saga not found handler completed for {MessageType}, SagaId: {SagaId}, Action: {Action}")]
        public static partial void SagaNotFoundHandlerCompleted(
            ILogger logger, string messageType, Guid sagaId, SagaNotFoundAction action);

        [LoggerMessage(
            EventId = 2902,
            Level = LogLevel.Warning,
            Message = "Saga not found handler cancelled for {MessageType}, SagaId: {SagaId}")]
        public static partial void SagaNotFoundHandlerCancelled(ILogger logger, string messageType, Guid sagaId);

        [LoggerMessage(
            EventId = 2903,
            Level = LogLevel.Error,
            Message = "Saga not found handler failed for {MessageType}, SagaId: {SagaId}")]
        public static partial void SagaNotFoundHandlerFailed(
            ILogger logger, string messageType, Guid sagaId, Exception exception);
    }
}
