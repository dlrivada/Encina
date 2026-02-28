using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Service for applying irreversible anonymization techniques to data objects.
/// </summary>
/// <remarks>
/// <para>
/// Anonymization renders personal data no longer identifiable, placing it outside the
/// scope of GDPR (Recital 26). Unlike pseudonymization, anonymization is irreversible —
/// once applied, the original data cannot be recovered.
/// </para>
/// <para>
/// The anonymizer applies configured techniques (generalization, suppression, perturbation,
/// data masking, k-anonymity, l-diversity, t-closeness) to data fields based on an
/// <see cref="AnonymizationProfile"/>. Each field rule in the profile specifies which
/// technique and parameters to use.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations are registered via <c>TryAdd</c> in DI, allowing users to override
/// the default implementation with custom anonymization logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var profile = AnonymizationProfile.Create("analytics", [
///     new FieldAnonymizationRule { FieldName = "Email", Technique = AnonymizationTechnique.Suppression },
///     new FieldAnonymizationRule { FieldName = "Age", Technique = AnonymizationTechnique.Generalization,
///         Parameters = new Dictionary&lt;string, object&gt; { ["Granularity"] = 10 } }
/// ]);
///
/// var result = await anonymizer.AnonymizeAsync(customerData, profile, cancellationToken);
/// </code>
/// </example>
public interface IAnonymizer
{
    /// <summary>
    /// Anonymizes the fields of a data object according to the specified profile.
    /// </summary>
    /// <typeparam name="T">The type of the data object to anonymize.</typeparam>
    /// <param name="data">The data object whose fields will be anonymized in place.</param>
    /// <param name="profile">The anonymization profile specifying which fields to anonymize and how.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A new instance of <typeparamref name="T"/> with anonymized field values,
    /// or an <see cref="EncinaError"/> if the anonymization could not be applied.
    /// </returns>
    /// <remarks>
    /// The returned object is a modified copy — the original <paramref name="data"/> is not mutated.
    /// Fields not covered by the profile are left unchanged.
    /// </remarks>
    ValueTask<Either<EncinaError, T>> AnonymizeAsync<T>(
        T data,
        AnonymizationProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonymizes fields and returns a detailed result describing the transformations applied.
    /// </summary>
    /// <typeparam name="T">The type of the data object to anonymize.</typeparam>
    /// <param name="data">The data object whose fields will be anonymized.</param>
    /// <param name="profile">The anonymization profile specifying which fields to anonymize and how.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="AnonymizationResult"/> describing the outcome of the operation
    /// (field counts, techniques applied), or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Use this method when audit logging or compliance reporting requires knowledge
    /// of exactly which fields were transformed and with which technique.
    /// </remarks>
    ValueTask<Either<EncinaError, AnonymizationResult>> AnonymizeFieldsAsync<T>(
        T data,
        AnonymizationProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a data object has already been anonymized.
    /// </summary>
    /// <typeparam name="T">The type of the data object to inspect.</typeparam>
    /// <param name="data">The data object to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the data appears to have been anonymized (e.g., suppressed fields
    /// contain null/default values, generalized fields match expected patterns),
    /// <c>false</c> otherwise, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// This is a heuristic check and may not be 100% accurate. It is useful for preventing
    /// double-anonymization or for verifying that anonymization was applied correctly.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsAnonymizedAsync<T>(
        T data,
        CancellationToken cancellationToken = default);
}
