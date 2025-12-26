using LanguageExt;

namespace Encina.Marten.Projections;

/// <summary>
/// Manages projection lifecycle and provides rebuild capabilities.
/// </summary>
/// <remarks>
/// <para>
/// The projection manager handles:
/// <list type="bullet">
/// <item><description>Starting and stopping projections</description></item>
/// <item><description>Rebuilding projections from scratch</description></item>
/// <item><description>Monitoring projection status and health</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Rebuilding Projections</b>: When the read model schema changes or a bug
/// is fixed in the projection logic, you may need to rebuild the projection.
/// This replays all events from the beginning to recreate the read model.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Rebuild a projection after deploying a fix
/// var result = await projectionManager.RebuildAsync&lt;OrderSummary&gt;(
///     cancellationToken);
///
/// result.Match(
///     Right: count =&gt; Console.WriteLine($"Rebuilt {count} read models"),
///     Left: error =&gt; Console.WriteLine($"Rebuild failed: {error.Message}"));
/// </code>
/// </example>
public interface IProjectionManager
{
    /// <summary>
    /// Rebuilds a projection from scratch.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type to rebuild.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of events processed during rebuild.</returns>
    /// <remarks>
    /// <para>
    /// This operation:
    /// <list type="number">
    /// <item><description>Deletes all existing read models of the type</description></item>
    /// <item><description>Replays all events from the beginning</description></item>
    /// <item><description>Applies each event to the projection</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Warning</b>: This can be a long-running operation for large event stores.
    /// Consider using <see cref="RebuildAsync{TReadModel}(RebuildOptions, CancellationToken)"/>
    /// with progress reporting for better visibility.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, long>> RebuildAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;

    /// <summary>
    /// Rebuilds a projection with advanced options.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type to rebuild.</typeparam>
    /// <param name="options">Rebuild options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of events processed during rebuild.</returns>
    Task<Either<EncinaError, long>> RebuildAsync<TReadModel>(
        RebuildOptions options,
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;

    /// <summary>
    /// Gets the current status of a projection.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projection status.</returns>
    Task<Either<EncinaError, ProjectionStatus>> GetStatusAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;

    /// <summary>
    /// Gets the status of all registered projections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of projection names to their status.</returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, ProjectionStatus>>> GetAllStatusesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a projection if it's not already running.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success.</returns>
    Task<Either<EncinaError, Unit>> StartAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;

    /// <summary>
    /// Stops a running projection.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success.</returns>
    Task<Either<EncinaError, Unit>> StopAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;

    /// <summary>
    /// Pauses a running projection.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success.</returns>
    Task<Either<EncinaError, Unit>> PauseAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;

    /// <summary>
    /// Resumes a paused projection.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success.</returns>
    Task<Either<EncinaError, Unit>> ResumeAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : class, IReadModel;
}

/// <summary>
/// Options for rebuilding projections.
/// </summary>
public sealed class RebuildOptions
{
    /// <summary>
    /// Gets or sets the batch size for processing events.
    /// </summary>
    /// <remarks>
    /// Larger batch sizes can improve performance but use more memory.
    /// Default is 1000.
    /// </remarks>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to delete existing read models before rebuild.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>. Set to <c>false</c> to update existing
    /// read models without deleting them first.
    /// </remarks>
    public bool DeleteExisting { get; set; } = true;

    /// <summary>
    /// Gets or sets the progress callback invoked periodically during rebuild.
    /// </summary>
    /// <remarks>
    /// The callback receives the current progress (0-100) and events processed.
    /// </remarks>
    public Action<int, long>? OnProgress { get; set; }

    /// <summary>
    /// Gets or sets the starting position for the rebuild.
    /// </summary>
    /// <remarks>
    /// Default is 0 (beginning). Use this to resume a failed rebuild.
    /// </remarks>
    public long StartPosition { get; set; }

    /// <summary>
    /// Gets or sets the ending position for the rebuild.
    /// </summary>
    /// <remarks>
    /// Default is null (process all events). Use this to rebuild
    /// up to a specific point in time.
    /// </remarks>
    public long? EndPosition { get; set; }

    /// <summary>
    /// Gets or sets whether to run the rebuild inline or in background.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c> (inline). Set to <c>true</c> to start
    /// the rebuild and return immediately.
    /// </remarks>
    public bool RunInBackground { get; set; }
}
