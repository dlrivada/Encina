using Encina.Messaging.Outbox;

namespace Encina.Dapper.MySQL.Outbox;

/// <summary>
/// Factory for creating <see cref="OutboxMessage"/> instances for Dapper MySQL provider.
/// </summary>
public sealed class OutboxMessageFactory : IOutboxMessageFactory
{
    /// <inheritdoc />
    public IOutboxMessage Create(
        Guid id,
        string notificationType,
        string content,
        DateTime createdAtUtc)
    {
        return new OutboxMessage
        {
            Id = id,
            NotificationType = notificationType,
            Content = content,
            CreatedAtUtc = createdAtUtc,
            RetryCount = 0
        };
    }
}
