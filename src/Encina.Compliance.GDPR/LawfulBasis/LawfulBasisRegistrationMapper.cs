namespace Encina.Compliance.GDPR;

/// <summary>
/// Maps between <see cref="LawfulBasisRegistration"/> domain records and
/// <see cref="LawfulBasisRegistrationEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the conversion of <see cref="Type"/> references to/from
/// assembly-qualified name strings, and <see cref="LawfulBasis"/> enum values to/from integers.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve lawful basis registrations without coupling to the domain model.
/// </para>
/// </remarks>
public static class LawfulBasisRegistrationMapper
{
    /// <summary>
    /// Converts a domain <see cref="LawfulBasisRegistration"/> to a persistence entity.
    /// </summary>
    /// <param name="registration">The domain registration to convert.</param>
    /// <returns>A <see cref="LawfulBasisRegistrationEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registration"/> is <c>null</c>.</exception>
    public static LawfulBasisRegistrationEntity ToEntity(LawfulBasisRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        return new LawfulBasisRegistrationEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            RequestTypeName = registration.RequestType.AssemblyQualifiedName!,
            BasisValue = (int)registration.Basis,
            Purpose = registration.Purpose,
            LIAReference = registration.LIAReference,
            LegalReference = registration.LegalReference,
            ContractReference = registration.ContractReference,
            RegisteredAtUtc = registration.RegisteredAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="LawfulBasisRegistration"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="LawfulBasisRegistration"/> if the <see cref="LawfulBasisRegistrationEntity.RequestTypeName"/>
    /// can be resolved to a <see cref="Type"/>, or <c>null</c> if the type cannot be found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static LawfulBasisRegistration? ToDomain(LawfulBasisRegistrationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var requestType = Type.GetType(entity.RequestTypeName);
        if (requestType is null)
        {
            return null;
        }

        return new LawfulBasisRegistration
        {
            RequestType = requestType,
            Basis = (LawfulBasis)entity.BasisValue,
            Purpose = entity.Purpose,
            LIAReference = entity.LIAReference,
            LegalReference = entity.LegalReference,
            ContractReference = entity.ContractReference,
            RegisteredAtUtc = entity.RegisteredAtUtc
        };
    }
}
