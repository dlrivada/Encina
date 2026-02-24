using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Provides lawful basis resolution and validation for Encina request types under Article 6.
/// </summary>
/// <remarks>
/// <para>
/// The lawful basis provider is the primary service used by the pipeline behavior to:
/// </para>
/// <list type="number">
/// <item>Resolve the declared lawful basis for a request type from the <see cref="ILawfulBasisRegistry"/></item>
/// <item>Validate that basis-specific requirements are met (e.g., active consent, LIA approval)</item>
/// <item>Return a <see cref="LawfulBasisValidationResult"/> with the outcome</item>
/// </list>
/// <para>
/// Custom implementations can add domain-specific validation logic beyond basic registry lookups,
/// such as integrating with a consent management system or verifying LIA approval status.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Resolve the lawful basis for a request type
/// var basis = await provider.GetBasisForRequestAsync(
///     typeof(CreateOrderCommand), cancellationToken);
///
/// // Validate the lawful basis with full checks
/// var result = await provider.ValidateBasisAsync&lt;CreateOrderCommand&gt;(
///     cancellationToken);
/// </code>
/// </example>
public interface ILawfulBasisProvider
{
    /// <summary>
    /// Retrieves the declared lawful basis registration for a request type.
    /// </summary>
    /// <param name="requestType">The request type to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="Option{LawfulBasisRegistration}"/> containing the matching registration if found,
    /// or <see cref="Option{LawfulBasisRegistration}.None"/> if no lawful basis is declared,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetBasisForRequestAsync(
        Type requestType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the lawful basis for a request type, including basis-specific checks.
    /// </summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="LawfulBasisValidationResult"/> indicating whether the lawful basis is valid,
    /// or an <see cref="EncinaError"/> if validation could not be performed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive validation including:
    /// </para>
    /// <list type="bullet">
    /// <item>Verifying a lawful basis is declared in the registry</item>
    /// <item>For <see cref="LawfulBasis.Consent"/>: checking that active consent exists (if consent provider is available)</item>
    /// <item>For <see cref="LawfulBasis.LegitimateInterests"/>: verifying LIA reference is present</item>
    /// <item>For <see cref="LawfulBasis.LegalObligation"/>: verifying legal reference is present</item>
    /// <item>For <see cref="LawfulBasis.Contract"/>: verifying contract reference is present</item>
    /// </list>
    /// </remarks>
    ValueTask<Either<EncinaError, LawfulBasisValidationResult>> ValidateBasisAsync<TRequest>(
        CancellationToken cancellationToken = default)
        where TRequest : notnull;
}
