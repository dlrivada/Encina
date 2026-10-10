namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Size limits of the dead letter queue, shared by every store, schema and EF Core configuration.
/// </summary>
public static class DeadLetterStoreLimits
{
    /// <summary>Maximum <c>take</c> accepted by <see cref="IDeadLetterStore.GetMessagesAsync"/>.</summary>
    public const int MaxPageSize = 1000;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.SourcePattern"/>.</summary>
    public const int SourcePatternMaxLength = 64;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.SourceMessageId"/>.</summary>
    public const int SourceMessageIdMaxLength = 256;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.RequestType"/> (assembly-qualified generic names are long).</summary>
    public const int RequestTypeMaxLength = 1000;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.ErrorCode"/>.</summary>
    public const int ErrorCodeMaxLength = 256;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.TenantId"/>.</summary>
    public const int TenantIdMaxLength = 128;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.CorrelationId"/>.</summary>
    public const int CorrelationIdMaxLength = 256;

    /// <summary>Maximum length of <see cref="IDeadLetterMessage.ReplayResult"/> (an outcome code).</summary>
    public const int ReplayResultMaxLength = 256;
}
