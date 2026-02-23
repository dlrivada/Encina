using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Consent;

/// <summary>
/// Default implementation of <see cref="IConsentValidator"/> that checks consent
/// against <see cref="IConsentStore"/> and <see cref="IConsentVersionManager"/>.
/// </summary>
/// <remarks>
/// <para>
/// For each required purpose, the validator:
/// <list type="number">
/// <item><description>Checks that a consent record exists via <see cref="IConsentStore"/>.</description></item>
/// <item><description>Verifies the consent status is <see cref="ConsentStatus.Active"/>.</description></item>
/// <item><description>Checks for expiration based on <see cref="ConsentRecord.ExpiresAtUtc"/>.</description></item>
/// <item><description>Verifies the consent version is current via <see cref="IConsentVersionManager"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// The result is a <see cref="ConsentValidationResult"/> containing details about
/// missing, expired, withdrawn, or version-mismatched purposes with appropriate
/// error messages and warnings.
/// </para>
/// </remarks>
public sealed class DefaultConsentValidator : IConsentValidator
{
    private readonly IConsentStore _consentStore;
    private readonly IConsentVersionManager _versionManager;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConsentValidator"/> class.
    /// </summary>
    /// <param name="consentStore">The consent store for looking up consent records.</param>
    /// <param name="versionManager">The version manager for checking consent version currency.</param>
    /// <param name="timeProvider">Time provider for expiration checks.</param>
    public DefaultConsentValidator(
        IConsentStore consentStore,
        IConsentVersionManager versionManager,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(consentStore);
        ArgumentNullException.ThrowIfNull(versionManager);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _consentStore = consentStore;
        _versionManager = versionManager;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ConsentValidationResult>> ValidateAsync(
        string subjectId,
        IEnumerable<string> requiredPurposes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentNullException.ThrowIfNull(requiredPurposes);

        var errors = new List<string>();
        var warnings = new List<string>();
        var missingPurposes = new List<string>();

        foreach (var purpose in requiredPurposes)
        {
            // Step 1: Get the consent record
            var consentResult = await _consentStore
                .GetConsentAsync(subjectId, purpose, cancellationToken)
                .ConfigureAwait(false);

            // Handle infrastructure errors from the store
            if (consentResult.IsLeft)
            {
                return (EncinaError)consentResult;
            }

            var consentOpt = (Option<ConsentRecord>)consentResult;

            // Step 2: Check consent exists
            if (consentOpt.IsNone)
            {
                missingPurposes.Add(purpose);
                errors.Add($"No consent record found for purpose '{purpose}'.");
                continue;
            }

            var consent = (ConsentRecord)consentOpt;

            // Step 3: Check consent status
            switch (consent.Status)
            {
                case ConsentStatus.Withdrawn:
                    missingPurposes.Add(purpose);
                    errors.Add($"Consent for purpose '{purpose}' has been withdrawn.");
                    continue;

                case ConsentStatus.Expired:
                    missingPurposes.Add(purpose);
                    errors.Add($"Consent for purpose '{purpose}' has expired.");
                    continue;

                case ConsentStatus.RequiresReconsent:
                    missingPurposes.Add(purpose);
                    errors.Add($"Consent for purpose '{purpose}' requires reconsent due to version change.");
                    continue;
            }

            // Step 4: Check expiration at validation time
            if (consent.ExpiresAtUtc.HasValue && _timeProvider.GetUtcNow() >= consent.ExpiresAtUtc.Value)
            {
                missingPurposes.Add(purpose);
                errors.Add($"Consent for purpose '{purpose}' has expired at {consent.ExpiresAtUtc.Value:O}.");
                continue;
            }

            // Step 5: Check version currency via IConsentVersionManager
            var reconsentResult = await _versionManager
                .RequiresReconsentAsync(subjectId, purpose, cancellationToken)
                .ConfigureAwait(false);

            if (reconsentResult.IsLeft)
            {
                // Version manager error — treat as warning, don't block
                warnings.Add($"Could not verify consent version for purpose '{purpose}': version manager unavailable.");
                continue;
            }

            var requiresReconsent = (bool)reconsentResult;

            if (requiresReconsent)
            {
                missingPurposes.Add(purpose);
                errors.Add($"Consent for purpose '{purpose}' was given under an outdated version and requires reconsent.");
                continue;
            }
        }

        // Build result
        if (missingPurposes.Count > 0)
        {
            return Right<EncinaError, ConsentValidationResult>(
                ConsentValidationResult.Invalid(errors, warnings, missingPurposes));
        }

        if (warnings.Count > 0)
        {
            return Right<EncinaError, ConsentValidationResult>(
                ConsentValidationResult.ValidWithWarnings([.. warnings]));
        }

        return Right<EncinaError, ConsentValidationResult>(ConsentValidationResult.Valid());
    }
}
