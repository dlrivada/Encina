namespace Encina.Compliance.GDPR;

/// <summary>
/// Controls how the lawful basis pipeline behavior responds to missing or invalid lawful basis declarations.
/// </summary>
/// <remarks>
/// The enforcement mode determines whether lawful basis violations block processing,
/// emit warnings, or are ignored entirely. This supports gradual adoption of
/// lawful basis enforcement in existing applications.
/// </remarks>
public enum LawfulBasisEnforcementMode
{
    /// <summary>
    /// Requests without a valid lawful basis are blocked and an error is returned.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production systems where GDPR Article 6(1) compliance
    /// is mandatory. Requests without a declared and valid lawful basis will receive a
    /// <c>GDPRErrors.LawfulBasisNotDeclared</c> error.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Requests without a valid lawful basis log a warning but are allowed to proceed.
    /// </summary>
    /// <remarks>
    /// Useful during migration or testing phases when lawful basis enforcement is being
    /// gradually introduced. All violations are logged at Warning level.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// Lawful basis validation is completely disabled. The pipeline behavior is a no-op.
    /// </summary>
    /// <remarks>
    /// Useful for development environments or scenarios where lawful basis is managed
    /// externally. No validation, logging, or metrics are emitted.
    /// </remarks>
    Disabled = 2
}
