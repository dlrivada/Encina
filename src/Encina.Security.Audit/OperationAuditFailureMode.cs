namespace Encina.Security.Audit;

/// <summary>
/// What <see cref="AuditPipelineBehavior{TRequest, TResponse}"/> does with a request whose audit
/// entry cannot be built or written.
/// </summary>
/// <remarks>
/// A handler that returns <c>Left</c> or throws keeps its own error or exception in both modes; the
/// audit failure is logged. The audit write is always awaited before the behavior returns.
/// </remarks>
public enum OperationAuditFailureMode
{
    /// <summary>
    /// A request that succeeded fails with <see cref="OperationAuditErrors.WriteFailedCode"/>: no
    /// operation without its audit evidence. This is the default.
    /// </summary>
    FailClosed = 0,

    /// <summary>
    /// The request proceeds and every dropped write is logged at Warning: an explicit opt-out for
    /// applications that prefer availability to a complete audit trail.
    /// </summary>
    BestEffort = 1
}
