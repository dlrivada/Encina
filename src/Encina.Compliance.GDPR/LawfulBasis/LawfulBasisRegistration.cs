using System.Reflection;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents a lawful basis registration linking a request type to its legal ground under Article 6.
/// </summary>
/// <remarks>
/// <para>
/// Each registration documents which lawful basis applies to a specific Encina request type
/// and optional metadata such as purpose, LIA reference, legal reference, or contract reference.
/// </para>
/// <para>
/// Registrations are stored in <see cref="ILawfulBasisRegistry"/> and can be created either:
/// </para>
/// <list type="bullet">
/// <item>Automatically from <see cref="LawfulBasisAttribute"/> decorations via <see cref="FromAttribute"/></item>
/// <item>Manually by constructing instances directly for programmatic registration</item>
/// </list>
/// </remarks>
public sealed record LawfulBasisRegistration
{
    /// <summary>
    /// The Encina request type this registration applies to.
    /// </summary>
    public required Type RequestType { get; init; }

    /// <summary>
    /// The lawful basis for processing under Article 6(1).
    /// </summary>
    public required LawfulBasis Basis { get; init; }

    /// <summary>
    /// The purpose of the processing. May be <c>null</c> if not specified.
    /// </summary>
    public string? Purpose { get; init; }

    /// <summary>
    /// Reference to a Legitimate Interest Assessment (LIA), if applicable.
    /// </summary>
    /// <remarks>
    /// Expected when <see cref="Basis"/> is <see cref="LawfulBasis.LegitimateInterests"/>.
    /// </remarks>
    public string? LIAReference { get; init; }

    /// <summary>
    /// Reference to the specific legal provision, if applicable.
    /// </summary>
    /// <remarks>
    /// Expected when <see cref="Basis"/> is <see cref="LawfulBasis.LegalObligation"/>.
    /// </remarks>
    public string? LegalReference { get; init; }

    /// <summary>
    /// Reference to the contract or pre-contractual steps, if applicable.
    /// </summary>
    /// <remarks>
    /// Expected when <see cref="Basis"/> is <see cref="LawfulBasis.Contract"/>.
    /// </remarks>
    public string? ContractReference { get; init; }

    /// <summary>
    /// Timestamp when this registration was created (UTC).
    /// </summary>
    public required DateTimeOffset RegisteredAtUtc { get; init; }

    /// <summary>
    /// Creates a <see cref="LawfulBasisRegistration"/> from a <see cref="LawfulBasisAttribute"/>
    /// found on the specified type.
    /// </summary>
    /// <param name="requestType">The request type decorated with <see cref="LawfulBasisAttribute"/>.</param>
    /// <param name="timeProvider">Time provider for timestamps. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    /// <returns>
    /// A new <see cref="LawfulBasisRegistration"/> populated from the attribute,
    /// or <c>null</c> if the type is not decorated with <see cref="LawfulBasisAttribute"/>.
    /// </returns>
    public static LawfulBasisRegistration? FromAttribute(Type requestType, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var attr = requestType.GetCustomAttribute<LawfulBasisAttribute>();
        if (attr is null)
        {
            return null;
        }

        var clock = timeProvider ?? TimeProvider.System;

        return new LawfulBasisRegistration
        {
            RequestType = requestType,
            Basis = attr.Basis,
            Purpose = attr.Purpose,
            LIAReference = attr.LIAReference,
            LegalReference = attr.LegalReference,
            ContractReference = attr.ContractReference,
            RegisteredAtUtc = clock.GetUtcNow()
        };
    }
}
