namespace Encina.Security.Audit;

/// <summary>
/// Thrown by <see cref="IOperationAuditEntryFactory"/> when the PII masker or redactor could not mask a payload,
/// so the audit entry is not built and nothing unmasked can reach storage.
/// </summary>
/// <remarks>
/// The message is fixed and never carries payload content; the masker's own exception is kept as
/// <see cref="Exception.InnerException"/> and must be logged through <c>ForLogging()</c> only.
/// <see cref="AuditPipelineBehavior{TRequest, TResponse}"/> turns it into
/// <see cref="OperationAuditErrors.MaskingFailedCode"/>.
/// </remarks>
public sealed class OperationAuditMaskingException : Exception
{
    private const string FixedMessage = "The audit payload could not be masked, so no audit entry was built.";

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAuditMaskingException"/> class.
    /// </summary>
    public OperationAuditMaskingException()
        : base(FixedMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAuditMaskingException"/> class.
    /// </summary>
    /// <param name="innerException">The exception the masker or redactor threw.</param>
    public OperationAuditMaskingException(Exception? innerException)
        : base(FixedMessage, innerException)
    {
    }
}
