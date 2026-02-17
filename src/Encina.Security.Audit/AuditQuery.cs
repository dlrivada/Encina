namespace Encina.Security.Audit;

/// <summary>
/// Represents a query for filtering and paginating audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to build flexible queries against the audit store.
/// All filter properties are optional; <c>null</c> values are ignored.
/// </para>
/// <para>
/// Pagination defaults:
/// <list type="bullet">
/// <item><see cref="PageNumber"/> defaults to 1 (first page)</item>
/// <item><see cref="PageSize"/> defaults to 50 entries per page</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query recent failed operations for a specific user
/// var query = new AuditQuery
/// {
///     UserId = "user-123",
///     Outcome = AuditOutcome.Failure,
///     FromUtc = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-7),
///     PageSize = 100
/// };
///
/// var result = await auditStore.QueryAsync(query);
/// </code>
/// </example>
public sealed record AuditQuery
{
    /// <summary>
    /// Filter by user ID.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.UserId"/> equals this value.
    /// </remarks>
    public string? UserId { get; init; }

    /// <summary>
    /// Filter by tenant ID.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.TenantId"/> equals this value.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.EntityType"/> equals this value (case-insensitive).
    /// </remarks>
    public string? EntityType { get; init; }

    /// <summary>
    /// Filter by specific entity ID.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.EntityId"/> equals this value.
    /// Must be used together with <see cref="EntityType"/> for meaningful results.
    /// </remarks>
    public string? EntityId { get; init; }

    /// <summary>
    /// Filter by action type.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.Action"/> equals this value (case-insensitive).
    /// Examples: "Create", "Update", "Delete", "Get", "List".
    /// </remarks>
    public string? Action { get; init; }

    /// <summary>
    /// Filter by operation outcome.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.Outcome"/> equals this value.
    /// Use to find failed operations, denied access attempts, or successful operations.
    /// </remarks>
    public AuditOutcome? Outcome { get; init; }

    /// <summary>
    /// Filter by correlation ID.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.CorrelationId"/> equals this value.
    /// Useful for tracing a specific request across distributed services.
    /// </remarks>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Filter by minimum timestamp (inclusive).
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.TimestampUtc"/> is greater than or equal to this value.
    /// </remarks>
    public DateTime? FromUtc { get; init; }

    /// <summary>
    /// Filter by maximum timestamp (inclusive).
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.TimestampUtc"/> is less than or equal to this value.
    /// </remarks>
    public DateTime? ToUtc { get; init; }

    /// <summary>
    /// Filter by IP address.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.IpAddress"/> equals this value.
    /// Useful for investigating suspicious activity from a specific IP.
    /// </remarks>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Filter by minimum operation duration.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.Duration"/> is greater than or equal to this value.
    /// Useful for identifying slow operations that may need optimization.
    /// </remarks>
    public TimeSpan? MinDuration { get; init; }

    /// <summary>
    /// Filter by maximum operation duration.
    /// </summary>
    /// <remarks>
    /// Matches audit entries where <see cref="AuditEntry.Duration"/> is less than or equal to this value.
    /// </remarks>
    public TimeSpan? MaxDuration { get; init; }

    /// <summary>
    /// The page number to retrieve (1-based).
    /// </summary>
    /// <remarks>
    /// Default is 1 (first page). Must be greater than 0.
    /// </remarks>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// The number of entries per page.
    /// </summary>
    /// <remarks>
    /// Default is 50. Must be between 1 and <see cref="MaxPageSize"/>.
    /// </remarks>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>
    /// The default page size when not specified.
    /// </summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// The maximum allowed page size to prevent excessive memory usage.
    /// </summary>
    public const int MaxPageSize = 1000;

    /// <summary>
    /// Creates a query builder for fluent construction.
    /// </summary>
    /// <returns>A new <see cref="AuditQueryBuilder"/> instance.</returns>
    /// <example>
    /// <code>
    /// var query = AuditQuery.Builder()
    ///     .ForUser("user-123")
    ///     .WithOutcome(AuditOutcome.Failure)
    ///     .InDateRange(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-7), TimeProvider.System.GetUtcNow().UtcDateTime)
    ///     .WithPageSize(100)
    ///     .Build();
    /// </code>
    /// </example>
    public static AuditQueryBuilder Builder() => new();
}

/// <summary>
/// Fluent builder for constructing <see cref="AuditQuery"/> instances.
/// </summary>
public sealed class AuditQueryBuilder
{
    private string? _userId;
    private string? _tenantId;
    private string? _entityType;
    private string? _entityId;
    private string? _action;
    private AuditOutcome? _outcome;
    private string? _correlationId;
    private DateTime? _fromUtc;
    private DateTime? _toUtc;
    private string? _ipAddress;
    private TimeSpan? _minDuration;
    private TimeSpan? _maxDuration;
    private int _pageNumber = 1;
    private int _pageSize = AuditQuery.DefaultPageSize;

    /// <summary>
    /// Filters by user ID.
    /// </summary>
    public AuditQueryBuilder ForUser(string userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Filters by tenant ID.
    /// </summary>
    public AuditQueryBuilder ForTenant(string tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Filters by entity type.
    /// </summary>
    public AuditQueryBuilder ForEntityType(string entityType)
    {
        _entityType = entityType;
        return this;
    }

    /// <summary>
    /// Filters by specific entity.
    /// </summary>
    public AuditQueryBuilder ForEntity(string entityType, string entityId)
    {
        _entityType = entityType;
        _entityId = entityId;
        return this;
    }

    /// <summary>
    /// Filters by action type.
    /// </summary>
    public AuditQueryBuilder WithAction(string action)
    {
        _action = action;
        return this;
    }

    /// <summary>
    /// Filters by operation outcome.
    /// </summary>
    public AuditQueryBuilder WithOutcome(AuditOutcome outcome)
    {
        _outcome = outcome;
        return this;
    }

    /// <summary>
    /// Filters by correlation ID.
    /// </summary>
    public AuditQueryBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>
    /// Filters by date range.
    /// </summary>
    public AuditQueryBuilder InDateRange(DateTime? fromUtc, DateTime? toUtc)
    {
        _fromUtc = fromUtc;
        _toUtc = toUtc;
        return this;
    }

    /// <summary>
    /// Filters by IP address.
    /// </summary>
    public AuditQueryBuilder FromIpAddress(string ipAddress)
    {
        _ipAddress = ipAddress;
        return this;
    }

    /// <summary>
    /// Filters by operation duration range.
    /// </summary>
    public AuditQueryBuilder WithDurationRange(TimeSpan? minDuration, TimeSpan? maxDuration)
    {
        _minDuration = minDuration;
        _maxDuration = maxDuration;
        return this;
    }

    /// <summary>
    /// Sets the page number.
    /// </summary>
    public AuditQueryBuilder OnPage(int pageNumber)
    {
        _pageNumber = pageNumber;
        return this;
    }

    /// <summary>
    /// Sets the page size.
    /// </summary>
    public AuditQueryBuilder WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Builds the query.
    /// </summary>
    public AuditQuery Build() => new()
    {
        UserId = _userId,
        TenantId = _tenantId,
        EntityType = _entityType,
        EntityId = _entityId,
        Action = _action,
        Outcome = _outcome,
        CorrelationId = _correlationId,
        FromUtc = _fromUtc,
        ToUtc = _toUtc,
        IpAddress = _ipAddress,
        MinDuration = _minDuration,
        MaxDuration = _maxDuration,
        PageNumber = _pageNumber,
        PageSize = _pageSize
    };
}
