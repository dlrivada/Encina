using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Default implementation of <see cref="ILegitimateInterestAssessment"/> that validates
/// LIAs by querying the <see cref="ILIAStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation performs straightforward validation:
/// </para>
/// <list type="bullet">
/// <item>Retrieves the LIA record from the store using the provided reference</item>
/// <item>Returns <c>Right(Approved)</c> if the record exists and its outcome is <see cref="LIAOutcome.Approved"/></item>
/// <item>Returns <c>Left(lia_not_found)</c> if no record exists for the given reference</item>
/// <item>Returns <c>Left(lia_not_approved)</c> if the record exists but is not approved
/// (either <see cref="LIAOutcome.Rejected"/> or <see cref="LIAOutcome.RequiresReview"/>)</item>
/// </list>
/// <para>
/// For more advanced validation (e.g., LIA expiry checks, DPO sign-off verification),
/// register a custom <see cref="ILegitimateInterestAssessment"/> implementation.
/// </para>
/// </remarks>
public sealed class DefaultLegitimateInterestAssessment : ILegitimateInterestAssessment
{
    private readonly ILIAStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLegitimateInterestAssessment"/> class.
    /// </summary>
    /// <param name="store">The LIA store to retrieve assessment records from.</param>
    public DefaultLegitimateInterestAssessment(ILIAStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, LIAValidationResult>> ValidateAsync(
        string liaReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(liaReference);

        var lookupResult = await _store.GetByReferenceAsync(liaReference, cancellationToken);

        return lookupResult.Bind(option => option.Match(
            Some: record => record.Outcome == LIAOutcome.Approved
                ? Right<EncinaError, LIAValidationResult>(LIAValidationResult.Approved())
                : Left<EncinaError, LIAValidationResult>(
                    GDPRErrors.LIANotApproved(liaReference, record.Outcome)),
            None: () => Left<EncinaError, LIAValidationResult>(
                GDPRErrors.LIANotFound(liaReference))));
    }
}
