using Encina.Messaging.Health;

namespace Encina.AmazonSQS;

/// <summary>
/// Configuration options for Encina Amazon SQS/SNS integration.
/// </summary>
public sealed class EncinaAmazonSQSOptions
{
    /// <summary>
    /// Gets or sets the AWS region.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Gets or sets the default queue URL for commands.
    /// </summary>
    /// <remarks>
    /// Optional. When set, it must be an absolute <c>https</c> URL that does not target a loopback,
    /// link-local, cloud metadata or unspecified address; <see cref="AllowInsecureHttp"/> and
    /// <see cref="AllowLocalEndpoints"/> relax the scheme and loopback rules for local emulators.
    /// </remarks>
    public string? DefaultQueueUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DefaultQueueUrl"/> may use plain <c>http</c>.
    /// </summary>
    /// <value>
    /// Defaults to <c>false</c>. Set to <c>true</c> only for a local emulator; a warning is logged at
    /// startup when it is set.
    /// </value>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DefaultQueueUrl"/> may target <c>localhost</c>
    /// or a loopback address.
    /// </summary>
    /// <value>
    /// Defaults to <c>false</c>. Set to <c>true</c> only for a local emulator; a warning is logged at
    /// startup when it is set.
    /// </value>
    public bool AllowLocalEndpoints { get; set; }

    /// <summary>
    /// Gets or sets the default topic ARN for events.
    /// </summary>
    public string? DefaultTopicArn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use FIFO queues.
    /// </summary>
    public bool UseFifoQueues { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages to receive per poll.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Gets or sets the visibility timeout in seconds.
    /// </summary>
    public int VisibilityTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the wait time in seconds for long polling.
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets a value indicating whether to use content-based deduplication.
    /// </summary>
    public bool UseContentBasedDeduplication { get; set; }

    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();
}
