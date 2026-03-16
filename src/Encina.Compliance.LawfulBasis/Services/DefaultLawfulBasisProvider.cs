using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.ReadModels;

using LanguageExt;

using static LanguageExt.Prelude;

using GDPR = Encina.Compliance.GDPR;

namespace Encina.Compliance.LawfulBasis.Services;

/// <summary>
/// Default implementation of <see cref="ILawfulBasisProvider"/> that resolves lawful basis
/// from the <see cref="ILawfulBasisService"/> and performs basis-specific validation.
/// </summary>
/// <remarks>
/// <para>
/// This provider performs the following validations:
/// </para>
/// <list type="bullet">
/// <item>Verifies a lawful basis registration exists for the request type</item>
/// <item>For <see cref="GDPR.LawfulBasis.LegitimateInterests"/>: warns if LIA reference is missing</item>
/// <item>For <see cref="GDPR.LawfulBasis.LegalObligation"/>: warns if legal reference is missing</item>
/// <item>For <see cref="GDPR.LawfulBasis.Contract"/>: warns if contract reference is missing</item>
/// </list>
/// <para>
/// Replace this by registering your own <see cref="ILawfulBasisProvider"/> implementation
/// for domain-specific validation such as consent status checks or LIA approval verification.
/// </para>
/// </remarks>
public sealed class DefaultLawfulBasisProvider : ILawfulBasisProvider
{
    private readonly ILawfulBasisService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLawfulBasisProvider"/> class.
    /// </summary>
    /// <param name="service">The lawful basis service to resolve registrations from.</param>
    public DefaultLawfulBasisProvider(ILawfulBasisService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>> GetBasisForRequestAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var requestTypeName = requestType.AssemblyQualifiedName ?? requestType.FullName ?? requestType.Name;
        return await _service.GetRegistrationByRequestTypeAsync(requestTypeName, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, LawfulBasisValidationResult>> ValidateBasisAsync<TRequest>(
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        var requestType = typeof(TRequest);
        var requestTypeName = requestType.AssemblyQualifiedName ?? requestType.FullName ?? requestType.Name;
        var lookupResult = await _service.GetRegistrationByRequestTypeAsync(requestTypeName, cancellationToken)
            .ConfigureAwait(false);

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

    private static LawfulBasisValidationResult ValidateRegistration(LawfulBasisReadModel registration)
    {
        var warnings = new List<string>();

        switch (registration.Basis)
        {
            case GDPR.LawfulBasis.LegitimateInterests when string.IsNullOrWhiteSpace(registration.LIAReference):
                warnings.Add(
                    "Legitimate interests basis requires a Legitimate Interest Assessment (LIA). " +
                    "No LIA reference was provided.");
                break;

            case GDPR.LawfulBasis.LegalObligation when string.IsNullOrWhiteSpace(registration.LegalReference):
                warnings.Add(
                    "Legal obligation basis should reference the specific legal provision. " +
                    "No legal reference was provided.");
                break;

            case GDPR.LawfulBasis.Contract when string.IsNullOrWhiteSpace(registration.ContractReference):
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
