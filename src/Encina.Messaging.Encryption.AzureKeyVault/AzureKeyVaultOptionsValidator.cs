using System.Collections.Concurrent;

using Encina.Validation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Validates <see cref="AzureKeyVaultOptions"/>: the vault URI must be an absolute <c>https</c> URI
/// that passes <see cref="EndpointValidator"/>.
/// </summary>
/// <remarks>
/// When <see cref="AzureKeyVaultOptions.AllowInsecureHttp"/> or
/// <see cref="AzureKeyVaultOptions.AllowLocalEndpoints"/> is set, a warning is logged once per
/// named options instance.
/// </remarks>
internal sealed partial class AzureKeyVaultOptionsValidator : IValidateOptions<AzureKeyVaultOptions>
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, byte> _warnedOptionNames = new(StringComparer.Ordinal);

    public AzureKeyVaultOptionsValidator(ILogger<AzureKeyVaultOptionsValidator>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AzureKeyVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var policy = EndpointPolicy.ForHttps(options.AllowInsecureHttp, options.AllowLocalEndpoints);
        var error = EndpointValidator.ValidateUri(options.VaultUri, nameof(options.VaultUri), policy);
        if (error is not null)
        {
            return ValidateOptionsResult.Fail($"AzureKeyVaultOptions.{error}");
        }

        var optionsName = name ?? Options.DefaultName;
        if ((options.AllowInsecureHttp || options.AllowLocalEndpoints) && _warnedOptionNames.TryAdd(optionsName, 0))
        {
            EndpointValidationRelaxed(_logger, optionsName, options.AllowInsecureHttp, options.AllowLocalEndpoints);
        }

        return ValidateOptionsResult.Success;
    }

    /// <summary>Event ID 2494 (see EventIdRanges.MessagingEncryption).</summary>
    [LoggerMessage(EventId = 2494, Level = LogLevel.Warning,
        Message = "AzureKeyVaultOptions '{OptionsName}' relaxes endpoint validation (AllowInsecureHttp={AllowInsecureHttp}, AllowLocalEndpoints={AllowLocalEndpoints}); use only for local development")]
    private static partial void EndpointValidationRelaxed(ILogger logger, string optionsName, bool allowInsecureHttp, bool allowLocalEndpoints);
}
