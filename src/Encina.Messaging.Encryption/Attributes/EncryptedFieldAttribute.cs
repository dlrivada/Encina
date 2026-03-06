namespace Encina.Messaging.Encryption.Attributes;

/// <summary>
/// Marks a property within a message type for selective field-level encryption within
/// the message encryption pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This attribute serves as a marker for selective field encryption within messages that
/// use payload-level encryption. When a message type is decorated with
/// <see cref="EncryptedMessageAttribute"/>, individual properties marked with this attribute
/// can receive additional field-level encryption treatment.
/// </para>
/// <para>
/// This attribute delegates to the existing <see cref="Security.Encryption.EncryptAttribute"/>
/// infrastructure for the actual cryptographic operations, providing a unified encryption
/// surface for message-oriented scenarios.
/// </para>
/// <para>
/// <strong>Note</strong>: For most use cases, payload-level encryption via
/// <see cref="EncryptedMessageAttribute"/> provides sufficient protection. Use this attribute
/// only when individual fields require different encryption keys or additional isolation
/// within an already-encrypted message.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EncryptedMessage]
/// public sealed record SensitiveNotification(
///     Guid Id,
///     [property: EncryptedField] string PersonalData,
///     [property: EncryptedField] string FinancialInfo,
///     string PublicMetadata
/// ) : INotification;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncryptedFieldAttribute : Attribute;
