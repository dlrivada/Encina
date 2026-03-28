using Encina.Messaging.Outbox;

namespace Encina.ADO.Sqlite.Outbox;

/// <summary>
/// Factory for creating <see cref="OutboxMessage"/> instances for ADO.NET Sqlite provider.
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
