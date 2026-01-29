namespace Encina.Messaging.DomainEvents;

/// <summary>
/// Configuration options for domain event dispatching.
/// </summary>
/// <remarks>
/// <para>
/// These options control how domain events are collected from entities and published
/// after persistence operations complete. The actual dispatching is handled by
/// provider-specific implementations (e.g., EF Core's SaveChanges interceptor).
/// </para>
/// <para>
/// <b>Design Philosophy</b>: Domain events are raised during aggregate operations but
/// should only be dispatched after persistence succeeds. This ensures that events
/// are only published for changes that were actually committed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseDomainEvents = true;
///     config.DomainEventsOptions.StopOnFirstError = true;
///     config.DomainEventsOptions.RequireINotification = false;
/// });
/// </code>
/// </example>
public sealed class DomainEventsOptions
{
    /// <summary>
    /// Gets or sets whether the domain event dispatcher is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, domain events are not automatically dispatched
    /// after persistence operations. Events will remain in the entities until manually cleared.
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to stop dispatching events when the first error occurs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, if publishing a domain event returns an error,
    /// the dispatcher stops processing remaining events and propagates the error.
    /// </para>
    /// <para>
    /// When <see langword="false"/> (default), errors are logged but dispatching
    /// continues for remaining events. All entities' events are still cleared
    /// regardless of errors.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="false"/></value>
    public bool StopOnFirstError { get; set; }

    /// <summary>
    /// Gets or sets whether domain events must implement <see cref="INotification"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/> (default), only domain events that implement
    /// <see cref="INotification"/> are dispatched through <see cref="IEncina.Publish{TNotification}"/>.
    /// Events that don't implement the interface are skipped with a warning.
    /// </para>
    /// <para>
    /// When <see langword="false"/>, non-INotification events are still skipped
    /// but without generating warnings.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool RequireINotification { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to clear domain events from entities after dispatching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/> (default), after all events from an entity are
    /// dispatched (or attempted), the events are cleared to prevent duplicate dispatching.
    /// </para>
    /// <para>
    /// When <see langword="false"/>, events remain on the entity. This can be useful
    /// for testing or when events need to be processed by multiple mechanisms.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool ClearEventsAfterDispatch { get; set; } = true;
}
