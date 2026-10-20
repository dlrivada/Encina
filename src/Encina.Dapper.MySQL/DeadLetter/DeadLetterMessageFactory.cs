using Encina.Messaging.DeadLetter;

namespace Encina.Dapper.MySQL.DeadLetter;

/// <summary>
/// Factory for creating Dapper MySQL dead letter message instances.
/// </summary>
public sealed class DeadLetterMessageFactory : IDeadLetterMessageFactory
{
    /// <inheritdoc />
    public IDeadLetterMessage Create(DeadLetterData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new DeadLetterMessage
        {
            Id = data.Id,
            RequestType = data.RequestType,
            RequestContent = data.RequestContent,
            ErrorCode = data.ErrorCode,
            ExceptionType = data.ExceptionType,
            ExceptionStackTrace = data.ExceptionStackTrace,
            CorrelationId = data.CorrelationId,
            SourcePattern = data.SourcePattern,
            SourceMessageId = data.SourceMessageId,
            TenantId = data.TenantId,
            TotalRetryAttempts = data.TotalRetryAttempts,
            FirstFailedAtUtc = data.FirstFailedAtUtc,
            DeadLetteredAtUtc = data.DeadLetteredAtUtc,
            ExpiresAtUtc = data.ExpiresAtUtc
        };
    }
}
