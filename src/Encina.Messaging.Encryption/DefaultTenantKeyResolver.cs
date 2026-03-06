using System.Globalization;
using Encina.Messaging.Encryption.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Messaging.Encryption;

/// <summary>
/// Default implementation of <see cref="ITenantKeyResolver"/> that uses a configurable
/// naming pattern to map tenant identifiers to encryption key IDs.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="MessageEncryptionOptions.TenantKeyPattern"/> to generate key IDs.
/// The default pattern is <c>"tenant-{0}-key"</c>, where <c>{0}</c> is replaced with the tenant ID.
/// </para>
/// <para>
/// Example: For tenant <c>"acme-corp"</c>, the default pattern produces key ID
/// <c>"tenant-acme-corp-key"</c>.
/// </para>
/// <para>
/// This class is thread-safe and suitable for singleton registration.
/// </para>
/// </remarks>
public sealed class DefaultTenantKeyResolver : ITenantKeyResolver
{
    private readonly IOptions<MessageEncryptionOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTenantKeyResolver"/> class.
    /// </summary>
    /// <param name="options">The message encryption options containing the tenant key pattern.</param>
    public DefaultTenantKeyResolver(IOptions<MessageEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <inheritdoc />
    public string ResolveKeyId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return string.Format(CultureInfo.InvariantCulture, _options.Value.TenantKeyPattern, tenantId);
    }
}
