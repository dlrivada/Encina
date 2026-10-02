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

        var error = ListenUrlError(options.ListenUrl)
            ?? ListenPortError(options.ListenPort)
            ?? ListenPathError(options.ListenPath);

        return error is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(error);
    }

    private static string? ListenUrlError(string? listenUrl) =>
        IsValidListenUrl(listenUrl)
            ? null
            : "DebeziumCdcOptions.ListenUrl must be 'http://' or 'https://' followed by a host name, an IP address, '+' or '*', without port or path (for example 'http://+').";

    private static string? ListenPortError(int listenPort) =>
        listenPort is >= 1 and <= 65535 ? null : "DebeziumCdcOptions.ListenPort must be between 1 and 65535.";

    private static string? ListenPathError(string? listenPath) =>
        listenPath is { Length: > 0 } && listenPath[0] == '/' ? null : "DebeziumCdcOptions.ListenPath must start with '/'.";

    private static bool IsValidListenUrl(string? listenUrl)
    {
        var prefix = Array.Find(
            AllowedPrefixes,
            p => listenUrl?.StartsWith(p, StringComparison.OrdinalIgnoreCase) == true);
        if (prefix is null)
        {
            return false;
        }

        return IsValidListenHost(listenUrl![prefix.Length..]);
    }

    // HttpListener needs IPv6 literals in brackets ("http://[::1]"); "http://::1" fails at Prefixes.Add.
    private static bool IsValidListenHost(string host) => host switch
    {
        "+" or "*" => true,
        ['[', .. var inner, ']'] => Uri.CheckHostName(inner) == UriHostNameType.IPv6,
        _ => Uri.CheckHostName(host) is UriHostNameType.Dns or UriHostNameType.IPv4,
    };
}
