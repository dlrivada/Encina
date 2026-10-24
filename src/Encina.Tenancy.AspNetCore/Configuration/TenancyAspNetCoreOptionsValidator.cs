using Microsoft.Extensions.Options;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Fails startup when the tenant header is enabled without an explicit trust decision.
/// </summary>
/// <remarks>
/// A tenant header is chosen by the client. Enabling <see cref="HeaderResolverOptions.Enabled"/>
/// requires either <see cref="HeaderResolverOptions.TrustedProxies"/> or the explicit opt-out
/// <see cref="HeaderResolverOptions.TrustAnyPeer"/>.
/// </remarks>
internal sealed class TenancyAspNetCoreOptionsValidator : IValidateOptions<TenancyAspNetCoreOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TenancyAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var header = options.HeaderResolver;

        if (header.Enabled && header.TrustedProxies.Count == 0 && !header.TrustAnyPeer)
        {
            return ValidateOptionsResult.Fail(
                "HeaderResolver.Enabled is true but neither HeaderResolver.TrustedProxies nor HeaderResolver.TrustAnyPeer is set: " +
                "a tenant header is chosen by the client. List the gateways that set it in HeaderResolver.TrustedProxies, " +
                "or set HeaderResolver.TrustAnyPeer to true only behind network isolation.");
        }

        return ValidateOptionsResult.Success;
    }
}
