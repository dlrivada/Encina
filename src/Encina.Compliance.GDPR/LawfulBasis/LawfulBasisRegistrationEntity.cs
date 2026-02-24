namespace Encina.Compliance.GDPR;

/// <summary>
/// Persistence entity for <see cref="LawfulBasisRegistration"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a lawful basis registration,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// The <see cref="RequestTypeName"/> stores the assembly-qualified type name, allowing
/// reconstruction of the <see cref="Type"/> reference when mapping back to the domain model.
/// </para>
/// <para>
/// Use <see cref="LawfulBasisRegistrationMapper"/> to convert between this entity and
/// <see cref="LawfulBasisRegistration"/>.
/// </para>
/// </remarks>
public sealed class LawfulBasisRegistrationEntity
{
    /// <summary>
    /// Unique identifier for this registration record (GUID as string).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Assembly-qualified name of the request type this registration applies to.
    /// </summary>
    public required string RequestTypeName { get; set; }

    /// <summary>
    /// Integer value of the <see cref="LawfulBasis"/> enum.
    /// </summary>
    public required int BasisValue { get; set; }

    /// <summary>
    /// The purpose of the processing, if specified.
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Reference to a Legitimate Interest Assessment (LIA), if applicable.
    /// </summary>
    public string? LIAReference { get; set; }

    /// <summary>
    /// Reference to the specific legal provision, if applicable.
    /// </summary>
    public string? LegalReference { get; set; }

    /// <summary>
    /// Reference to the contract or pre-contractual steps, if applicable.
    /// </summary>
    public string? ContractReference { get; set; }

    /// <summary>
    /// Timestamp when this registration was created (UTC).
    /// </summary>
    public DateTimeOffset RegisteredAtUtc { get; set; }
}
