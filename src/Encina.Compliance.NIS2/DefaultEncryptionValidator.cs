using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Diagnostics;
using Encina.Security.Encryption.Abstractions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Default implementation of <see cref="IEncryptionValidator"/> that validates against
/// the configured <see cref="NIS2Options.EncryptedDataCategories"/> and
/// <see cref="NIS2Options.EncryptedEndpoints"/>.
/// </summary>
/// <remarks>
/// <para>
/// This configuration-based implementation returns <c>true</c> for data categories and
/// endpoints that are registered in <see cref="NIS2Options"/>. Applications should register
/// a custom <see cref="IEncryptionValidator"/> before calling <c>AddEncinaNIS2()</c> to
/// perform actual encryption posture checks against their infrastructure.
/// </para>
/// <para>
/// When <c>Encina.Security.Encryption</c>'s <c>IKeyProvider</c> is registered in the DI
/// container, this implementation additionally verifies that active encryption keys exist,
/// upgrading from purely declarative validation to real infrastructure verification.
/// </para>
/// </remarks>
internal sealed class DefaultEncryptionValidator : IEncryptionValidator
{
    private readonly IOptions<NIS2Options> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultEncryptionValidator> _logger;

    public DefaultEncryptionValidator(
        IOptions<NIS2Options> options,
        IServiceProvider serviceProvider,
        ILogger<DefaultEncryptionValidator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsDataEncryptedAtRestAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataCategory);

        var isEncrypted = _options.Value.EncryptedDataCategories.Contains(dataCategory);
        return ValueTask.FromResult(Right<EncinaError, bool>(isEncrypted));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> IsDataEncryptedInTransitAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var isEncrypted = _options.Value.EncryptedEndpoints.Contains(endpoint);
        return ValueTask.FromResult(Right<EncinaError, bool>(isEncrypted));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ValidateEncryptionPolicyAsync(
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;

        // Configuration-based check: at least some categories/endpoints are configured
        var hasPolicy = opts.EncryptedDataCategories.Count > 0 || opts.EncryptedEndpoints.Count > 0;

        if (!hasPolicy)
        {
            return Right<EncinaError, bool>(false);
        }

        // Infrastructure-based check: verify active encryption keys exist if IKeyProvider is available
        var keyProviderAvailable = _serviceProvider.GetService<IKeyProvider>() is not null;

        if (!keyProviderAvailable)
        {
            // No IKeyProvider registered — config-only validation is sufficient.
            // Applications should register Encina.Security.Encryption for infrastructure verification.
            _logger.EncryptionInfrastructureChecked(false);
            return Right<EncinaError, bool>(true);
        }

        // IKeyProvider IS registered — verify it actually has active keys.
        // If it's available but reports no active key, that's a real problem:
        // config says "encrypted" but infrastructure can't encrypt.
        var hasActiveKey = await VerifyEncryptionInfrastructureAsync(cancellationToken).ConfigureAwait(false);

        if (!hasActiveKey)
        {
            _logger.EncryptionInfrastructureChecked(false);
        }

        return Right<EncinaError, bool>(hasActiveKey);
    }

    /// <summary>
    /// Verifies that actual encryption infrastructure is in place by checking
    /// <c>IKeyProvider</c> from <c>Encina.Security.Encryption</c>.
    /// Uses resilience protection (pipeline or timeout) for the external call.
    /// Returns <c>true</c> if active keys exist, <c>false</c> if unavailable or no keys.
    /// </summary>
    private async ValueTask<bool> VerifyEncryptionInfrastructureAsync(CancellationToken cancellationToken)
    {
        var keyProvider = _serviceProvider.GetService<IKeyProvider>();
        if (keyProvider is null)
        {
            return false;
        }

        var timeout = _options.Value.ExternalCallTimeout;

        return await NIS2ResilienceHelper.ExecuteAsync(
            _serviceProvider,
            async ct =>
            {
                var result = await keyProvider.GetCurrentKeyIdAsync(ct).ConfigureAwait(false);

                var hasActiveKey = result.Match(
                    Right: keyId => !string.IsNullOrEmpty(keyId),
                    Left: _ => false);

                _logger.EncryptionInfrastructureChecked(hasActiveKey);
                return hasActiveKey;
            },
            fallback: false,
            timeout,
            cancellationToken).ConfigureAwait(false);
    }
}
