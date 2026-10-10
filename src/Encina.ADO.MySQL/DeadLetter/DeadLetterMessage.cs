using Encina.Messaging.DeadLetter;

namespace Encina.ADO.MySQL.DeadLetter;

/// <summary>
/// ADO.NET MySQL implementation of a dead letter message.
/// </summary>
/// <remarks>
/// A plain data holder: it never reads the clock. Expiry is evaluated against an instant supplied
/// by the caller (<see cref="IsExpiredAt"/>).
/// </remarks>
public sealed class DeadLetterMessage : IDeadLetterMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; }

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
}
