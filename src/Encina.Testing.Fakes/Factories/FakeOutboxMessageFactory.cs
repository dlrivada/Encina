using Encina.Messaging.Outbox;
using Encina.Testing.Fakes.Models;

namespace Encina.Testing.Fakes.Factories;

/// <summary>
/// Fake implementation of <see cref="IOutboxMessageFactory"/> for testing.
/// </summary>
public sealed class FakeOutboxMessageFactory : IOutboxMessageFactory
{
    /// <inheritdoc />
    public IOutboxMessage Create(
        Guid id,
        string notificationType,
        string content,
        DateTime createdAtUtc)
    {
        return new FakeOutboxMessage
        {
            Id = id,
            NotificationType = notificationType,
            Content = content,
            CreatedAtUtc = createdAtUtc,
            RetryCount = 0
        };
    }
}
