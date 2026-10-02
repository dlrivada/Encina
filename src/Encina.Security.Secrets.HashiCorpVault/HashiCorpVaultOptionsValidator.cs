using System.Collections.Concurrent;

using Encina.Validation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Security.Secrets.HashiCorpVault;

/// <summary>
/// Validates <see cref="HashiCorpVaultOptions"/>: the Vault address must be an absolute
/// <c>https</c> URL that passes <see cref="EndpointValidator"/>, and an authentication method is required.
/// </summary>
/// <remarks>
/// When <see cref="HashiCorpVaultOptions.AllowInsecureHttp"/> or
/// <see cref="HashiCorpVaultOptions.AllowLocalEndpoints"/> is set, a warning is logged once per
/// named options instance.
/// </remarks>
internal sealed class HashiCorpVaultOptionsValidator : IValidateOptions<HashiCorpVaultOptions>
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, byte> _warnedOptionNames = new(StringComparer.Ordinal);

    public HashiCorpVaultOptionsValidator(ILogger<HashiCorpVaultOptionsValidator>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, HashiCorpVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var policy = EndpointPolicy.ForHttps(options.AllowInsecureHttp, options.AllowLocalEndpoints);
        var error = EndpointValidator.ValidateUrl(options.VaultAddress, nameof(options.VaultAddress), policy);
        if (error is not null)
        {
            return ValidateOptionsResult.Fail($"HashiCorpVaultOptions.{error}");
        }

        if (options.AuthMethod is null)
        {
            return ValidateOptionsResult.Fail(
                "HashiCorpVaultOptions.AuthMethod is required. "
                + "Provide an IAuthMethodInfo implementation (e.g., TokenAuthMethodInfo, AppRoleAuthMethodInfo).");
        }

        WarnOnceIfOptedOut(name ?? Options.DefaultName, options);
        return ValidateOptionsResult.Success;
    }

    private void WarnOnceIfOptedOut(string name, HashiCorpVaultOptions options)
    {
        if ((options.AllowInsecureHttp || options.AllowLocalEndpoints) && _warnedOptionNames.TryAdd(name, 0))
        {
            Log.EndpointValidationRelaxed(_logger, name, options.AllowInsecureHttp, options.AllowLocalEndpoints);
        }
    }
}
