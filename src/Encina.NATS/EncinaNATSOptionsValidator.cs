using System.Collections.Concurrent;

using Encina.Validation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.NATS;

/// <summary>
/// Validates <see cref="EncinaNATSOptions"/>: every server URL in <see cref="EncinaNATSOptions.Url"/>
/// must be absolute, use the <c>nats</c>, <c>tls</c>, <c>ws</c> or <c>wss</c> scheme and pass
/// <see cref="EndpointValidator"/>.
/// </summary>
/// <remarks>
/// When <see cref="EncinaNATSOptions.AllowLocalEndpoints"/> is set, a warning is logged once per
/// named options instance.
/// </remarks>
internal sealed class EncinaNATSOptionsValidator : IValidateOptions<EncinaNATSOptions>
{
    private static readonly string[] AllowedSchemes = ["nats", "tls", "ws", "wss"];

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, byte> _warnedOptionNames = new(StringComparer.Ordinal);

    public EncinaNATSOptionsValidator(ILogger<EncinaNATSOptionsValidator>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, EncinaNATSOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var error = GetEndpointError(options);
        if (error is not null)
        {
            return ValidateOptionsResult.Fail($"EncinaNATSOptions.{error}");
        }

        WarnOnceIfRelaxed(name ?? Options.DefaultName, options.AllowLocalEndpoints);
        return ValidateOptionsResult.Success;
    }

    private static string? GetEndpointError(EncinaNATSOptions options)
    {
        var policy = new EndpointPolicy
        {
            AllowedSchemes = AllowedSchemes,
            AllowLocalEndpoints = options.AllowLocalEndpoints,
        };

        var servers = (options.Url ?? string.Empty).Split(',', StringSplitOptions.TrimEntries);
        return servers
            .Select(server => EndpointValidator.ValidateUrl(server, nameof(options.Url), policy))
            .FirstOrDefault(error => error is not null);
    }

    private void WarnOnceIfRelaxed(string optionsName, bool allowLocalEndpoints)
    {
        if (allowLocalEndpoints && _warnedOptionNames.TryAdd(optionsName, 0))
        {
            Log.EndpointValidationRelaxed(_logger, optionsName);
        }
    }
}
