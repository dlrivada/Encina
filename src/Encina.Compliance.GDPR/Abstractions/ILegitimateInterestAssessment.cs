using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Validates Legitimate Interest Assessments (LIAs) for processing under Article 6(1)(f).
/// </summary>
/// <remarks>
/// <para>
/// This service is used by the lawful basis validation pipeline to verify that a referenced
/// LIA exists, has been completed, and is approved before processing can proceed under
/// <see cref="LawfulBasis.LegitimateInterests"/>.
/// </para>
/// <para>
/// The default implementation (<see cref="DefaultLegitimateInterestAssessment"/>) retrieves
/// the LIA from <see cref="ILIAStore"/> and returns:
/// </para>
/// <list type="bullet">
/// <item><c>Right(Approved)</c> when the LIA exists and is approved</item>
/// <item><c>Left(lia_not_found)</c> when no LIA record exists for the given reference</item>
/// <item><c>Left(lia_not_approved)</c> when the LIA exists but is not approved</item>
/// </list>
/// <para>
/// Custom implementations can add domain-specific validation logic, such as checking
/// LIA expiry, verifying DPO sign-off, or integrating with external governance systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Validate a LIA by its reference identifier
/// var result = await assessment.ValidateAsync("LIA-2024-FRAUD-001", cancellationToken);
///
/// result.Match(
///     Right: validation => Console.WriteLine($"Valid: {validation.IsValid}"),
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
/// </code>
/// </example>
public interface ILegitimateInterestAssessment
{
    /// <summary>
    /// Validates a Legitimate Interest Assessment by its reference identifier.
    /// </summary>
    /// <param name="liaReference">
    /// The LIA reference identifier (maps to <see cref="LIARecord.Id"/>).
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="LIAValidationResult"/> indicating the validation outcome,
    /// or an <see cref="EncinaError"/> if validation could not be performed
    /// (e.g., the LIA was not found or is not approved).
    /// </returns>
    ValueTask<Either<EncinaError, LIAValidationResult>> ValidateAsync(
        string liaReference,
        CancellationToken cancellationToken = default);
}
