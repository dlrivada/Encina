namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Represents the location of a specific piece of personal data belonging to a data subject.
/// </summary>
/// <remarks>
/// <para>
/// A personal data location identifies exactly where a data subject's personal data resides:
/// the entity type, the entity instance (by ID), and the specific field name. This information
/// is used by the data locator, erasure executor, and portability exporter to operate on
/// the correct data.
/// </para>
/// <para>
/// The <see cref="IsErasable"/>, <see cref="IsPortable"/>, and <see cref="HasLegalRetention"/>
/// flags determine which DSR operations can be performed on this data field.
/// </para>
/// </remarks>
public sealed record PersonalDataLocation
{
    /// <summary>
    /// The CLR type of the entity containing the personal data.
    /// </summary>
    /// <example><c>typeof(Customer)</c>, <c>typeof(Order)</c></example>
    public required Type EntityType { get; init; }

    /// <summary>
    /// The unique identifier of the entity instance containing the personal data.
    /// </summary>
    /// <example>"customer-123", "order-456"</example>
    public required string EntityId { get; init; }

    /// <summary>
    /// The name of the property or field containing the personal data.
    /// </summary>
    /// <example>"Email", "PhoneNumber", "DateOfBirth"</example>
    public required string FieldName { get; init; }

    /// <summary>
    /// The category of this personal data field.
    /// </summary>
    public required PersonalDataCategory Category { get; init; }

    /// <summary>
    /// Whether this field can be erased in response to an Article 17 erasure request.
    /// </summary>
    /// <remarks>
    /// Fields with legal retention requirements (e.g., tax records) should be marked
    /// as non-erasable. See <see cref="HasLegalRetention"/>.
    /// </remarks>
    public required bool IsErasable { get; init; }

    /// <summary>
    /// Whether this field is eligible for data portability under Article 20.
    /// </summary>
    /// <remarks>
    /// Only data processed by automated means and based on consent or contract
    /// qualifies for portability.
    /// </remarks>
    public required bool IsPortable { get; init; }

    /// <summary>
    /// Whether this field has a legal retention requirement that prevents erasure.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the field must be retained regardless of erasure requests.
    /// Common examples include tax records, anti-money laundering data, and audit logs
    /// required by law. Corresponds to Article 17(3) exemptions.
    /// </remarks>
    public required bool HasLegalRetention { get; init; }

    /// <summary>
    /// The current value of the personal data field.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the value has already been erased or is not available.
    /// Boxing is acceptable here since this is used for portability export and access responses,
    /// not in hot paths.
    /// </remarks>
    public object? CurrentValue { get; init; }
}
