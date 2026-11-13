namespace Encina.GraphQL;

/// <summary>
/// Configuration options for Encina GraphQL integration.
/// </summary>
public sealed class EncinaGraphQLOptions
{
    /// <summary>
    /// Gets or sets the GraphQL endpoint path.
    /// </summary>
    /// <remarks>No effect yet: no code reads this option.</remarks>
    public string Path { get; set; } = "/graphql";

    /// <summary>
    /// Gets or sets a value indicating whether to enable the GraphQL IDE (Nitro).
    /// </summary>
    /// <remarks>No effect yet: no code reads this option.</remarks>
    public bool EnableGraphQLIDE { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable introspection.
    /// </summary>
    /// <remarks>No effect yet: no code reads this option.</remarks>
    public bool EnableIntrospection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include exception details in errors.
    /// </summary>
    /// <remarks>No effect yet: no code reads this option.</remarks>
    public bool IncludeExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed execution depth.
    /// </summary>
    /// <remarks>No effect yet: no code reads this option, so it is not a protection.</remarks>
    public int MaxExecutionDepth { get; set; } = 15;

    /// <summary>
    /// Gets or sets the execution timeout.
    /// </summary>
    /// <remarks>Applied to queries and mutations run through the bridge.</remarks>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to enable subscriptions.
    /// </summary>
    /// <remarks>Subscriptions are not implemented yet: this option only chooses which not-implemented error is returned.</remarks>
    public bool EnableSubscriptions { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable persisted queries.
    /// </summary>
    /// <remarks>No effect yet: no code reads this option.</remarks>
    public bool EnablePersistedQueries { get; set; }
}
