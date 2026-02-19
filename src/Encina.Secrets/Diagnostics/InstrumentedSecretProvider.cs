using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Secrets.Diagnostics;

/// <summary>
/// A decorator that adds OpenTelemetry tracing and metrics to any <see cref="ISecretProvider"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps each secret operation with distributed tracing (<see cref="Activity"/>) and metrics
/// (counters, histograms) instrumentation.
/// </para>
/// <para>
/// <b>ROP-aware instrumentation:</b> Uses <c>Match()</c> on <c>Either&lt;EncinaError, T&gt;</c> results
/// to record success or failure without modifying the result. <c>Right</c> results increment success
/// counters; <c>Left</c> results set error status on the activity, increment error counters, and
/// emit structured warning logs with the error code.
/// </para>
/// <para>
/// The original <c>Either</c> result is always propagated unchanged to the caller.
/// </para>
/// <para>
/// <b>Security:</b> Secret names are only recorded in telemetry when
/// <see cref="SecretsInstrumentationOptions.RecordSecretNames"/> is <c>true</c>.
/// Secret values are <b>never</b> recorded.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI (recommended)
/// services.AddSingleton&lt;ISecretProvider, MyVaultProvider&gt;();
/// services.AddEncinaSecretsInstrumentation(options =>
/// {
///     options.RecordSecretNames = true;
///     options.EnableTracing = true;
///     options.EnableMetrics = true;
/// });
/// </code>
/// </example>
internal sealed class InstrumentedSecretProvider : ISecretProvider
{
    private readonly ISecretProvider _inner;
    private readonly SecretsInstrumentationOptions _options;
    private readonly SecretsMetrics? _metrics;
    private readonly ILogger<InstrumentedSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedSecretProvider"/> class.
    /// </summary>
    /// <param name="inner">The inner secret provider to instrument.</param>
    /// <param name="options">The instrumentation options.</param>
    /// <param name="metrics">The metrics instance, or <c>null</c> if metrics are disabled.</param>
    /// <param name="logger">The logger instance.</param>
    public InstrumentedSecretProvider(
        ISecretProvider inner,
        SecretsInstrumentationOptions options,
        SecretsMetrics? metrics,
        ILogger<InstrumentedSecretProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _options = options;
        _metrics = metrics;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await InstrumentAsync(
            "get",
            name,
            () => _inner.GetSecretAsync(name, cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(
        string name,
        string version,
        CancellationToken cancellationToken = default)
    {
        return await InstrumentAsync(
            "get_version",
            name,
            () => _inner.GetSecretVersionAsync(name, version, cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(
        string name,
        string value,
        SecretOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await InstrumentAsync(
            "set",
            name,
            () => _inner.SetSecretAsync(name, value, options, cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await InstrumentAsync(
            "delete",
            name,
            () => _inner.DeleteSecretAsync(name, cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(
        CancellationToken cancellationToken = default)
    {
        return await InstrumentAsync<IEnumerable<string>>(
            "list",
            secretName: null,
            () => _inner.ListSecretsAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await InstrumentAsync(
            "exists",
            name,
            () => _inner.ExistsAsync(name, cancellationToken));
    }

    private async ValueTask<Either<EncinaError, T>> InstrumentAsync<T>(
        string operation,
        string? secretName,
        Func<ValueTask<Either<EncinaError, T>>> execute)
    {
        var sanitizedName = _options.RecordSecretNames ? secretName : null;

        using var activity = _options.EnableTracing
            ? SecretsActivitySource.StartOperation(operation, sanitizedName)
            : null;

        var stopwatch = Stopwatch.GetTimestamp();

        var result = await execute();

        var elapsedMs = Stopwatch.GetElapsedTime(stopwatch).TotalMilliseconds;

        result.Match(
            Right: _ =>
            {
                SecretsActivitySource.SetSuccess(activity);

                if (_options.EnableMetrics)
                {
                    _metrics?.RecordSuccess(operation, elapsedMs, sanitizedName);
                }
            },
            Left: error =>
            {
                var errorCode = error.GetCode().IfNone("unknown");

                SecretsActivitySource.SetError(activity, errorCode, error.Message);

                if (_options.EnableMetrics)
                {
                    _metrics?.RecordError(operation, elapsedMs, errorCode, sanitizedName);
                }

                _logger.LogWarning(
                    "Secret operation '{Operation}' failed with code '{ErrorCode}': {ErrorMessage}",
                    operation,
                    errorCode,
                    error.Message);
            });

        return result;
    }
}
