namespace Encina.Aspire.Testing;

/// <summary>
/// Configuration options for Encina test support in Aspire integration tests.
/// </summary>
public sealed class EncinaTestSupportOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to clear the outbox before each test.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ClearOutboxBeforeTest { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to clear the inbox before each test.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ClearInboxBeforeTest { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to reset sagas before each test.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ResetSagasBeforeTest { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to clear scheduled messages before each test.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ClearScheduledMessagesBeforeTest { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to clear dead letter messages before each test.
    /// Default is <c>true</c>.
    /// </summary>
    public bool ClearDeadLetterBeforeTest { get; set; } = true;

    /// <summary>
    /// Gets or sets the default timeout for wait operations.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan DefaultWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the polling interval for wait operations.
    /// Default is 100 milliseconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);
}
