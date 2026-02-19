namespace Encina.Secrets;

/// <summary>
/// Configuration options for the <see cref="CachedSecretProvider"/> decorator.
/// </summary>
/// <remarks>
/// <para>
/// Controls caching behavior for secret read operations. Write operations
/// (<see cref="ISecretProvider.SetSecretAsync"/> and <see cref="ISecretProvider.DeleteSecretAsync"/>)
/// always invalidate the corresponding cache entries.
/// </para>
/// <para>
/// Only read operations are cached:
/// <see cref="ISecretProvider.GetSecretAsync"/>,
/// <see cref="ISecretProvider.GetSecretVersionAsync"/>, and
/// <see cref="ISecretProvider.ExistsAsync"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSecretsCaching(options =>
/// {
///     options.DefaultTtl = TimeSpan.FromMinutes(10);
///     options.Enabled = true;
/// });
/// </code>
/// </example>
public sealed class SecretCacheOptions
{
    /// <summary>
    /// Gets or sets the default time-to-live for cached secrets.
    /// </summary>
    /// <remarks>
    /// Default is 5 minutes.
    /// </remarks>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    /// <remarks>
    /// When <c>false</c>, the <see cref="CachedSecretProvider"/> passes all calls
    /// directly to the inner provider without caching.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool Enabled { get; set; } = true;
}
