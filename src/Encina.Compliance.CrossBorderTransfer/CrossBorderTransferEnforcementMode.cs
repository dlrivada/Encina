namespace Encina.Compliance.CrossBorderTransfer;

/// <summary>
/// Controls how the cross-border transfer pipeline behavior responds to transfers
/// that lack a valid GDPR Chapter V legal basis.
/// </summary>
/// <remarks>
/// The enforcement mode determines whether non-compliant transfers are blocked,
/// emit warnings, or are ignored entirely. This supports gradual adoption of
/// cross-border transfer enforcement in existing applications.
/// </remarks>
public enum CrossBorderTransferEnforcementMode
{
    /// <summary>
    /// Non-compliant transfers are blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where GDPR Chapter V compliance
    /// is mandatory. Transfers without a valid adequacy decision, SCC agreement, or TIA
    /// will receive a <c>CrossBorderTransferErrors.TransferBlocked</c> error.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Non-compliant transfers log a warning but are allowed to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when cross-border transfer enforcement
    /// is being gradually introduced. All transfer violations are logged at Warning level
    /// and counted via OpenTelemetry metrics.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Transfer validation is completely disabled. The pipeline behavior is a no-op.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where cross-border transfers
    /// are managed externally. No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
