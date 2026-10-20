using Encina.Messaging.DeadLetter;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="IDeadLetterMessage"/> for testing.
/// </summary>
/// <remarks>
/// The message never reads the clock. A timestamp left at its default is stamped by
/// <c>FakeDeadLetterStore.AddAsync</c> from the store's <see cref="TimeProvider"/>.
/// </remarks>
public sealed class FakeDeadLetterMessage : IDeadLetterMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RequestContent { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ErrorCode { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? ExceptionType { get; set; }

    /// <inheritdoc />
    public string? ExceptionStackTrace { get; set; }

    /// <inheritdoc />
    public string? CorrelationId { get; set; }

    /// <inheritdoc />
    public string SourcePattern { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SourceMessageId { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public int TotalRetryAttempts { get; set; }

    /// <inheritdoc />
    public DateTime FirstFailedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime DeadLetteredAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ExpiresAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ReplayClaimedAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ReplayedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ReplayResult { get; set; }

    /// <inheritdoc />
    public bool IsReplayed => ReplayedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsExpiredAt(DateTime utcNow) => ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= utcNow;

    /// <summary>
    /// Creates a deep copy of this message.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public FakeDeadLetterMessage Clone() => new()
    {
        Id = Id,
        RequestType = RequestType,
        RequestContent = RequestContent,
        ErrorCode = ErrorCode,
        ExceptionType = ExceptionType,
        ExceptionStackTrace = ExceptionStackTrace,
        CorrelationId = CorrelationId,
        SourcePattern = SourcePattern,
        SourceMessageId = SourceMessageId,
        TenantId = TenantId,
        TotalRetryAttempts = TotalRetryAttempts,
        FirstFailedAtUtc = FirstFailedAtUtc,
        DeadLetteredAtUtc = DeadLetteredAtUtc,
        ExpiresAtUtc = ExpiresAtUtc,
        ReplayClaimedAtUtc = ReplayClaimedAtUtc,
        ReplayedAtUtc = ReplayedAtUtc,
        ReplayResult = ReplayResult
    };
}
