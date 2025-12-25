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
    public string? DefaultQueueUrl { get; set; }

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
