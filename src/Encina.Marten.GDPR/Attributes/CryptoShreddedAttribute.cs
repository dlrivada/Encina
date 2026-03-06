namespace Encina.Marten.GDPR;

/// <summary>
/// Marks a property for crypto-shredding — field-level encryption bound to a data subject's
/// encryption key for GDPR Article 17 compliance.
/// </summary>
/// <remarks>
/// <para>
/// Properties decorated with <c>[CryptoShredded]</c> are automatically encrypted during
/// event serialization and decrypted during deserialization by the
/// <c>CryptoShredderSerializer</c>. When a data subject exercises their right to be
/// forgotten, deleting the subject's encryption key renders all crypto-shredded fields
/// permanently unreadable.
/// </para>
/// <para>
/// The <see cref="SubjectIdProperty"/> identifies which sibling property on the same
/// event type contains the data subject's unique identifier. This binding enables the
/// serializer to retrieve the correct per-subject encryption key.
/// </para>
/// <para>
/// <b>Important</b>: Properties marked with <c>[CryptoShredded]</c> MUST also have the
/// <c>[PersonalData]</c> attribute from <c>Encina.Compliance.DataSubjectRights</c>.
/// The <c>[PersonalData]</c> attribute governs <i>what</i> is personal data and how it
/// participates in Data Subject Rights workflows; <c>[CryptoShredded]</c> governs
/// <i>which subject</i> owns the data and triggers serializer-level encryption.
/// Missing <c>[PersonalData]</c> is reported as a configuration error at startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record UserEmailChangedEvent(
///     string UserId,
///     [property: PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
///     [property: CryptoShredded(SubjectIdProperty = nameof(UserId))]
///     string NewEmail,
///     DateTimeOffset OccurredAtUtc);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CryptoShreddedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the sibling property that identifies the data subject.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property name must reference a readable <c>string</c> property on the same
    /// declaring type. The value of that property at serialization time determines
    /// which per-subject encryption key is used.
    /// </para>
    /// <para>
    /// Use <c>nameof()</c> for compile-time safety:
    /// <code>
    /// [CryptoShredded(SubjectIdProperty = nameof(UserId))]
    /// </code>
    /// </para>
    /// </remarks>
    public required string SubjectIdProperty { get; set; }
}
