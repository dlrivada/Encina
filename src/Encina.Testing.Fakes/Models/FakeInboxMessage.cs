using Encina.Messaging.Inbox;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="IInboxMessage"/> for testing.
/// </summary>
public sealed class FakeInboxMessage : IInboxMessage
{
    /// <inheritdoc />
    public string MessageId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Response { get; set; }

    /// <inheritdoc />
    public string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? ProcessedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <inheritdoc />
    public int RetryCount { get; set; }

    /// <inheritdoc />
    public DateTime? NextRetryAtUtc { get; set; }

    /// <inheritdoc />
    public bool IsProcessed => ProcessedAtUtc.HasValue && ErrorMessage == null;

    /// <inheritdoc />
    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc;

    /// <summary>
    /// Creates a deep copy of this message.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public FakeInboxMessage Clone() => new()
    {
        MessageId = MessageId,
        RequestType = RequestType,
        Response = Response,
        ErrorMessage = ErrorMessage,
        ReceivedAtUtc = ReceivedAtUtc,
        ProcessedAtUtc = ProcessedAtUtc,
        ExpiresAtUtc = ExpiresAtUtc,
        RetryCount = RetryCount,
        NextRetryAtUtc = NextRetryAtUtc
    };
}
