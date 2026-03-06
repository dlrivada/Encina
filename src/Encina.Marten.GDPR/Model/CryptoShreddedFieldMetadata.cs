using Encina.Compliance.DataSubjectRights;

namespace Encina.Marten.GDPR;

/// <summary>
/// Metadata describing a property that participates in crypto-shredding.
/// </summary>
/// <remarks>
/// <para>
/// This record is produced by the property cache during startup scanning and contains
/// the resolved information needed to encrypt/decrypt a specific PII field on a domain event.
/// It combines the <c>[CryptoShredded]</c> attribute binding (which subject owns the data)
/// with the <c>[PersonalData]</c> classification from <see cref="PersonalDataCategory"/>.
/// </para>
/// <para>
/// The <see cref="SubjectIdProperty"/> identifies which sibling property on the event type
/// contains the data subject identifier (e.g., <c>"UserId"</c>), enabling the serializer
/// to resolve the correct encryption key at runtime.
/// </para>
/// </remarks>
public sealed record CryptoShreddedFieldMetadata
{
    /// <summary>
    /// The type that declares the crypto-shredded property.
    /// </summary>
    public required Type DeclaringType { get; init; }

    /// <summary>
    /// Name of the property that is crypto-shredded.
    /// </summary>
    /// <example>Email</example>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Name of the sibling property on the declaring type that identifies the data subject.
    /// </summary>
    /// <example>UserId</example>
    public required string SubjectIdProperty { get; init; }

    /// <summary>
    /// The personal data category as classified by the <c>[PersonalData]</c> attribute
    /// from <c>Encina.Compliance.DataSubjectRights</c>.
    /// </summary>
    public required PersonalDataCategory Category { get; init; }
}
