using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Registry for managing lawful basis declarations as required by GDPR Article 6.
/// </summary>
/// <remarks>
/// <para>
/// Article 6 of the GDPR requires that personal data processing has a lawful basis.
/// This interface provides CRUD operations for managing lawful basis registrations
/// that link Encina request types to their legal grounds.
/// </para>
/// <para>
/// Implementations may store registrations in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store.
/// </para>
/// <para><b>The six lawful bases under Article 6(1):</b></para>
/// <list type="bullet">
/// <item><see cref="LawfulBasis.Consent"/> - Data subject consent (Article 6(1)(a))</item>
/// <item><see cref="LawfulBasis.Contract"/> - Contractual necessity (Article 6(1)(b))</item>
/// <item><see cref="LawfulBasis.LegalObligation"/> - Legal obligation (Article 6(1)(c))</item>
/// <item><see cref="LawfulBasis.VitalInterests"/> - Vital interests (Article 6(1)(d))</item>
/// <item><see cref="LawfulBasis.PublicTask"/> - Public task (Article 6(1)(e))</item>
/// <item><see cref="LawfulBasis.LegitimateInterests"/> - Legitimate interests (Article 6(1)(f))</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Register a lawful basis for a request type
/// var registration = new LawfulBasisRegistration
/// {
///     RequestType = typeof(CreateOrderCommand),
///     Basis = LawfulBasis.Contract,
///     Purpose = "Fulfill customer orders",
///     ContractReference = "Terms of Service v2.1",
///     RegisteredAtUtc = DateTimeOffset.UtcNow
/// };
///
/// await registry.RegisterAsync(registration, cancellationToken);
///
/// // Look up by request type
/// var result = await registry.GetByRequestTypeAsync(typeof(CreateOrderCommand), cancellationToken);
/// </code>
/// </example>
public interface ILawfulBasisRegistry
{
    /// <summary>
    /// Registers a new lawful basis declaration for a request type.
    /// </summary>
    /// <param name="registration">The lawful basis registration to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the registration
    /// could not be stored (e.g., duplicate request type).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RegisterAsync(
        LawfulBasisRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the lawful basis registration for a specific request type.
    /// </summary>
    /// <param name="requestType">The request type to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="Option{LawfulBasisRegistration}"/> containing the matching registration if found,
    /// or <see cref="Option{LawfulBasisRegistration}.None"/> if no registration exists for the given type,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the lawful basis registration by the assembly-qualified request type name.
    /// </summary>
    /// <param name="requestTypeName">The assembly-qualified name of the request type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="Option{LawfulBasisRegistration}"/> containing the matching registration if found,
    /// or <see cref="Option{LawfulBasisRegistration}.None"/> if no registration exists for the given name,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// This method is useful for persistence providers that store the type name as a string
    /// and need to resolve registrations without a <see cref="Type"/> reference.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeNameAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered lawful basis declarations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all lawful basis registrations, or an <see cref="EncinaError"/>
    /// if the registry could not be queried.
    /// </returns>
    /// <remarks>
    /// This method is typically used for compliance auditing and reporting.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisRegistration>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
