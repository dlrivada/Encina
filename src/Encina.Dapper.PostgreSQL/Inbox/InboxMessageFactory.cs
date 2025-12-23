using System.Text.Json;
using Encina.Messaging.Inbox;

namespace Encina.Dapper.PostgreSQL.Inbox;

/// <summary>
/// Factory for creating <see cref="InboxMessage"/> instances for Dapper PostgreSQL provider.
/// </summary>
public sealed class InboxMessageFactory : IInboxMessageFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <inheritdoc />
    public IInboxMessage Create(
        string messageId,
        string requestType,
        DateTime receivedAtUtc,
        DateTime expiresAtUtc,
        InboxMetadata? metadata)
    {
        return new InboxMessage
        {
            MessageId = messageId,
            RequestType = requestType,
            ReceivedAtUtc = receivedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            RetryCount = 0,
            Metadata = metadata != null ? JsonSerializer.Serialize(metadata, JsonOptions) : null
        };
    }
}
