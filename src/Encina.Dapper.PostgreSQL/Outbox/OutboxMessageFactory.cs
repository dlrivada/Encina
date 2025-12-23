using Encina.Messaging.Outbox;

namespace Encina.Dapper.PostgreSQL.Outbox;

/// <summary>
/// Factory for creating <see cref="OutboxMessage"/> instances for Dapper PostgreSQL provider.
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
