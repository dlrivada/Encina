namespace SimpleMediator.Wolverine;

/// <summary>
/// Configuration options for SimpleMediator Wolverine integration.
/// </summary>
public sealed class SimpleMediatorWolverineOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically publish domain events
    /// after successful command handling.
    /// </summary>
    public bool AutoPublishDomainEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use Wolverine's built-in
    /// outbox pattern for reliable messaging.
    /// </summary>
    public bool UseOutbox { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include exception details
    /// in error responses.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the default timeout for message processing.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
