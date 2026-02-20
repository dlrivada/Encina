namespace Encina.Security.Encryption;

/// <summary>
/// Marks a response class to indicate that its encrypted properties should be
/// encrypted before returning from the pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to response DTOs or query results that contain properties
/// marked with <see cref="EncryptAttribute"/>. The encryption pipeline behavior
/// will encrypt marked properties after the handler produces the response.
/// </para>
/// <para>
/// Individual properties within the response must still be marked with
/// <see cref="EncryptAttribute"/> to specify which fields require encryption.
/// This class-level attribute acts as an opt-in signal for the pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EncryptedResponse]
/// public sealed record UserProfileDto
/// {
///     public Guid Id { get; init; }
///     public string Username { get; init; }
///
///     [Encrypt(Purpose = "User.Email")]
///     public string Email { get; init; }
///
///     [Encrypt(Purpose = "User.Phone")]
///     public string PhoneNumber { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class EncryptedResponseAttribute : EncryptionAttribute
{
}
