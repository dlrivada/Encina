namespace Encina.Security.Audit;

/// <summary>
/// Represents a query for filtering and paginating read audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to build flexible queries against the read audit store.
/// All filter properties are optional; <c>null</c> values are ignored during query execution.
/// </para>
/// <para>
/// Pagination defaults:
/// <list type="bullet">
/// <item><see cref="PageNumber"/> defaults to 1 (first page)</item>
/// <item><see cref="PageSize"/> defaults to 50 entries per page</item>
/// </list>
/// </para>
/// <para>
/// For compliance reporting, combine filters to answer regulatory questions:
/// <list type="bullet">
/// <item><b>GDPR Art. 15</b> — "Who accessed this patient's data?" → filter by <see cref="EntityType"/> + <see cref="EntityId"/></item>
/// <item><b>HIPAA</b> — "What did this user access?" → filter by <see cref="UserId"/> + date range</item>
/// <item><b>Security</b> — "Was there unusual export activity?" → filter by <see cref="AccessMethod"/></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query who accessed a specific patient's record in the last 7 days
/// var query = ReadAuditQuery.Builder()
///     .ForEntity("Patient", "PAT-12345")
///     .InDateRange(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow)
///     .WithPageSize(100)
///     .Build();
///
/// var result = await readAuditStore.QueryAsync(query, cancellationToken);
/// </code>
/// </example>
public sealed record ReadAuditQuery
{
    /// <summary>
    /// Filter by user ID.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.UserId"/> equals this value.
    /// </remarks>
    public string? UserId { get; init; }

    /// <summary>
    /// Filter by tenant ID.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.TenantId"/> equals this value.
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// Filter by entity type.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.EntityType"/> equals this value (case-insensitive).
    /// </remarks>
    public string? EntityType { get; init; }

    /// <summary>
    /// Filter by specific entity ID.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.EntityId"/> equals this value.
    /// Should be used together with <see cref="EntityType"/> for meaningful results.
    /// </remarks>
    public string? EntityId { get; init; }

    /// <summary>
    /// Filter by access method.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.AccessMethod"/> equals this value.
    /// Useful for identifying export operations or unusual access patterns.
    /// </remarks>
    public ReadAccessMethod? AccessMethod { get; init; }

    /// <summary>
    /// Filter by declared access purpose.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.Purpose"/> contains this value (case-insensitive).
    /// Supports GDPR Art. 15 compliance reporting on data access purposes.
    /// </remarks>
    public string? Purpose { get; init; }

    /// <summary>
    /// Filter by correlation ID.
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.CorrelationId"/> equals this value.
    /// Useful for tracing a specific request across distributed services.
    /// </remarks>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Filter by minimum access timestamp (inclusive).
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.AccessedAtUtc"/> is greater than or equal to this value.
    /// </remarks>
    public DateTimeOffset? FromUtc { get; init; }

    /// <summary>
    /// Filter by maximum access timestamp (inclusive).
    /// </summary>
    /// <remarks>
    /// Matches read audit entries where <see cref="ReadAuditEntry.AccessedAtUtc"/> is less than or equal to this value.
    /// </remarks>
    public DateTimeOffset? ToUtc { get; init; }

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
    /// <returns>A new <see cref="ReadAuditQueryBuilder"/> instance.</returns>
    /// <example>
    /// <code>
    /// var query = ReadAuditQuery.Builder()
    ///     .ForUser("user-123")
    ///     .ForEntityType("Patient")
    ///     .WithAccessMethod(ReadAccessMethod.Repository)
    ///     .InDateRange(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow)
    ///     .WithPageSize(100)
    ///     .Build();
    /// </code>
    /// </example>
    public static ReadAuditQueryBuilder Builder() => new();
}

/// <summary>
/// Fluent builder for constructing <see cref="ReadAuditQuery"/> instances.
/// </summary>
public sealed class ReadAuditQueryBuilder
{
    private string? _userId;
    private string? _tenantId;
    private string? _entityType;
    private string? _entityId;
    private ReadAccessMethod? _accessMethod;
    private string? _purpose;
    private string? _correlationId;
    private DateTimeOffset? _fromUtc;
    private DateTimeOffset? _toUtc;
    private int _pageNumber = 1;
    private int _pageSize = ReadAuditQuery.DefaultPageSize;

    /// <summary>
    /// Filters by user ID.
    /// </summary>
    /// <param name="userId">The user ID to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder ForUser(string userId)
    {
        _userId = userId;
        return this;
    }

    /// <summary>
    /// Filters by tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder ForTenant(string tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Filters by entity type.
    /// </summary>
    /// <param name="entityType">The entity type to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder ForEntityType(string entityType)
    {
        _entityType = entityType;
        return this;
    }

    /// <summary>
    /// Filters by specific entity.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder ForEntity(string entityType, string entityId)
    {
        _entityType = entityType;
        _entityId = entityId;
        return this;
    }

    /// <summary>
    /// Filters by access method.
    /// </summary>
    /// <param name="accessMethod">The access method to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder WithAccessMethod(ReadAccessMethod accessMethod)
    {
        _accessMethod = accessMethod;
        return this;
    }

    /// <summary>
    /// Filters by access purpose.
    /// </summary>
    /// <param name="purpose">The purpose text to filter by (contains match).</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder WithPurpose(string purpose)
    {
        _purpose = purpose;
        return this;
    }

    /// <summary>
    /// Filters by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to filter by.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder WithCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>
    /// Filters by date range.
    /// </summary>
    /// <param name="fromUtc">The start of the date range (inclusive), or <c>null</c> for no lower bound.</param>
    /// <param name="toUtc">The end of the date range (inclusive), or <c>null</c> for no upper bound.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder InDateRange(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        _fromUtc = fromUtc;
        _toUtc = toUtc;
        return this;
    }

    /// <summary>
    /// Sets the page number.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder OnPage(int pageNumber)
    {
        _pageNumber = pageNumber;
        return this;
    }

    /// <summary>
    /// Sets the page size.
    /// </summary>
    /// <param name="pageSize">The number of entries per page.</param>
    /// <returns>This builder for chaining.</returns>
    public ReadAuditQueryBuilder WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ReadAuditQuery"/> from the configured filters.
    /// </summary>
    /// <returns>A new <see cref="ReadAuditQuery"/> instance.</returns>
    public ReadAuditQuery Build() => new()
    {
        UserId = _userId,
        TenantId = _tenantId,
        EntityType = _entityType,
        EntityId = _entityId,
        AccessMethod = _accessMethod,
        Purpose = _purpose,
        CorrelationId = _correlationId,
        FromUtc = _fromUtc,
        ToUtc = _toUtc,
        PageNumber = _pageNumber,
        PageSize = _pageSize
    };
}
