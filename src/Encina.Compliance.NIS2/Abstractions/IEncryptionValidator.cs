using LanguageExt;

namespace Encina.Compliance.NIS2.Abstractions;

/// <summary>
/// Validates cryptography and encryption posture under NIS2 Article 21(2)(h).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 21(2)(h), entities must implement "policies and procedures regarding
/// the use of cryptography and, where appropriate, encryption."
/// </para>
/// <para>
/// The default implementation validates against the configured <c>NIS2Options.EncryptedDataCategories</c>
/// and <c>NIS2Options.EncryptedEndpoints</c>. Applications should register a custom
/// <see cref="IEncryptionValidator"/> that integrates with their infrastructure to perform
/// actual encryption posture checks (e.g., verifying TLS configurations, checking at-rest
/// encryption status in storage systems).
/// </para>
/// </remarks>
public interface IEncryptionValidator
{
    /// <summary>
    /// Validates that data of the specified category is encrypted at rest.
    /// </summary>
    /// <param name="dataCategory">
    /// The category of data to validate (e.g., "PII", "HealthRecords", "FinancialData").
    /// Must match a category registered in <c>NIS2Options.EncryptedDataCategories</c>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if encryption at rest is confirmed for this category;
    /// <c>false</c> if not encrypted; or an <see cref="EncinaError"/> if validation failed.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> IsDataEncryptedAtRestAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that data transmitted to the specified endpoint is encrypted in transit.
    /// </summary>
    /// <param name="endpoint">
    /// The endpoint to validate (e.g., "https://api.example.com", "payment-gateway").
    /// Must match an endpoint registered in <c>NIS2Options.EncryptedEndpoints</c>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if encryption in transit is confirmed for this endpoint;
    /// <c>false</c> if not encrypted; or an <see cref="EncinaError"/> if validation failed.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> IsDataEncryptedInTransitAsync(
        string endpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the overall encryption policy compliance.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the entity's encryption policy meets NIS2 requirements;
    /// <c>false</c> if gaps exist; or an <see cref="EncinaError"/> if validation failed.
    /// </returns>
    /// <remarks>
    /// This method performs a comprehensive check of the entity's cryptographic posture,
    /// including key management, approved algorithms, and encryption coverage across
    /// all configured data categories and endpoints.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> ValidateEncryptionPolicyAsync(
        CancellationToken cancellationToken = default);
}
