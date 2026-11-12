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
/// to refresh secrets. A failed reload never escapes the timer callback: it is logged (exception type
/// and stack trace, never its message), the previously loaded data is kept and the timer keeps running.
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
        var secretReader = _serviceProvider.GetService<ISecretReader>();

        if (secretReader is null)
        {
            LogWarning("ISecretReader is not registered. No secrets will be loaded into configuration.");
            return;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var count = 0;

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
                    count++;
                },
                Left: error =>
                {
                    LogWarning($"Failed to load secret '{secretName}': {error.GetCode().IfNone("encina.unknown")}");
                });
        }

        Data = data;
        SetupReloadTimer();
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
    /// clock terminates the process. A failed <see cref="Load"/> leaves <see cref="ConfigurationProvider.Data"/>
    /// unchanged, and the timer keeps running so the next interval retries.
    /// </summary>
    private void ReloadFromTimer()
    {
        try
        {
            Load();
            OnReload();
        }
        catch (Exception ex)
        {
            Log.ConfigurationReloadFailed(_reloadLogger, ex.ForLogging());
        }
    }

    private void LogWarning(string message)
    {
        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<SecretsConfigurationProvider>();
        logger?.LogWarning("{Message}", message);
    }
}
