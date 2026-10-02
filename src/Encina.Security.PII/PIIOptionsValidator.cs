using Encina.Security.PII.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Security.PII;

/// <summary>
/// Validates <see cref="PIIOptions"/> so that <see cref="MaskingMode.Hash"/> fails closed:
/// it needs a <see cref="PIIOptions.HashKey"/> unless <see cref="PIIOptions.AllowUnkeyedHash"/>
/// is set explicitly.
/// </summary>
internal sealed class PIIOptionsValidator : IValidateOptions<PIIOptions>
{
    private readonly ILogger _logger;
    private int _warned;

    /// <summary>
    /// Initializes a new instance of the <see cref="PIIOptionsValidator"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger that receives the unkeyed-hash warning; <c>null</c> (no logging registered) discards it.
    /// </param>
    public PIIOptionsValidator(ILogger<PIIOptionsValidator>? logger = null)
    {
        _logger = logger ?? NullLogger<PIIOptionsValidator>.Instance;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PIIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failure = FindFailure(options);
        if (failure is not null)
        {
            return ValidateOptionsResult.Fail(failure);
        }

        // The validator is a singleton and runs once per options cache (startup validation and
        // IOptions resolution), so the flag keeps the warning to one line per process.
        if (options.HashKey is null && options.AllowUnkeyedHash && Interlocked.Exchange(ref _warned, 1) == 0)
        {
            PIILogMessages.UnkeyedHashAllowed(_logger);
        }

        return ValidateOptionsResult.Success;
    }

    private static string? FindFailure(PIIOptions options)
    {
        if (HasBlankKey(options))
        {
            return "PIIOptions.HashKey must not be empty or whitespace; set a real key or leave it null.";
        }

        if (RequiresMissingKey(options))
        {
            return "MaskingMode.Hash requires PIIOptions.HashKey; set a key or opt out explicitly with PIIOptions.AllowUnkeyedHash.";
        }

        return null;
    }

    private static bool HasBlankKey(PIIOptions options) =>
        options.HashKey is not null && string.IsNullOrWhiteSpace(options.HashKey);

    private static bool RequiresMissingKey(PIIOptions options) =>
        options.HashKey is null && options.DefaultMode == MaskingMode.Hash && !options.AllowUnkeyedHash;
}
