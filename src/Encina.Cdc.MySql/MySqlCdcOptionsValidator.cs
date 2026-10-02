using System.Collections.Concurrent;

using Encina.Cdc.MySql.Diagnostics;
using Encina.Validation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using MySqlConnector;

namespace Encina.Cdc.MySql;

/// <summary>
/// Validates <see cref="MySqlCdcOptions"/>: <see cref="MySqlCdcOptions.Hostname"/> and every server in
/// <see cref="MySqlCdcOptions.ConnectionString"/> must pass <see cref="EndpointValidator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unix socket, named pipe and shared memory connections are local endpoints and need
/// <see cref="MySqlCdcOptions.AllowLocalEndpoints"/>. Error messages never echo the connection string.
/// </para>
/// <para>
/// When <see cref="MySqlCdcOptions.AllowLocalEndpoints"/> is set, a warning is logged once per named
/// options instance.
/// </para>
/// </remarks>
internal sealed class MySqlCdcOptionsValidator : IValidateOptions<MySqlCdcOptions>
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, byte> _warnedOptionNames = new(StringComparer.Ordinal);

    public MySqlCdcOptionsValidator(ILogger<MySqlCdcOptionsValidator>? logger = null)
    {
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MySqlCdcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var policy = new EndpointPolicy { AllowedSchemes = [], AllowLocalEndpoints = options.AllowLocalEndpoints };
        var error = EndpointValidator.ValidateHost(options.Hostname, nameof(options.Hostname), policy)
            ?? ValidateConnectionString(options.ConnectionString, policy);
        if (error is not null)
        {
            return ValidateOptionsResult.Fail($"MySqlCdcOptions.{error}");
        }

        var optionsName = name ?? Options.DefaultName;
        if (options.AllowLocalEndpoints && _warnedOptionNames.TryAdd(optionsName, 0))
        {
            MySqlCdcLog.EndpointValidationRelaxed(_logger, optionsName);
        }

        return ValidateOptionsResult.Success;
    }

    private static string? ValidateConnectionString(string? connectionString, EndpointPolicy policy)
    {
        const string property = nameof(MySqlCdcOptions.ConnectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"{property} must be configured.";
        }

        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            return $"{property} is not a valid MySQL connection string.";
        }

        return builder.ConnectionProtocol == MySqlConnectionProtocol.Sockets
            ? ValidateServers(builder.Server, property, policy)
            : ValidateLocalTransport(property, policy);
    }

    private static string? ValidateServers(string? servers, string property, EndpointPolicy policy)
    {
        // MySqlConnector connects to localhost when Server is empty.
        string[] hosts = string.IsNullOrWhiteSpace(servers) ? ["localhost"] : servers.Split(',', StringSplitOptions.TrimEntries);
        foreach (var host in hosts)
        {
            // MySqlConnector connects through a Unix socket when Server is an absolute path.
            var error = host.StartsWith('/')
                ? ValidateLocalTransport(property, policy)
                : EndpointValidator.ValidateHost(host, property, policy);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static string? ValidateLocalTransport(string property, EndpointPolicy policy) =>
        policy.AllowLocalEndpoints
            ? null
            : $"{property} uses a local transport (Unix socket, named pipe or shared memory). Set AllowLocalEndpoints = true to allow local endpoints (development/testing only).";
}
