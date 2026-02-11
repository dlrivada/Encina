using Encina.Messaging.DeadLetter;

namespace Encina.Testing.Fakes.Models;

/// <summary>
/// In-memory implementation of <see cref="IDeadLetterMessage"/> for testing.
/// </summary>
public sealed class FakeDeadLetterMessage : IDeadLetterMessage
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <inheritdoc />
    public string RequestType { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RequestContent { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ErrorMessage { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? ExceptionType { get; set; }

    /// <inheritdoc />
    public string? ExceptionMessage { get; set; }

    /// <inheritdoc />
    public string? ExceptionStackTrace { get; set; }

    /// <inheritdoc />
    public string? CorrelationId { get; set; }

    /// <inheritdoc />
    public string SourcePattern { get; set; } = string.Empty;

    /// <inheritdoc />
    public int TotalRetryAttempts { get; set; }

    /// <inheritdoc />
    public DateTime FirstFailedAtUtc { get; set; } = TimeProvider.System.GetUtcNow().UtcDateTime;

    /// <inheritdoc />
    public DateTime DeadLetteredAtUtc { get; set; } = TimeProvider.System.GetUtcNow().UtcDateTime;

    /// <inheritdoc />
    public DateTime? ExpiresAtUtc { get; set; }

    /// <inheritdoc />
    public DateTime? ReplayedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ReplayResult { get; set; }

    /// <inheritdoc />
    public bool IsReplayed => ReplayedAtUtc.HasValue;

    /// <inheritdoc />
    public bool IsExpired => ExpiresAtUtc.HasValue && TimeProvider.System.GetUtcNow().UtcDateTime > ExpiresAtUtc.Value;

    /// <summary>
    /// Creates a deep copy of this message.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public FakeDeadLetterMessage Clone() => new()
    {
        Id = Id,
        RequestType = RequestType,
        RequestContent = RequestContent,
        ErrorMessage = ErrorMessage,
        ExceptionType = ExceptionType,
        ExceptionMessage = ExceptionMessage,
        ExceptionStackTrace = ExceptionStackTrace,
        CorrelationId = CorrelationId,
        SourcePattern = SourcePattern,
        TotalRetryAttempts = TotalRetryAttempts,
        FirstFailedAtUtc = FirstFailedAtUtc,
        DeadLetteredAtUtc = DeadLetteredAtUtc,
        ExpiresAtUtc = ExpiresAtUtc,
        ReplayedAtUtc = ReplayedAtUtc,
        ReplayResult = ReplayResult
    };
}
