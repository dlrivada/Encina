using System.Collections.Concurrent;

using Encina.Validation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.AmazonSQS;

/// <summary>
/// Validates <see cref="EncinaAmazonSQSOptions"/>: when <see cref="EncinaAmazonSQSOptions.DefaultQueueUrl"/>
/// is set, it must be an absolute <c>https</c> URL that passes <see cref="EndpointValidator"/>.
/// </summary>
/// <remarks>
/// When <see cref="EncinaAmazonSQSOptions.AllowInsecureHttp"/> or
/// <see cref="EncinaAmazonSQSOptions.AllowLocalEndpoints"/> is set, a warning is logged once per
/// named options instance.
/// </remarks>
internal sealed class EncinaAmazonSQSOptionsValidator : IValidateOptions<EncinaAmazonSQSOptions>
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, byte> _warnedOptionNames = new(StringComparer.Ordinal);

    public EncinaAmazonSQSOptionsValidator(ILogger<EncinaAmazonSQSOptionsValidator>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, EncinaAmazonSQSOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var error = GetEndpointError(options);
        if (error is not null)
        {
            return ValidateOptionsResult.Fail($"EncinaAmazonSQSOptions.{error}");
        }

        WarnOnceIfRelaxed(name ?? Options.DefaultName, options);
        return ValidateOptionsResult.Success;
    }

    private static string? GetEndpointError(EncinaAmazonSQSOptions options) =>
        options.DefaultQueueUrl is null
            ? null
            : EndpointValidator.ValidateUrl(
                options.DefaultQueueUrl,
                nameof(options.DefaultQueueUrl),
                EndpointPolicy.ForHttps(options.AllowInsecureHttp, options.AllowLocalEndpoints));

    private void WarnOnceIfRelaxed(string optionsName, EncinaAmazonSQSOptions options)
    {
        var relaxed = options.AllowInsecureHttp || options.AllowLocalEndpoints;
        if (relaxed && _warnedOptionNames.TryAdd(optionsName, 0))
        {
            Log.EndpointValidationRelaxed(_logger, optionsName, options.AllowInsecureHttp, options.AllowLocalEndpoints);
        }
    }
}
