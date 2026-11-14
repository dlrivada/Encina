using Encina.Diagnostics;
using Encina.Security.Secrets.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Security.Secrets.Configuration;

/// <summary>
/// A <see cref="ConfigurationProvider"/> that loads secrets from an <see cref="ISecretReader"/>
/// into the .NET configuration system.
/// </summary>
/// <remarks>
/// <para>
/// On <see cref="Load"/>, this provider resolves <see cref="ISecretReader"/> from the DI container,
/// reads the configured secret names, and populates the <see cref="ConfigurationProvider.Data"/> dictionary.
/// </para>
/// <para>
/// <b>ROP handling:</b> Uses <c>Match()</c> to branch on <c>Either&lt;EncinaError, T&gt;</c> results.
/// Only successful (<c>Right</c>) results are added to configuration. Errors (<c>Left</c>) are logged
/// but do not prevent the application from starting.
/// </para>
/// <para>
/// <b>Key mapping:</b> Secret names are converted to configuration keys by replacing
/// the <see cref="SecretsConfigurationOptions.KeyDelimiter"/> with the standard
/// configuration section separator (<c>:</c>).
/// </para>
/// <para>
/// <b>Reload support:</b> When <see cref="SecretsConfigurationOptions.ReloadInterval"/> is set,
/// a timer created through the injected <see cref="TimeProvider"/> periodically calls <see cref="Load"/>
/// to refresh secrets. A reload publishes new data only when every secret was read: if the reader returns
/// an error (<c>Left</c>) for any of them, the previously loaded data is kept, no reload is signalled, and
/// the next interval retries. An exception during a reload never escapes the timer callback: it is logged
/// (exception type and stack trace, never its message) and the timer keeps running. When reading the
/// secrets throws, the previously loaded data is kept; when a reload listener throws, the data has already
/// been replaced.
/// </para>
/// </remarks>
internal sealed class SecretsConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SecretsConfigurationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _reloadLogger;
    private ITimer? _reloadTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsConfigurationProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="ISecretReader"/>.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="timeProvider">
    /// The time provider that drives the reload timer. Pass <c>null</c> to use <see cref="TimeProvider.System"/>.
    /// </param>
    public SecretsConfigurationProvider(
        IServiceProvider serviceProvider,
        SecretsConfigurationOptions options,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Resolved once here, so logging a failed reload does not depend on a service provider that
        // may already be disposed when the timer fires.
        _reloadLogger = (ILogger?)serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<SecretsConfigurationProvider>()
            ?? NullLogger.Instance;
    }

    /// <summary>
    /// Loads secrets from the <see cref="ISecretReader"/> into the configuration data.
    /// </summary>
    /// <remarks>
    /// This method uses synchronous blocking (<c>GetAwaiter().GetResult()</c>) because
    /// <see cref="ConfigurationProvider.Load"/> is a synchronous API invoked once during startup.
    /// This is consistent with Azure Key Vault Configuration Provider and other Encina startup patterns.
    /// </remarks>
    public override void Load()
    {
        if (ReadSecrets() is not { } secrets)
        {
            return;
        }

        Data = secrets.Data;
        SetupReloadTimer();
    }

    /// <summary>
    /// Reads the configured secrets. Returns <c>null</c> when no <see cref="ISecretReader"/> is registered;
    /// otherwise the secrets that were read, and whether every secret was read (no <c>Left</c>).
    /// </summary>
    private (Dictionary<string, string?> Data, bool Complete)? ReadSecrets()
    {
        var secretReader = _serviceProvider.GetService<ISecretReader>();

        if (secretReader is null)
        {
            LogWarning("ISecretReader is not registered. No secrets will be loaded into configuration.");
            return null;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var complete = true;

        foreach (var secretName in _options.SecretNames)
        {
            var resolvedName = _options.SecretPrefix is not null
                ? _options.SecretPrefix + secretName
                : secretName;

            var result = secretReader.GetSecretAsync(resolvedName)
                .AsTask().GetAwaiter().GetResult();

            result.Match(
                Right: value =>
                {
                    var key = MapSecretNameToConfigurationKey(resolvedName);
                    data[key] = value;
                },
                Left: error =>
                {
                    complete = false;
                    LogWarning($"Failed to load secret '{secretName}': {error.GetCode().IfNone("encina.unknown")}");
                });
        }

        return (data, complete);
    }

    /// <summary>
    /// Releases the reload timer resources.
    /// </summary>
    public void Dispose()
    {
        _reloadTimer?.Dispose();
        _reloadTimer = null;
    }

    private string MapSecretNameToConfigurationKey(string secretName)
    {
        var key = secretName;

        // Strip prefix if configured
        if (_options.SecretPrefix is not null && _options.StripPrefix &&
            key.StartsWith(_options.SecretPrefix, StringComparison.OrdinalIgnoreCase))
        {
            key = key[_options.SecretPrefix.Length..];
        }

        // Replace delimiter with configuration section separator
        if (!string.IsNullOrEmpty(_options.KeyDelimiter))
        {
            key = key.Replace(_options.KeyDelimiter, ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        }

        return key;
    }

    private void SetupReloadTimer()
    {
        if (_options.ReloadInterval is not null)
        {
            _reloadTimer?.Dispose();
            _reloadTimer = _timeProvider.CreateTimer(
                _ => ReloadFromTimer(),
                state: null,
                dueTime: _options.ReloadInterval.Value,
                period: _options.ReloadInterval.Value);
        }
    }

    /// <summary>
    /// The reload timer callback. It never throws: an exception escaping a timer callback on the system
    /// clock terminates the process. A reload that could not read every secret, or whose read throws,
    /// leaves <see cref="ConfigurationProvider.Data"/> unchanged and signals nothing, so readers never see
    /// a configuration with secrets missing; the timer keeps running so the next interval retries.
    /// </summary>
    private void ReloadFromTimer()
    {
        try
        {
            if (ReadSecrets() is not { Complete: true } secrets)
            {
                return;
            }

            Data = secrets.Data;
            OnReload();
        }
        catch (Exception ex)
        {
            try
            {
                Log.ConfigurationReloadFailed(_reloadLogger, ex.ForLogging());
            }
            catch (Exception)
            {
                // A throwing logging provider must not escape a system-clock timer callback either.
            }
        }
    }

    private void LogWarning(string message)
    {
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<SecretsConfigurationProvider>();
        logger?.LogWarning("{Message}", message);
    }
}
