using System.Text;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Context object shared across broker load test scenarios.
/// Provides thread-safe topic/queue naming and message generation helpers.
/// </summary>
/// <param name="ProviderFactory">The broker provider factory.</param>
/// <param name="ProviderName">The name of the broker provider being tested.</param>
public sealed record BrokerScenarioContext(
    IBrokerProviderFactory ProviderFactory,
    string ProviderName)
{
    private long _messageSequence;
    private long _topicSequence;
    private static readonly Random _random = new();

    /// <summary>
    /// Gets the provider options.
    /// </summary>
    public BrokerProviderOptions Options => ProviderFactory.Options;

    /// <summary>
    /// Generates a unique message ID.
    /// Thread-safe.
    /// </summary>
    /// <returns>A unique message ID.</returns>
    public long NextMessageId()
    {
        return Interlocked.Increment(ref _messageSequence);
    }

    /// <summary>
    /// Generates a unique topic/queue name.
    /// </summary>
    /// <returns>A unique topic/queue name.</returns>
    public string NextTopicName()
    {
        var sequence = Interlocked.Increment(ref _topicSequence);
        return $"{Options.TopicPrefix}-topic-{sequence}";
    }

    /// <summary>
    /// Gets a deterministic topic name for a specific bucket (shared resource).
    /// </summary>
    /// <param name="bucketId">The bucket identifier (0-based).</param>
    /// <returns>A topic name for the specified bucket.</returns>
    public string GetBucketTopic(int bucketId)
    {
        return $"{Options.TopicPrefix}-bucket-{bucketId}";
    }

    /// <summary>
    /// Creates a test message of the configured size.
    /// </summary>
    /// <returns>Test message bytes.</returns>
    public byte[] CreateTestMessage()
    {
        return CreateTestMessage(Options.MessageSizeBytes);
    }

    /// <summary>
    /// Creates a test message of the specified size.
    /// </summary>
    /// <param name="sizeBytes">The size in bytes.</param>
    /// <returns>Test message bytes.</returns>
    public byte[] CreateTestMessage(int sizeBytes)
    {
        var messageId = NextMessageId();
        var header = Encoding.UTF8.GetBytes($"msg:{messageId}:");
        var payload = new byte[sizeBytes];

        // Fill with deterministic pattern
        for (var i = 0; i < sizeBytes; i++)
        {
            payload[i] = (byte)((i + messageId) % 256);
        }

        // Prepend header if there's room
        if (sizeBytes >= header.Length)
        {
            Array.Copy(header, payload, Math.Min(header.Length, sizeBytes));
        }

        return payload;
    }

    /// <summary>
    /// Creates a test message as a string.
    /// </summary>
    /// <returns>Test message string.</returns>
    public string CreateTestMessageString()
    {
        return CreateTestMessageString(Options.MessageSizeBytes);
    }

    /// <summary>
    /// Creates a test message as a string.
    /// </summary>
    /// <param name="sizeBytes">The approximate size in bytes.</param>
    /// <returns>Test message string.</returns>
    public string CreateTestMessageString(int sizeBytes)
    {
        var messageId = NextMessageId();
        var header = $"msg:{messageId}:";
        var remaining = Math.Max(0, sizeBytes - header.Length);

        var chars = new char[remaining];
        for (var i = 0; i < remaining; i++)
        {
            chars[i] = (char)('A' + (i % 26));
        }

        return header + new string(chars);
    }

    /// <summary>
    /// Creates a batch of test messages.
    /// </summary>
    /// <returns>An array of test messages.</returns>
    public byte[][] CreateMessageBatch()
    {
        return CreateMessageBatch(Options.BatchSize);
    }

    /// <summary>
    /// Creates a batch of test messages.
    /// </summary>
    /// <param name="batchSize">The number of messages in the batch.</param>
    /// <returns>An array of test messages.</returns>
    public byte[][] CreateMessageBatch(int batchSize)
    {
        var messages = new byte[batchSize][];
        for (var i = 0; i < batchSize; i++)
        {
            messages[i] = CreateTestMessage();
        }

        return messages;
    }

    /// <summary>
    /// Gets the current message sequence number (for metrics/logging).
    /// </summary>
    public long CurrentMessageSequence => Interlocked.Read(ref _messageSequence);

    /// <summary>
    /// Gets the current topic sequence number (for metrics/logging).
    /// </summary>
    public long CurrentTopicSequence => Interlocked.Read(ref _topicSequence);
}
