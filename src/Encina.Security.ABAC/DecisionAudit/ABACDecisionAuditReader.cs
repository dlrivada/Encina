using System.Text.Json;

using Encina.Diagnostics;
using Encina.Security.ABAC.Diagnostics;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// The default <see cref="IABACDecisionAuditReader"/> over the application's
/// <see cref="IOperationAuditStore"/>.
/// </summary>
/// <remarks>
/// The store is resolved inside each call, never at construction, so the reader is registered
/// whether or not the application registers an operation audit store; without one every call
/// returns <see cref="ABACErrors.DecisionAuditStoreUnavailable"/>.
/// </remarks>
internal sealed class ABACDecisionAuditReader : IABACDecisionAuditReader
{
    private static readonly byte[] LineSeparator = "\n"u8.ToArray();

    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ABACOptions> _options;
    private readonly ILogger<ABACDecisionAuditReader> _logger;

    public ABACDecisionAuditReader(
        IServiceProvider serviceProvider,
        IOptions<ABACOptions> options,
        ILogger<ABACDecisionAuditReader> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, PagedResult<ABACDecisionAuditRecord>>> QueryAsync(
        ABACDecisionAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await Validate(query)
            .Bind(_ => ResolveTenant(query.TenantId))
            .MatchAsync(
                RightAsync: tenant => RunQueryAsync(ToStoreQuery(query, tenant.TenantId), cancellationToken),
                Left: error => (Either<EncinaError, PagedResult<ABACDecisionAuditRecord>>)error)
            .ConfigureAwait(false);
    }

    private async Task<Either<EncinaError, PagedResult<ABACDecisionAuditRecord>>> RunQueryAsync(
        OperationAuditQuery storeQuery, CancellationToken cancellationToken)
    {
        var store = _serviceProvider.GetService<IOperationAuditStore>();
        if (store is null)
        {
            return ABACErrors.DecisionAuditStoreUnavailable();
        }

        var page = await store.QueryAsync(storeQuery, cancellationToken).ConfigureAwait(false);
        return ReadRecords(page);
    }

    // Stored data that cannot be read back is a data problem, not a crash: the query fails with a
    // code, and the parser's exception is logged redacted (type and stack trace only).
    private Either<EncinaError, PagedResult<ABACDecisionAuditRecord>> ReadRecords(
        Either<EncinaError, PagedResult<OperationAuditEntry>> page)
    {
        try
        {
            return page.Map(ToRecords);
        }
        catch (JsonException ex)
        {
            ABACLogMessages.DecisionAuditRecordUnreadable(_logger, ex.ForLogging());
            return ABACErrors.DecisionAuditRecordUnreadable(ex.GetType().Name);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> ExportAsync(
        ABACDecisionAuditQuery query,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(destination);

        // The tenant gate runs (and logs) once per export. The range is not pinned here: the Marten
        // store rejects a UTC DateTime filter (follow-up issue), so a caller that needs a stable
        // export under live writes sets ToUtc itself.
        var pinned = query with { PageNumber = 1, PageSize = OperationAuditQuery.MaxPageSize };

        return await Validate(pinned)
            .Bind(_ => ResolveTenant(pinned.TenantId))
            .MatchAsync(
                RightAsync: tenant => ExportPagesAsync(ToStoreQuery(pinned, tenant.TenantId), destination, cancellationToken),
                Left: error => (Either<EncinaError, int>)error)
            .ConfigureAwait(false);
    }

    private async Task<Either<EncinaError, int>> ExportPagesAsync(
        OperationAuditQuery storeQuery, Stream destination, CancellationToken cancellationToken)
    {
        var written = 0;

        while (true)
        {
            var page = await RunQueryAsync(storeQuery, cancellationToken).ConfigureAwait(false);
            if (page.IsLeft)
            {
                return page.Map(_ => written);
            }

            var records = page.IfLeft(PagedResult<ABACDecisionAuditRecord>.Empty());
            await WriteLinesAsync(destination, records.Items, cancellationToken).ConfigureAwait(false);
            written += records.Count;

            if (!records.HasNextPage || records.Count == 0)
            {
                return written;
            }

            storeQuery = storeQuery with { PageNumber = storeQuery.PageNumber + 1 };
        }
    }

    // ── Validation and tenant gate ───────────────────────────────────

    private static Either<EncinaError, Unit> Validate(ABACDecisionAuditQuery query) =>
        ValidatePaging(query).Bind(_ => ValidateRange(query));

    private static Either<EncinaError, Unit> ValidatePaging(ABACDecisionAuditQuery query)
    {
        if (query.PageNumber < 1)
        {
            return ABACErrors.InvalidDecisionAuditQuery("pageNumber");
        }

        return query.PageSize is < 1 or > OperationAuditQuery.MaxPageSize
            ? ABACErrors.InvalidDecisionAuditQuery("pageSize")
            : Unit.Default;
    }

    private static Either<EncinaError, Unit> ValidateRange(ABACDecisionAuditQuery query) =>
        query is { FromUtc: { } from, ToUtc: { } to } && from > to
            ? ABACErrors.InvalidDecisionAuditQuery("dateRange")
            : Unit.Default;

    // The tenant filter the store query runs with. The ambient tenant always wins; without one, a
    // multi-tenant application denies unless the operator opt-out is set; a single-tenant one passes.
    // A blank tenant is no tenant: stores ignore a whitespace tenant filter, so treating it as a
    // tenant would read every tenant's trail.
    private Either<EncinaError, TenantFilter> ResolveTenant(string? requested)
    {
        if (requested is not null && string.IsNullOrWhiteSpace(requested))
        {
            return ABACErrors.InvalidDecisionAuditQuery("tenantId");
        }

        var ambient = _serviceProvider.GetService<IRequestContextAccessor>()?.RequestContext?.TenantId;
        return string.IsNullOrWhiteSpace(ambient) ? WithoutAmbientTenant(requested) : WithAmbientTenant(ambient, requested);
    }

    private static Either<EncinaError, TenantFilter> WithAmbientTenant(string ambient, string? requested) =>
        requested is null || string.Equals(requested, ambient, StringComparison.Ordinal)
            ? new TenantFilter(ambient)
            : ABACErrors.DecisionAuditTenantMismatch();

    private Either<EncinaError, TenantFilter> WithoutAmbientTenant(string? requested)
    {
        if (!IsMultiTenant())
        {
            return new TenantFilter(requested);
        }

        if (!_options.Value.DecisionAudit.AllowCrossTenantQueries)
        {
            return ABACErrors.DecisionAuditTenantRequired();
        }

        ABACLogMessages.DecisionAuditCrossTenantQuery(_logger);
        return new TenantFilter(requested);
    }

    /// <summary>The tenant a store query filters by; <c>null</c> means no tenant filter.</summary>
    private readonly record struct TenantFilter(string? TenantId);

    private bool IsMultiTenant() =>
        _serviceProvider.GetService<IServiceProviderIsService>()?.IsService(typeof(MultiTenancyMarker))
            ?? _serviceProvider.GetService<MultiTenancyMarker>() is not null;

    // ── Mapping ──────────────────────────────────────────────────────

    // Filters go through the mapper's column rule, so a value stored as a hash is found by its original.
    private static OperationAuditQuery ToStoreQuery(ABACDecisionAuditQuery query, string? tenantId) => new()
    {
        Action = ABACDecisionAuditSchema.Action,
        UserId = ABACDecisionAuditEntryMapper.NormalizeIdentifier(query.UserId, ABACDecisionAuditSchema.UserIdMaxLength),
        TenantId = ABACDecisionAuditEntryMapper.NormalizeIdentifier(tenantId, ABACDecisionAuditSchema.TenantIdMaxLength),
        EntityType = ABACDecisionAuditEntryMapper.NormalizeIdentifier(query.RequestType, ABACDecisionAuditSchema.EntityTypeMaxLength),
        EntityId = ABACDecisionAuditEntryMapper.NormalizeIdentifier(query.ResourceId, ABACDecisionAuditSchema.EntityIdMaxLength),
        CorrelationId = ABACDecisionAuditEntryMapper.NormalizeIdentifier(query.CorrelationId, ABACDecisionAuditSchema.CorrelationIdMaxLength),
        Outcome = query.Outcome,
        FromUtc = query.FromUtc,
        ToUtc = query.ToUtc,
        PageNumber = query.PageNumber,
        PageSize = query.PageSize
    };

    private static PagedResult<ABACDecisionAuditRecord> ToRecords(PagedResult<OperationAuditEntry> page) =>
        PagedResult<ABACDecisionAuditRecord>.Create(
            page.Items.Select(ABACDecisionAuditEntryMapper.ToDecisionAuditRecord).ToList(),
            page.TotalCount,
            page.PageNumber,
            page.PageSize);

    private static async ValueTask WriteLinesAsync(
        Stream destination, IReadOnlyList<ABACDecisionAuditRecord> records, CancellationToken cancellationToken)
    {
        foreach (var record in records)
        {
            await JsonSerializer.SerializeAsync(
                destination, record, ABACDecisionAuditJsonContext.Default.ABACDecisionAuditRecord, cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(LineSeparator, cancellationToken).ConfigureAwait(false);
        }
    }
}
