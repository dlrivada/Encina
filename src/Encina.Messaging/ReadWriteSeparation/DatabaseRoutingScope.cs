namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// A disposable scope that sets the database routing intent and restores the previous value on disposal.
/// </summary>
/// <remarks>
/// <para>
/// This class provides structured management of the <see cref="DatabaseRoutingContext.CurrentIntent"/>,
/// ensuring the previous intent is restored when the scope is disposed. This is essential for
/// nested scenarios where an inner operation needs a different intent than the outer operation.
/// </para>
/// <para>
/// The scope is typically used by pipeline behaviors to set the intent for a request,
/// but it can also be used directly in handler code for fine-grained control.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Pipeline behavior usage
/// public async Task&lt;Either&lt;EncinaError, TResponse&gt;&gt; Handle(
///     TRequest request,
///     RequestHandlerDelegate&lt;TResponse&gt; next,
///     CancellationToken ct)
/// {
///     var intent = request is IQuery ? DatabaseIntent.Read : DatabaseIntent.Write;
///     using var scope = new DatabaseRoutingScope(intent);
///     return await next();
/// }
///
/// // Direct usage for read-after-write consistency
/// await _repository.CreateAsync(entity);
/// using (var scope = new DatabaseRoutingScope(DatabaseIntent.ForceWrite))
/// {
///     var created = await _repository.GetByIdAsync(entity.Id);
/// }
/// </code>
/// </example>
public readonly struct DatabaseRoutingScope : IDisposable
{
    private readonly DatabaseIntent? _previousIntent;
    private readonly bool _previousEnabled;

    /// <summary>
    /// Gets the intent that was set when this scope was created.
    /// </summary>
    public DatabaseIntent Intent { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRoutingScope"/> struct.
    /// </summary>
    /// <param name="intent">The database routing intent to set for this scope.</param>
    /// <remarks>
    /// <para>
    /// The current <see cref="DatabaseRoutingContext.CurrentIntent"/> is saved and
    /// the new intent is set. When the scope is disposed, the previous intent is restored.
    /// </para>
    /// <para>
    /// This constructor also sets <see cref="DatabaseRoutingContext.IsEnabled"/> to
    /// <see langword="true"/>, indicating that routing is active.
    /// </para>
    /// </remarks>
    public DatabaseRoutingScope(DatabaseIntent intent)
    {
        _previousIntent = DatabaseRoutingContext.CurrentIntent;
        _previousEnabled = DatabaseRoutingContext.IsEnabled;
        Intent = intent;

        DatabaseRoutingContext.CurrentIntent = intent;
        DatabaseRoutingContext.IsEnabled = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRoutingScope"/> struct
    /// that optionally enables routing.
    /// </summary>
    /// <param name="intent">The database routing intent to set for this scope.</param>
    /// <param name="enabled">
    /// Whether to enable routing. If <see langword="false"/>, the intent is set but
    /// <see cref="DatabaseRoutingContext.IsEnabled"/> remains unchanged.
    /// </param>
    public DatabaseRoutingScope(DatabaseIntent intent, bool enabled)
    {
        _previousIntent = DatabaseRoutingContext.CurrentIntent;
        _previousEnabled = DatabaseRoutingContext.IsEnabled;
        Intent = intent;

        DatabaseRoutingContext.CurrentIntent = intent;
        DatabaseRoutingContext.IsEnabled = enabled;
    }

    /// <summary>
    /// Restores the previous routing intent and enabled state.
    /// </summary>
    /// <remarks>
    /// This method is called automatically when using the scope with a <c>using</c>
    /// statement or block. It restores the exact state that existed before this
    /// scope was created, including <see langword="null"/> intents.
    /// </remarks>
    public void Dispose()
    {
        DatabaseRoutingContext.CurrentIntent = _previousIntent;
        DatabaseRoutingContext.IsEnabled = _previousEnabled;
    }

    /// <summary>
    /// Creates a scope for read operations.
    /// </summary>
    /// <returns>A new <see cref="DatabaseRoutingScope"/> with <see cref="DatabaseIntent.Read"/>.</returns>
    /// <remarks>
    /// Convenience method equivalent to <c>new DatabaseRoutingScope(DatabaseIntent.Read)</c>.
    /// </remarks>
    public static DatabaseRoutingScope ForRead() => new(DatabaseIntent.Read);

    /// <summary>
    /// Creates a scope for write operations.
    /// </summary>
    /// <returns>A new <see cref="DatabaseRoutingScope"/> with <see cref="DatabaseIntent.Write"/>.</returns>
    /// <remarks>
    /// Convenience method equivalent to <c>new DatabaseRoutingScope(DatabaseIntent.Write)</c>.
    /// </remarks>
    public static DatabaseRoutingScope ForWrite() => new(DatabaseIntent.Write);

    /// <summary>
    /// Creates a scope that forces read operations to use the primary database.
    /// </summary>
    /// <returns>A new <see cref="DatabaseRoutingScope"/> with <see cref="DatabaseIntent.ForceWrite"/>.</returns>
    /// <remarks>
    /// <para>
    /// Use this when a query requires read-after-write consistency.
    /// </para>
    /// <para>
    /// Convenience method equivalent to <c>new DatabaseRoutingScope(DatabaseIntent.ForceWrite)</c>.
    /// </para>
    /// </remarks>
    public static DatabaseRoutingScope ForForceWrite() => new(DatabaseIntent.ForceWrite);
}
