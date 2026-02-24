using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Default implementation of <see cref="ILawfulBasisProvider"/> that resolves lawful basis
/// from the <see cref="ILawfulBasisRegistry"/> and performs basis-specific validation.
/// </summary>
/// <remarks>
/// <para>
/// This provider performs the following validations:
/// </para>
/// <list type="bullet">
/// <item>Verifies a lawful basis registration exists for the request type</item>
/// <item>For <see cref="LawfulBasis.LegitimateInterests"/>: warns if <see cref="LawfulBasisRegistration.LIAReference"/> is missing</item>
/// <item>For <see cref="LawfulBasis.LegalObligation"/>: warns if <see cref="LawfulBasisRegistration.LegalReference"/> is missing</item>
/// <item>For <see cref="LawfulBasis.Contract"/>: warns if <see cref="LawfulBasisRegistration.ContractReference"/> is missing</item>
/// </list>
/// <para>
/// Replace this by registering your own <see cref="ILawfulBasisProvider"/> implementation
/// for domain-specific validation such as consent status checks or LIA approval verification.
/// </para>
/// </remarks>
public sealed class DefaultLawfulBasisProvider : ILawfulBasisProvider
{
    private readonly ILawfulBasisRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLawfulBasisProvider"/> class.
    /// </summary>
    /// <param name="registry">The lawful basis registry to resolve registrations from.</param>
    public DefaultLawfulBasisProvider(ILawfulBasisRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetBasisForRequestAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        return _registry.GetByRequestTypeAsync(requestType, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, LawfulBasisValidationResult>> ValidateBasisAsync<TRequest>(
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        var requestType = typeof(TRequest);
        var lookupResult = await _registry.GetByRequestTypeAsync(requestType, cancellationToken);

        return lookupResult.Match(
            Right: option => option.Match(
                Some: registration => Right<EncinaError, LawfulBasisValidationResult>(
                    ValidateRegistration(registration)),
                None: () => Right<EncinaError, LawfulBasisValidationResult>(
                    LawfulBasisValidationResult.Invalid(
                        $"No lawful basis is declared for request type '{requestType.Name}'. " +
                        "All personal data processing must have a lawful basis under Article 6(1)."))),
            Left: error => Left<EncinaError, LawfulBasisValidationResult>(error));
    }

    private static LawfulBasisValidationResult ValidateRegistration(LawfulBasisRegistration registration)
    {
        var warnings = new List<string>();

        switch (registration.Basis)
        {
            case LawfulBasis.LegitimateInterests when string.IsNullOrWhiteSpace(registration.LIAReference):
                warnings.Add(
                    "Legitimate interests basis requires a Legitimate Interest Assessment (LIA). " +
                    "No LIA reference was provided.");
                break;

            case LawfulBasis.LegalObligation when string.IsNullOrWhiteSpace(registration.LegalReference):
                warnings.Add(
                    "Legal obligation basis should reference the specific legal provision. " +
                    "No legal reference was provided.");
                break;

            case LawfulBasis.Contract when string.IsNullOrWhiteSpace(registration.ContractReference):
                warnings.Add(
                    "Contract basis should reference the specific contract or terms. " +
                    "No contract reference was provided.");
                break;
        }

        return warnings.Count > 0
            ? LawfulBasisValidationResult.ValidWithWarnings(registration.Basis, [.. warnings])
            : LawfulBasisValidationResult.Valid(registration.Basis);
    }
}
