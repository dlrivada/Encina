using Encina.Messaging.Outbox;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="IOutboxMessage"/> for testing.
/// </summary>
public sealed class FakeOutboxMessage : IOutboxMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public string NotificationType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime CreatedAtUtc { get; set; } = TimeProvider.System.GetUtcNow().UtcDateTime;

    /// <inheritdoc />
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsDeadLettered(int maxRetries) => RetryCount >= maxRetries && !IsProcessed;

    /// <summary>
    /// Creates a deep copy of this message.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public FakeOutboxMessage Clone() => new()
    {
        Id = Id,
        NotificationType = NotificationType,
        Content = Content,
        CreatedAtUtc = CreatedAtUtc,
        ProcessedAtUtc = ProcessedAtUtc,
        ErrorMessage = ErrorMessage,
        RetryCount = RetryCount,
        NextRetryAtUtc = NextRetryAtUtc
    };
}
