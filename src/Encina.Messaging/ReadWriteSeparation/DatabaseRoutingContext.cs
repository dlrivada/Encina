namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Provides ambient context for database routing decisions using <see cref="AsyncLocal{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides thread-safe, async-aware storage for the current database routing intent.
/// The intent flows automatically across async boundaries, ensuring consistent routing
/// throughout a request's execution, including across await points.
/// </para>
/// <para>
/// The routing pipeline behavior sets the intent at the start of request processing,
/// and connection/context factories read it to determine which database to use.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// <see cref="AsyncLocal{T}"/> ensures that each async execution flow maintains its own
/// copy of the intent, preventing cross-request contamination in concurrent scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // The pipeline behavior sets the intent automatically
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
///
/// // Connection factories read the intent
/// var intent = DatabaseRoutingContext.CurrentIntent;
/// if (intent == DatabaseIntent.Read)
/// {
///     return CreateReadReplicaConnection();
/// }
/// return CreatePrimaryConnection();
/// </code>
/// </example>
public static class DatabaseRoutingContext
{
    private static readonly AsyncLocal<DatabaseIntent?> _currentIntent = new();
    private static readonly AsyncLocal<bool> _isEnabled = new();

    /// <summary>
    /// Gets or sets the current database routing intent for the executing async flow.
    /// </summary>
    /// <value>
    /// The current intent, or <see langword="null"/> if no intent has been set.
    /// When <see langword="null"/>, callers should default to <see cref="DatabaseIntent.Write"/>
    /// for safety.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property uses <see cref="AsyncLocal{T}"/> internally, so the value is
    /// scoped to the current async execution context. Changes made in one async flow
    /// do not affect other concurrent flows.
    /// </para>
    /// <para>
    /// For structured intent management, prefer using <see cref="DatabaseRoutingScope"/>
    /// which automatically restores the previous intent on disposal.
    /// </para>
    /// </remarks>
    public static DatabaseIntent? CurrentIntent
    {
        get => _currentIntent.Value;
        set => _currentIntent.Value = value;
    }

    /// <summary>
    /// Gets or sets whether database routing is enabled for the current async flow.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if routing is enabled; otherwise, <see langword="false"/>.
    /// Default is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When routing is disabled, connection selectors should fall back to the default
    /// (typically write) connection. This allows gradual rollout or temporary disabling
    /// of read/write separation.
    /// </para>
    /// </remarks>
    public static bool IsEnabled
    {
        get => _isEnabled.Value;
        set => _isEnabled.Value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the current context has an active routing intent.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="CurrentIntent"/> has a value;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public static bool HasIntent => _currentIntent.Value.HasValue;

    /// <summary>
    /// Gets the effective intent, defaulting to <see cref="DatabaseIntent.Write"/> if not set.
    /// </summary>
    /// <value>
    /// The current intent if set; otherwise, <see cref="DatabaseIntent.Write"/>.
    /// </value>
    /// <remarks>
    /// This property provides a safe default for cases where no intent has been explicitly set,
    /// ensuring write operations are never accidentally routed to read replicas.
    /// </remarks>
    public static DatabaseIntent EffectiveIntent =>
        _currentIntent.Value ?? DatabaseIntent.Write;

    /// <summary>
    /// Determines whether the current intent indicates a read operation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the current intent is <see cref="DatabaseIntent.Read"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Note that <see cref="DatabaseIntent.ForceWrite"/> returns <see langword="false"/>
    /// even though it may be set on a query, because it should use the primary database.
    /// </remarks>
    public static bool IsReadIntent =>
        _currentIntent.Value == DatabaseIntent.Read;

    /// <summary>
    /// Determines whether the current intent indicates a write or force-write operation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the current intent is <see cref="DatabaseIntent.Write"/>
    /// or <see cref="DatabaseIntent.ForceWrite"/>, or if no intent is set;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public static bool IsWriteIntent =>
        _currentIntent.Value is null or DatabaseIntent.Write or DatabaseIntent.ForceWrite;

    /// <summary>
    /// Clears the current routing context.
    /// </summary>
    /// <remarks>
    /// This resets both the intent and the enabled flag to their default values.
    /// Generally, this should not be needed as <see cref="DatabaseRoutingScope"/>
    /// handles cleanup automatically.
    /// </remarks>
    public static void Clear()
    {
        _currentIntent.Value = null;
        _isEnabled.Value = false;
    }
}
