namespace SimpleMediator.GraphQL;

/// <summary>
/// Configuration options for SimpleMediator GraphQL integration.
/// </summary>
public sealed class SimpleMediatorGraphQLOptions
{
    /// <summary>
    /// Gets or sets the GraphQL endpoint path.
    /// </summary>
    public string Path { get; set; } = "/graphql";

    /// <summary>
    /// Gets or sets a value indicating whether to enable the GraphQL IDE (Nitro).
    /// </summary>
    public bool EnableGraphQLIDE { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable introspection.
    /// </summary>
    public bool EnableIntrospection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include exception details in errors.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed execution depth.
    /// </summary>
    public int MaxExecutionDepth { get; set; } = 15;

    /// <summary>
    /// Gets or sets the execution timeout.
    /// </summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to enable subscriptions.
    /// </summary>
    public bool EnableSubscriptions { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable persisted queries.
    /// </summary>
    public bool EnablePersistedQueries { get; set; }
}
