namespace Encina.Secrets;

/// <summary>
/// Represents metadata about a secret, returned after write operations.
/// </summary>
/// <param name="Name">The name of the secret.</param>
/// <param name="Version">The version identifier assigned to the secret.</param>
/// <param name="CreatedAtUtc">The UTC timestamp when the secret version was created.</param>
/// <param name="ExpiresAtUtc">The UTC expiration time of the secret, or <c>null</c> if no expiration is set.</param>
public record SecretMetadata(string Name, string Version, DateTime CreatedAtUtc, DateTime? ExpiresAtUtc);
