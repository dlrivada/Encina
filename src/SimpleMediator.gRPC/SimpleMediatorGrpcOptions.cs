namespace SimpleMediator.gRPC;

/// <summary>
/// Configuration options for SimpleMediator gRPC integration.
/// </summary>
public sealed class SimpleMediatorGrpcOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use reflection for service discovery.
    /// </summary>
    public bool EnableReflection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum receive message size in bytes.
    /// </summary>
    public int MaxReceiveMessageSize { get; set; } = 4 * 1024 * 1024; // 4 MB

    /// <summary>
    /// Gets or sets the maximum send message size in bytes.
    /// </summary>
    public int MaxSendMessageSize { get; set; } = 4 * 1024 * 1024; // 4 MB

    /// <summary>
    /// Gets or sets a value indicating whether to use interceptors for logging.
    /// </summary>
    public bool EnableLoggingInterceptor { get; set; } = true;

    /// <summary>
    /// Gets or sets the deadline for unary calls.
    /// </summary>
    public TimeSpan DefaultDeadline { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to compress responses.
    /// </summary>
    public bool EnableCompression { get; set; }
}
