namespace Encina.Security.Secrets;

/// <summary>
/// A typed reference to a secret, used for DI registration and configuration.
/// </summary>
/// <remarks>
/// Use <see cref="SecretReference"/> to configure which secrets to load,
/// with optional versioning, caching, and rotation settings.
/// </remarks>
/// <example>
/// <code>
/// var reference = new SecretReference
/// {
///     Name = "database-connection-string",
///     CacheDuration = TimeSpan.FromMinutes(10),
///     AutoRotate = true,
///     RotationInterval = TimeSpan.FromHours(24)
/// };
/// </code>
/// </example>
public sealed record SecretReference
{
    /// <summary>
    /// Gets the name of the secret.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional version of the secret. When <c>null</c>, the latest version is used.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the optional cache duration override for this specific secret.
    /// When <c>null</c>, the default from <see cref="SecretsOptions.DefaultCacheDuration"/> is used.
    /// </summary>
    public TimeSpan? CacheDuration { get; init; }

    /// <summary>
    /// Gets whether this secret should be automatically rotated.
    /// </summary>
    public bool AutoRotate { get; init; }

    /// <summary>
    /// Gets the optional rotation interval for this specific secret.
    /// Only meaningful when <see cref="AutoRotate"/> is <c>true</c>.
    /// </summary>
    public TimeSpan? RotationInterval { get; init; }
}
