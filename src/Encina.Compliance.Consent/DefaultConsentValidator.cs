using Encina.Compliance.Consent.Abstractions;
using Encina.Compliance.Consent.ReadModels;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Consent;

/// <summary>
/// Default implementation of <see cref="IConsentValidator"/> that checks consent
/// against <see cref="IConsentService"/>.
/// </summary>
/// <remarks>
/// <para>
/// For each required purpose, the validator:
/// <list type="number">
/// <item><description>Looks up the consent record via <see cref="IConsentService.GetConsentBySubjectAndPurposeAsync"/>.</description></item>
/// <item><description>Verifies the consent status is <see cref="ConsentStatus.Active"/>.</description></item>
/// <item><description>Checks for expiration based on <see cref="ConsentReadModel.ExpiresAtUtc"/>.</description></item>
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
    private readonly IConsentService _consentService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConsentValidator"/> class.
    /// </summary>
    /// <param name="consentService">The consent service for looking up consent state.</param>
    /// <param name="timeProvider">Time provider for expiration checks.</param>
    public DefaultConsentValidator(
        IConsentService consentService,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(consentService);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _consentService = consentService;
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
            // Step 1: Get the consent record via IConsentService
            var consentResult = await _consentService
                .GetConsentBySubjectAndPurposeAsync(subjectId, purpose, cancellationToken)
                .ConfigureAwait(false);

            // Handle infrastructure errors from the service
            if (consentResult.IsLeft)
            {
                return (EncinaError)consentResult;
            }

            var consentOpt = (Option<ConsentReadModel>)consentResult;

            // Step 2: Check consent exists
            if (consentOpt.IsNone)
            {
                missingPurposes.Add(purpose);
                errors.Add($"No consent record found for purpose '{purpose}'.");
                continue;
            }

            var consent = (ConsentReadModel)consentOpt;

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
