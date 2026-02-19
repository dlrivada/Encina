namespace Encina.Secrets;

/// <summary>
/// Represents a secret retrieved from a secret provider.
/// </summary>
/// <param name="Name">The name of the secret.</param>
/// <param name="Value">The secret value.</param>
/// <param name="Version">The version identifier of the secret, or <c>null</c> if versioning is not supported.</param>
/// <param name="ExpiresAtUtc">The UTC expiration time of the secret, or <c>null</c> if no expiration is set.</param>
public record Secret(string Name, string Value, string? Version, DateTime? ExpiresAtUtc);
