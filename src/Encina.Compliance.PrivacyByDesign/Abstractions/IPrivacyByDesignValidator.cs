using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Main orchestrator for Privacy by Design validation, combining data minimization,
/// purpose limitation, and default privacy checks into a single validation result.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary interface used by the <c>DataMinimizationPipelineBehavior</c>
/// to enforce Privacy by Design compliance in the Encina request pipeline. It coordinates
/// the individual validation strategies and produces an aggregate <see cref="PrivacyValidationResult"/>.
/// </para>
/// <para>
/// Per GDPR Article 25(1), the controller shall implement appropriate technical and
/// organisational measures "designed to implement data-protection principles, such as
/// data minimisation, in an effective manner and to integrate the necessary safeguards
/// into the processing."
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Full validation in the pipeline behavior
/// var result = await validator.ValidateAsync(request, cancellationToken);
/// result.Match(
///     Right: validation => { /* check validation.IsCompliant */ },
///     Left: error => { /* handle infrastructure error */ });
///
/// // Data minimization analysis only
/// var report = await validator.AnalyzeMinimizationAsync(request, cancellationToken);
///
/// // Purpose limitation check only
/// var purposeResult = await validator.ValidatePurposeLimitationAsync(
///     request, "Order Processing", cancellationToken);
///
/// // Default privacy check only
/// var defaults = await validator.ValidateDefaultsAsync(request, cancellationToken);
/// </code>
/// </example>
public interface IPrivacyByDesignValidator
{
    /// <summary>
    /// Performs a comprehensive Privacy by Design validation on the request, combining
    /// data minimization, purpose limitation, and default privacy checks.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to validate.</typeparam>
    /// <param name="request">The request instance to validate.</param>
    /// <param name="moduleId">Optional module identifier for module-scoped purpose lookups. When provided,
    /// the purpose registry searches module-specific registrations before falling back to global.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="PrivacyValidationResult"/> containing all detected violations,
    /// the minimization report, and the purpose validation result;
    /// or an <see cref="EncinaError"/> if the validation infrastructure fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called by the pipeline behavior for every request decorated with
    /// <see cref="EnforceDataMinimizationAttribute"/>. The result drives the enforcement
    /// decision (block, warn, or allow) based on <see cref="PrivacyByDesignEnforcementMode"/>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, PrivacyValidationResult>> ValidateAsync<TRequest>(
        TRequest request,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;

    /// <summary>
    /// Analyzes the data minimization profile of a request, producing a detailed
    /// <see cref="MinimizationReport"/> with field-level findings.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to analyze.</typeparam>
    /// <param name="request">The request instance to analyze.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="MinimizationReport"/> with necessary/unnecessary field breakdowns,
    /// a minimization score, and recommendations;
    /// or an <see cref="EncinaError"/> if the analysis infrastructure fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 25(2), "only personal data which are necessary for each specific
    /// purpose of the processing are processed." This method identifies fields marked with
    /// <see cref="NotStrictlyNecessaryAttribute"/> that have non-default values.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, MinimizationReport>> AnalyzeMinimizationAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;

    /// <summary>
    /// Validates that all fields in the request comply with the declared processing purpose.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to validate.</typeparam>
    /// <param name="request">The request instance to validate.</param>
    /// <param name="purpose">The declared processing purpose to validate against.</param>
    /// <param name="moduleId">Optional module identifier for module-scoped purpose lookups. When provided,
    /// the purpose registry searches module-specific registrations before falling back to global.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="PurposeValidationResult"/> indicating which fields comply and which violate
    /// the purpose limitation;
    /// or an <see cref="EncinaError"/> if the validation infrastructure fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit
    /// and legitimate purposes and not further processed in a manner that is incompatible
    /// with those purposes."
    /// </para>
    /// <para>
    /// Fields decorated with <see cref="PurposeLimitationAttribute"/> whose declared purpose
    /// does not match <paramref name="purpose"/> are flagged as violations.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, PurposeValidationResult>> ValidatePurposeLimitationAsync<TRequest>(
        TRequest request,
        string purpose,
        string? moduleId = null,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;

    /// <summary>
    /// Validates that all fields with declared privacy defaults match their expected values.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request to validate.</typeparam>
    /// <param name="request">The request instance to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="DefaultPrivacyFieldInfo"/> describing each field's
    /// declared default, actual value, and whether they match;
    /// or an <see cref="EncinaError"/> if the validation infrastructure fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
    /// organisational measures for ensuring that, by default, personal data are not made
    /// accessible without the individual's intervention."
    /// </para>
    /// <para>
    /// Fields decorated with <see cref="PrivacyDefaultAttribute"/> are checked: when their
    /// actual value differs from the declared default, a deviation is reported.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>> ValidateDefaultsAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;
}
