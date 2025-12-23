using Encina.Messaging.Inbox;

namespace Encina.MongoDB.Inbox;

/// <summary>
/// Factory for creating <see cref="InboxMessage"/> instances for MongoDB provider.
/// </summary>
public sealed class InboxMessageFactory : IInboxMessageFactory
{
    /// <inheritdoc />
    public IInboxMessage Create(
        string messageId,
        string requestType,
        DateTime receivedAtUtc,
        DateTime expiresAtUtc,
        InboxMetadata? metadata)
    {
        // MongoDB InboxMessage doesn't have a Metadata property,
        // so metadata is not stored. Consider adding if needed.
        return new InboxMessage
        {
            MessageId = messageId,
            RequestType = requestType,
            ReceivedAtUtc = receivedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            RetryCount = 0
        };
    }
}
