using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Validates Data Processing Agreement compliance for processors.
/// </summary>
/// <remarks>
/// <para>
/// The DPA validator provides a high-level validation API that queries both the
/// <see cref="IProcessorRegistry"/> (processor identity) and <see cref="IDPAStore"/>
/// (contractual state) to determine whether a processor has a valid, active, and
/// fully compliant Data Processing Agreement per GDPR Article 28(3).
/// </para>
/// <para>
/// Validation checks include:
/// </para>
/// <list type="bullet">
/// <item><description>Processor existence in the registry.</description></item>
/// <item><description>Presence of an active DPA (<see cref="DPAStatus.Active"/>).</description></item>
/// <item><description>DPA expiration status (not expired or past due).</description></item>
/// <item><description>Mandatory term compliance (all 8 terms from Article 28(3)(a)-(h)).</description></item>
/// <item><description>SCC requirements for cross-border transfers (Articles 46-49).</description></item>
/// </list>
/// <para>
/// <see cref="HasValidDPAAsync"/> is optimized for the pipeline behavior hot path,
/// returning a simple boolean. <see cref="ValidateAsync"/> provides detailed results
/// including missing terms and warnings for compliance dashboards.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Quick check in pipeline behavior (hot path)
/// var hasValid = await validator.HasValidDPAAsync("stripe-payments", ct);
///
/// // Detailed validation for compliance dashboard
/// var result = await validator.ValidateAsync("stripe-payments", ct);
/// if (result.IsRight &amp;&amp; !result.Match(r => r.IsValid, _ => false))
/// {
///     // Handle missing terms, warnings, etc.
/// }
///
/// // Bulk validation for regulatory audit
/// var allResults = await validator.ValidateAllAsync(ct);
/// </code>
/// </example>
public interface IDPAValidator
{
    /// <summary>
    /// Performs a detailed validation of a processor's Data Processing Agreement compliance.
    /// </summary>
    /// <param name="processorId">The unique identifier of the processor to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="DPAValidationResult"/> with detailed compliance information,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The validation flow:
    /// </para>
    /// <list type="number">
    /// <item><description>Verifies the processor exists in <see cref="IProcessorRegistry"/>.</description></item>
    /// <item><description>Retrieves the active DPA from <see cref="IDPAStore.GetActiveByProcessorIdAsync"/>.</description></item>
    /// <item><description>Checks DPA status, expiration, mandatory terms, and SCC requirements.</description></item>
    /// <item><description>Returns a <see cref="DPAValidationResult"/> with <see cref="DPAValidationResult.IsValid"/>,
    /// <see cref="DPAValidationResult.MissingTerms"/>, and <see cref="DPAValidationResult.Warnings"/>.</description></item>
    /// </list>
    /// <para>
    /// Per Article 28(3), the result includes verification of all eight mandatory
    /// contractual terms (a)-(h) and identifies any gaps for remediation.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, DPAValidationResult>> ValidateAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a lightweight check of whether a processor has a valid Data Processing Agreement.
    /// </summary>
    /// <param name="processorId">The unique identifier of the processor to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the processor has a valid, active, and fully compliant DPA;
    /// <see langword="false"/> otherwise; or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is optimized for the <c>ProcessorValidationPipelineBehavior</c> hot path.
    /// It performs the same checks as <see cref="ValidateAsync"/> but returns a simple boolean
    /// instead of the full <see cref="DPAValidationResult"/>, avoiding allocation of the
    /// detailed result object.
    /// </para>
    /// <para>
    /// A DPA is considered valid when:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The processor exists in the registry.</description></item>
    /// <item><description>An active DPA exists with <see cref="DPAStatus.Active"/> status.</description></item>
    /// <item><description>The DPA has not expired (<see cref="DataProcessingAgreement.IsActive"/> returns <see langword="true"/>).</description></item>
    /// <item><description>All mandatory terms are met (<see cref="DPAMandatoryTerms.IsFullyCompliant"/>).</description></item>
    /// </list>
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> HasValidDPAAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Data Processing Agreement compliance for all registered processors.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="DPAValidationResult"/> — one per registered processor —
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no processors
    /// are registered.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Performs <see cref="ValidateAsync"/> for every processor in the <see cref="IProcessorRegistry"/>.
    /// Primarily used for compliance dashboards, regulatory audits, and periodic health checks
    /// that need an overview of the entire processor landscape.
    /// </para>
    /// <para>
    /// Per the accountability principle (Article 5(2)), the controller must be able to
    /// demonstrate compliance. This method provides the data needed for that demonstration.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DPAValidationResult>>> ValidateAllAsync(
        CancellationToken cancellationToken = default);
}
