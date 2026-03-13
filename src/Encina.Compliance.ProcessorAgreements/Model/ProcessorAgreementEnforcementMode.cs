namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// Controls how the <c>ProcessorValidationPipelineBehavior</c> responds when a request
/// targets a processor without a valid Data Processing Agreement.
/// </summary>
/// <remarks>
/// <para>
/// The enforcement mode allows gradual adoption in existing systems:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Disabled"/> — DPA validation is skipped entirely (for migration periods).</description></item>
/// <item><description><see cref="Warn"/> — Requests proceed but generate warnings in logs and diagnostics.</description></item>
/// <item><description><see cref="Block"/> — Requests are rejected with an error (full GDPR Article 28 compliance).</description></item>
/// </list>
/// </remarks>
public enum ProcessorAgreementEnforcementMode
{
    /// <summary>
    /// Requests targeting processors without a valid DPA are blocked with an error.
    /// </summary>
    /// <remarks>
    /// This is the recommended mode for production environments where GDPR Article 28
    /// compliance is mandatory.
    /// </remarks>
    Block = 0,

    /// <summary>
    /// Requests targeting processors without a valid DPA proceed but generate warnings.
    /// </summary>
    /// <remarks>
    /// Useful during initial adoption to identify which processors need agreements
    /// without disrupting existing operations.
    /// </remarks>
    Warn = 1,

    /// <summary>
    /// DPA validation is disabled entirely; no checks are performed.
    /// </summary>
    /// <remarks>
    /// Use only during migration periods or in non-GDPR environments.
    /// </remarks>
    Disabled = 2
}
