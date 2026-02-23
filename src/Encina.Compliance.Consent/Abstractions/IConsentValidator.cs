using LanguageExt;

namespace Encina.Compliance.Consent;

/// <summary>
/// Validates whether a data subject has given valid consent for required processing purposes.
/// </summary>
/// <remarks>
/// <para>
/// The consent validator performs comprehensive consent checks, verifying that active,
/// non-expired consent exists for all required purposes. It also checks consent version
/// currency to detect when reconsent is needed.
/// </para>
/// <para>
/// This interface is used by the <c>ConsentRequiredPipelineBehavior</c> to enforce consent
/// requirements before processing proceeds. Custom implementations can add domain-specific
/// validation logic beyond basic consent status checks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate consent for multiple purposes
/// var result = await validator.ValidateAsync(
///     "user-123",
///     [ConsentPurposes.Analytics, ConsentPurposes.Personalization],
///     cancellationToken);
///
/// result.Match(
///     Right: validationResult =&gt;
///     {
///         if (!validationResult.IsValid)
///             logger.LogWarning("Missing consent for: {Purposes}",
///                 string.Join(", ", validationResult.MissingPurposes));
///     },
///     Left: error =&gt; logger.LogError("Consent validation failed: {Error}", error.Message));
/// </code>
/// </example>
public interface IConsentValidator
{
    /// <summary>
    /// Validates whether a data subject has valid consent for all required processing purposes.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="requiredPurposes">The processing purposes that require consent.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ConsentValidationResult"/> indicating whether all required consents are valid,
    /// or an <see cref="EncinaError"/> if validation could not be performed.
    /// </returns>
    ValueTask<Either<EncinaError, ConsentValidationResult>> ValidateAsync(
        string subjectId,
        IEnumerable<string> requiredPurposes,
        CancellationToken cancellationToken = default);
}
