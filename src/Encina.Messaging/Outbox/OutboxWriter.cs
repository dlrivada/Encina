using Encina.Messaging.Serialization;
using LanguageExt;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Serializes notifications and adds them to the outbox store, shared by the transactional outbox behavior and
/// the interim post-processor.
/// </summary>
/// <remarks>
/// Failures of the store are returned, never swallowed: the first <see langword="Left"/> of
/// <see cref="IOutboxStore.AddAsync"/> or <see cref="IOutboxStore.SaveChangesAsync"/> stops the write.
/// Serializer exceptions propagate unwrapped, as they always did.
/// </remarks>
internal sealed class OutboxWriter
{
    private readonly IOutboxStore _outboxStore;
    private readonly IOutboxMessageFactory _messageFactory;
    private readonly IMessageSerializer _messageSerializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxWriter"/> class.
    /// </summary>
    /// <param name="outboxStore">The outbox store.</param>
    /// <param name="messageFactory">The factory for outbox messages.</param>
    /// <param name="messageSerializer">
    /// The serializer for the persisted payload; going through the abstraction lets decorators such as
    /// <c>EncryptingMessageSerializer</c> apply to outbox payloads.
    /// </param>
    /// <param name="timeProvider">The time provider for the creation timestamp.</param>
    internal OutboxWriter(
        IOutboxStore outboxStore,
        IOutboxMessageFactory messageFactory,
        IMessageSerializer messageSerializer,
        TimeProvider timeProvider)
    {
        _outboxStore = outboxStore;
        _messageFactory = messageFactory;
        _messageSerializer = messageSerializer;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Adds one outbox message per notification and saves them.
    /// </summary>
    /// <param name="notifications">The notifications to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of notifications stored, or the first store error.</returns>
    internal async ValueTask<Either<EncinaError, int>> WriteAsync(
        IReadOnlyList<INotification> notifications,
        CancellationToken cancellationToken)
    {
        foreach (var notification in notifications)
        {
            var notificationType = notification.GetType().AssemblyQualifiedName
                ?? notification.GetType().FullName
                ?? notification.GetType().Name;

            // Serialize using the notification's runtime type (not the declared INotification interface) so
            // that all of its properties are captured and EncryptingMessageSerializer can read the
            // [EncryptedMessage] attribute off the concrete type.
            var content = _messageSerializer.SerializeAsRuntimeType(notification);

            var outboxMessage = _messageFactory.Create(
                Guid.NewGuid(),
                notificationType,
                content,
                _timeProvider.GetUtcNow().UtcDateTime);

            var added = await _outboxStore.AddAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
            if (added.IsLeft)
            {
                return added.Map(_ => 0);
            }
        }

        var saved = await _outboxStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return saved.Map(_ => notifications.Count);
    }
}
