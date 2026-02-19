namespace Encina.Secrets;

/// <summary>
/// Options for creating or updating a secret.
/// </summary>
/// <param name="ExpiresAtUtc">The UTC expiration time for the secret, or <c>null</c> for no expiration.</param>
/// <param name="Tags">Optional key-value tags to associate with the secret.</param>
public record SecretOptions(DateTime? ExpiresAtUtc = null, IDictionary<string, string>? Tags = null);
