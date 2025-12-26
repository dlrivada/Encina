namespace Encina.Marten.Projections;

/// <summary>
/// Configuration options for projections.
/// </summary>
public sealed class ProjectionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether projections are enabled.
    /// Default is false.
    /// </summary>
    /// <remarks>
    /// When enabled, projection infrastructure (repositories, dispatcher, manager)
    /// will be registered in the DI container.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use inline projections.
    /// Default is true when projections are enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Inline projections</b>: Events are projected synchronously during
    /// command execution. Read models are immediately consistent.
    /// </para>
    /// <para>
    /// <b>Async projections</b>: Events are projected in the background.
    /// Read models are eventually consistent. (Future feature)
    /// </para>
    /// </remarks>
    public bool UseInlineProjections { get; set; } = true;

    /// <summary>
    /// Gets or sets the default batch size for projection rebuilds.
    /// Default is 1000.
    /// </summary>
    public int RebuildBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically rebuild
    /// projections on startup when they are outdated. Default is false.
    /// </summary>
    /// <remarks>
    /// <b>Warning</b>: This can significantly delay application startup
    /// for large event stores.
    /// </remarks>
    public bool AutoRebuildOnStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to throw exceptions
    /// when a projection fails. Default is false (errors are logged).
    /// </summary>
    public bool ThrowOnProjectionError { get; set; }
}
