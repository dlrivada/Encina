namespace SimpleMediator.RabbitMQ;

/// <summary>
/// Configuration options for SimpleMediator RabbitMQ integration.
/// </summary>
public sealed class SimpleMediatorRabbitMQOptions
{
    /// <summary>
    /// Gets or sets the RabbitMQ host name.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the RabbitMQ port.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Gets or sets the RabbitMQ virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Gets or sets the RabbitMQ username.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the RabbitMQ password.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Gets or sets the default exchange name.
    /// </summary>
    public string ExchangeName { get; set; } = "simplemediator";

    /// <summary>
    /// Gets or sets a value indicating whether to use publisher confirms.
    /// </summary>
    public bool UsePublisherConfirms { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefetch count for consumers.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether messages should be durable.
    /// </summary>
    public bool Durable { get; set; } = true;
}
