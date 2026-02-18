using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Validates GDPR compliance for Encina requests that process personal data.
/// </summary>
/// <remarks>
/// <para>
/// The compliance validator checks whether a request satisfies GDPR requirements before
/// personal data processing occurs. This includes verifying:
/// </para>
/// <list type="bullet">
/// <item><b>Lawful basis</b> (Article 6): processing has a valid legal ground</item>
/// <item><b>Purpose limitation</b> (Article 5(1)(b)): processing is limited to stated purposes</item>
/// <item><b>Registered activity</b> (Article 30): processing is documented in the RoPA</item>
/// </list>
/// <para>
/// The validator is invoked by <c>GDPRCompliancePipelineBehavior</c> for requests
/// decorated with <c>[ProcessingActivity]</c> or <c>[ProcessesPersonalData]</c> attributes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom compliance validator
/// public sealed class StrictComplianceValidator : IGDPRComplianceValidator
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, ComplianceResult&gt;&gt; ValidateAsync&lt;TRequest&gt;(
///         TRequest request,
///         IRequestContext context,
///         CancellationToken cancellationToken)
///     {
///         // Validate lawful basis, purpose limitation, etc.
///         return ComplianceResult.Compliant();
///     }
/// }
/// </code>
/// </example>
public interface IGDPRComplianceValidator
{
    /// <summary>
    /// Validates whether the specified request complies with GDPR requirements.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being validated.</typeparam>
    /// <param name="request">The request to validate for compliance.</param>
    /// <param name="context">The ambient request context with user and tenant information.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ComplianceResult"/> indicating whether the request is compliant,
    /// or an <see cref="EncinaError"/> if validation could not be performed.
    /// </returns>
    ValueTask<Either<EncinaError, ComplianceResult>> ValidateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken = default);
}
