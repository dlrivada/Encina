using Encina.Messaging.Health;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Configuration options for Durable Functions integration with Encina.
/// </summary>
/// <remarks>
/// <para>
/// These options control how Encina integrates with Azure Durable Functions,
/// including default retry policies and saga behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaDurableFunctions(options =>
/// {
///     options.DefaultMaxRetries = 3;
///     options.DefaultFirstRetryInterval = TimeSpan.FromSeconds(5);
///     options.ContinueCompensationOnError = true;
/// });
/// </code>
/// </example>
public sealed class DurableFunctionsOptions
{
    /// <summary>
    /// Gets or sets the default maximum number of retry attempts for activities.
    /// </summary>
    /// <value>Default: 3</value>
    public int DefaultMaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the default first retry interval.
    /// </summary>
    /// <value>Default: 5 seconds</value>
    public TimeSpan DefaultFirstRetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the default backoff coefficient for retries.
    /// </summary>
    /// <value>Default: 2.0 (exponential backoff)</value>
    public double DefaultBackoffCoefficient { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the default maximum retry interval.
    /// </summary>
    /// <value>Default: 1 minute</value>
    public TimeSpan DefaultMaxRetryInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets whether to continue compensation when a compensation step fails.
    /// </summary>
    /// <value>Default: true (continue compensating remaining steps)</value>
    public bool ContinueCompensationOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets the default saga timeout.
    /// </summary>
    /// <value>Default: null (no timeout)</value>
    public TimeSpan? DefaultSagaTimeout { get; set; }

    /// <summary>
    /// Configuration options for the Durable Functions health check.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new()
    {
        Name = "encina-durable-functions",
        Tags = ["encina", "durable-functions", "ready"]
    };
}
