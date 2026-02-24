using Encina.Compliance.GDPR;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.LawfulBasis;

/// <summary>
/// MongoDB document representation of a <see cref="LawfulBasisRegistrationEntity"/>.
/// </summary>
/// <remarks>
/// Maps to the lawful_basis_registrations collection. Uses snake_case naming convention
/// for MongoDB field names to follow MongoDB community conventions.
/// </remarks>
public sealed class LawfulBasisRegistrationDocument
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    [BsonId]
    [BsonElement("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified request type name.
    /// </summary>
    [BsonElement("request_type_name")]
    public string RequestTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lawful basis enum value.
    /// </summary>
    [BsonElement("basis_value")]
    public int BasisValue { get; set; }

    /// <summary>
    /// Gets or sets the purpose of processing.
    /// </summary>
    [BsonElement("purpose")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Gets or sets the Legitimate Interest Assessment reference.
    /// </summary>
    [BsonElement("lia_reference")]
    public string? LIAReference { get; set; }

    /// <summary>
    /// Gets or sets the legal reference.
    /// </summary>
    [BsonElement("legal_reference")]
    public string? LegalReference { get; set; }

    /// <summary>
    /// Gets or sets the contract reference.
    /// </summary>
    [BsonElement("contract_reference")]
    public string? ContractReference { get; set; }

    /// <summary>
    /// Gets or sets the registration timestamp in UTC.
    /// </summary>
    [BsonElement("registered_at_utc")]
    public DateTimeOffset RegisteredAtUtc { get; set; }

    /// <summary>
    /// Creates a document from an entity.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>A new document instance.</returns>
    public static LawfulBasisRegistrationDocument FromEntity(LawfulBasisRegistrationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LawfulBasisRegistrationDocument
        {
            Id = entity.Id,
            RequestTypeName = entity.RequestTypeName,
            BasisValue = entity.BasisValue,
            Purpose = entity.Purpose,
            LIAReference = entity.LIAReference,
            LegalReference = entity.LegalReference,
            ContractReference = entity.ContractReference,
            RegisteredAtUtc = entity.RegisteredAtUtc
        };
    }

    /// <summary>
    /// Converts this document to an entity.
    /// </summary>
    /// <returns>A new entity instance.</returns>
    public LawfulBasisRegistrationEntity ToEntity() => new()
    {
        Id = Id,
        RequestTypeName = RequestTypeName,
        BasisValue = BasisValue,
        Purpose = Purpose,
        LIAReference = LIAReference,
        LegalReference = LegalReference,
        ContractReference = ContractReference,
        RegisteredAtUtc = RegisteredAtUtc
    };
}
