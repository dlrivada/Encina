using LanguageExt;

namespace Encina.Compliance.Consent;

/// <summary>
/// Manages consent term versions and reconsent requirements.
/// </summary>
/// <remarks>
/// <para>
/// The consent version manager tracks changes to consent terms over time. When consent
/// terms are updated (e.g., new data categories, expanded processing scope), a new version
/// is published and existing consents may need to be refreshed.
/// </para>
/// <para>
/// This supports GDPR Article 7 requirements by ensuring that consent is always linked
/// to the specific terms the data subject agreed to. When terms change materially, the
/// manager can flag existing consents as requiring reconsent.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Publish a new consent version
/// var version = new ConsentVersion
/// {
///     VersionId = "marketing-v3",
///     Purpose = ConsentPurposes.Marketing,
///     EffectiveFromUtc = DateTimeOffset.UtcNow,
///     Description = "Added social media retargeting to marketing scope",
///     RequiresExplicitReconsent = true
/// };
///
/// await versionManager.PublishNewVersionAsync(version, cancellationToken);
///
/// // Check if a user needs to reconsent
/// var needsReconsent = await versionManager.RequiresReconsentAsync(
///     "user-123", ConsentPurposes.Marketing, cancellationToken);
/// </code>
/// </example>
public interface IConsentVersionManager
{
    /// <summary>
    /// Gets the current (latest) consent version for a specific purpose.
    /// </summary>
    /// <param name="purpose">The processing purpose to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The current <see cref="ConsentVersion"/> for the purpose,
    /// or an <see cref="EncinaError"/> if no version exists or the lookup fails.
    /// </returns>
    ValueTask<Either<EncinaError, ConsentVersion>> GetCurrentVersionAsync(
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a new consent version for a processing purpose.
    /// </summary>
    /// <param name="version">The new consent version to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the version
    /// could not be published.
    /// </returns>
    /// <remarks>
    /// If <see cref="ConsentVersion.RequiresExplicitReconsent"/> is <c>true</c>, existing
    /// consents for the affected purpose should be transitioned to
    /// <see cref="ConsentStatus.RequiresReconsent"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> PublishNewVersionAsync(
        ConsentVersion version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a data subject needs to provide fresh consent due to version changes.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">The processing purpose to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the data subject's consent was given under an older version that
    /// requires explicit reconsent, <c>false</c> if consent is current,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> RequiresReconsentAsync(
        string subjectId,
        string purpose,
        CancellationToken cancellationToken = default);
}
