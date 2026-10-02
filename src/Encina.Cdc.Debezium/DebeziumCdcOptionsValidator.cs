using Microsoft.Extensions.Options;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Validates the listener prefix of <see cref="DebeziumCdcOptions"/>: <see cref="DebeziumCdcOptions.ListenUrl"/>
/// must be <c>http://</c> or <c>https://</c> followed by a host (or the <c>+</c>/<c>*</c> wildcards) and
/// nothing else, <see cref="DebeziumCdcOptions.ListenPort"/> must be a valid TCP port and
/// <see cref="DebeziumCdcOptions.ListenPath"/> must start with <c>/</c>.
/// </summary>
/// <remarks>
/// The listener binds an inbound prefix, so only its format is checked; loopback and wildcard hosts
/// are valid (decision 4 of #852).
/// </remarks>
internal sealed class DebeziumCdcOptionsValidator : IValidateOptions<DebeziumCdcOptions>
{
    private static readonly string[] AllowedPrefixes = ["http://", "https://"];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DebeziumCdcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!IsValidListenUrl(options.ListenUrl))
        {
            return ValidateOptionsResult.Fail(
                "DebeziumCdcOptions.ListenUrl must be 'http://' or 'https://' followed by a host name, an IP address, '+' or '*', without port or path (for example 'http://+').");
        }

        if (options.ListenPort is < 1 or > 65535)
        {
            return ValidateOptionsResult.Fail("DebeziumCdcOptions.ListenPort must be between 1 and 65535.");
        }

        return options.ListenPath is { Length: > 0 } path && path[0] == '/'
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("DebeziumCdcOptions.ListenPath must start with '/'.");
    }

    private static bool IsValidListenUrl(string? listenUrl)
    {
        var prefix = Array.Find(
            AllowedPrefixes,
            p => listenUrl?.StartsWith(p, StringComparison.OrdinalIgnoreCase) == true);
        if (prefix is null)
        {
            return false;
        }

        var host = listenUrl![prefix.Length..];
        return host is "+" or "*" || Uri.CheckHostName(host) != UriHostNameType.Unknown;
    }
}
