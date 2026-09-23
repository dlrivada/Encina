using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Post-processor that intercepts notifications and stores them in the outbox instead of publishing immediately.
/// This ensures reliable event delivery by persisting events in the same transaction as domain changes.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class OutboxPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOutboxStore _outboxStore;
    private readonly IOutboxMessageFactory _messageFactory;
    private readonly ILogger<OutboxPostProcessor<TRequest, TResponse>> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IMessageSerializer _messageSerializer;

    private static readonly MethodInfo SerializeMethodDefinition =
        typeof(IMessageSerializer).GetMethod(nameof(IMessageSerializer.Serialize))!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> SerializeMethodCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPostProcessor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="outboxStore">The outbox store for persisting notifications.</param>
    /// <param name="messageFactory">The factory for creating outbox messages.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageSerializer">
    /// The message serializer used to convert notifications to their persisted representation.
    /// Serializing through this abstraction (rather than calling <c>JsonSerializer</c> directly)
    /// ensures that decorators such as <c>EncryptingMessageSerializer</c> from
    /// <c>Encina.Messaging.Encryption</c> apply to outbox payloads too.
    /// </param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public OutboxPostProcessor(
        IOutboxStore outboxStore,
        IOutboxMessageFactory messageFactory,
        ILogger<OutboxPostProcessor<TRequest, TResponse>> logger,
        IMessageSerializer messageSerializer,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(outboxStore);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageSerializer);

        _outboxStore = outboxStore;
        _messageFactory = messageFactory;
        _logger = logger;
        _messageSerializer = messageSerializer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task Process(
        TRequest request,
        IRequestContext context,
        Either<EncinaError, TResponse> response,
        CancellationToken cancellationToken)
    {
        // Only process if request has notifications
        if (request is not IHasNotifications hasNotifications)
            return;

        var notifications = hasNotifications.GetNotifications().ToList();
        if (notifications.Count == 0)
            return;

        // Only store notifications from successful requests
        await response.Match(
            Right: async _ =>
            {
                Log.StoringNotificationsInOutbox(_logger, notifications.Count, typeof(TRequest).Name, context.CorrelationId);

                foreach (var notification in notifications)
                {
                    var notificationType = notification.GetType().AssemblyQualifiedName
                        ?? notification.GetType().FullName
                        ?? notification.GetType().Name;

                    // Serialize using the notification's runtime type (not the declared
                    // INotification interface) so that all of its properties are captured,
                    // matching the previous JsonSerializer.Serialize(obj, obj.GetType()) behavior,
                    // and so that EncryptingMessageSerializer can read the [EncryptedMessage]
                    // attribute off the concrete type. IMessageSerializer.Serialize<T> is generic,
                    // so the closed method for the runtime type is built once via reflection
                    // (a `dynamic` call here does NOT infer T from the runtime type reliably
                    // across all interface implementations, so reflection is used instead).
                    var serializeMethod = SerializeMethodCache.GetOrAdd(
                        notification.GetType(),
                        static t => SerializeMethodDefinition.MakeGenericMethod(t));
                    // DoNotWrapExceptions: a serializer failure (e.g. an encryption failure from
                    // EncryptingMessageSerializer) propagates as itself, not as a
                    // TargetInvocationException wrapper.
                    var content = (string)serializeMethod.Invoke(
                        _messageSerializer,
                        BindingFlags.DoNotWrapExceptions,
                        binder: null,
                        parameters: [notification],
                        culture: null)!;

                    var outboxMessage = _messageFactory.Create(
                        Guid.NewGuid(),
                        notificationType,
                        content,
                        _timeProvider.GetUtcNow().UtcDateTime);

                    await _outboxStore.AddAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
                }

                await _outboxStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                Log.StoredNotificationsInOutbox(_logger, notifications.Count, context.CorrelationId);
            },
            Left: error =>
            {
                // Only the error code is logged: EncinaError.Message may carry personal data
                // (e.g. a data-subject id from compliance modules).
                Log.SkippingOutboxStorageDueToError(
                    _logger, notifications.Count, error.GetCode().IfNone("encina.unknown"), context.CorrelationId);

                return Task.CompletedTask;
            });
    }
}

/// <summary>
/// Marker interface for requests that can emit notifications.
/// </summary>
public interface IHasNotifications
{
    /// <summary>
    /// Gets the notifications to be published.
    /// </summary>
    IEnumerable<INotification> GetNotifications();
}

/// <summary>
/// LoggerMessage definitions for OutboxPostProcessor.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 2842,
        Level = LogLevel.Debug,
        Message = "Storing {Count} notifications in outbox for request {RequestType} (correlation: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(ILogger logger, int count, string requestType, string correlationId);

    [LoggerMessage(
        EventId = 2843,
        Level = LogLevel.Debug,
        Message = "Stored {Count} notifications in outbox (correlation: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(ILogger logger, int count, string correlationId);

    [LoggerMessage(
        EventId = 2844,
        Level = LogLevel.Debug,
        Message = "Skipping outbox storage for {Count} notifications due to error code {ErrorCode} (correlation: {CorrelationId})")]
    public static partial void SkippingOutboxStorageDueToError(ILogger logger, int count, string errorCode, string correlationId);
}
