namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Metadata about a personal data field discovered from attribute scanning at startup.
/// </summary>
/// <remarks>
/// <para>
/// This record captures the compile-time metadata from <c>[PersonalData]</c> attributes
/// applied to entity properties. It is used to build a <c>PersonalDataMap</c> at startup,
/// avoiding runtime reflection in hot paths.
/// </para>
/// <para>
/// Each entry describes one property on one entity type, including its data category,
/// erasability, portability eligibility, and legal retention status.
/// </para>
/// </remarks>
public sealed record PersonalDataField
{
    /// <summary>
    /// The name of the property decorated with the <c>[PersonalData]</c> attribute.
    /// </summary>
    /// <example>"Email", "PhoneNumber", "SocialSecurityNumber"</example>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The category of this personal data field.
    /// </summary>
    public required PersonalDataCategory Category { get; init; }

    /// <summary>
    /// Whether this field can be erased in response to an Article 17 erasure request.
    /// </summary>
    public required bool IsErasable { get; init; }

    /// <summary>
    /// Whether this field is eligible for data portability under Article 20.
    /// </summary>
    public required bool IsPortable { get; init; }

    /// <summary>
    /// Whether this field has a legal retention requirement that prevents erasure.
    /// </summary>
    /// <remarks>
    /// Corresponds to Article 17(3) exemptions. Fields marked with legal retention
    /// are unconditionally excluded from erasure operations.
    /// </remarks>
    public required bool HasLegalRetention { get; init; }
}
