using System.Text.Json;

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
        return page.Map(ToRecords);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, int>> ExportAsync(
        ABACDecisionAuditQuery query,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(destination);

        var written = 0;
        var pageQuery = query with { PageNumber = 1, PageSize = OperationAuditQuery.MaxPageSize };

        while (true)
        {
            var page = await QueryAsync(pageQuery, cancellationToken).ConfigureAwait(false);
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

            pageQuery = pageQuery with { PageNumber = pageQuery.PageNumber + 1 };
        }
    }

    // ── Validation and tenant gate ───────────────────────────────────

    private static Either<EncinaError, Unit> Validate(ABACDecisionAuditQuery query)
    {
        if (query.PageNumber < 1)
        {
            return ABACErrors.InvalidDecisionAuditQuery("pageNumber");
        }

        if (query.PageSize is < 1 or > OperationAuditQuery.MaxPageSize)
        {
            return ABACErrors.InvalidDecisionAuditQuery("pageSize");
        }

        return query.FromUtc > query.ToUtc
            ? ABACErrors.InvalidDecisionAuditQuery("dateRange")
            : Unit.Default;
    }

    // The tenant filter the store query runs with. The ambient tenant always wins; without one, a
    // multi-tenant application denies unless the operator opt-out is set; a single-tenant one passes.
    private Either<EncinaError, TenantFilter> ResolveTenant(string? requested)
    {
        var ambient = _serviceProvider.GetService<IRequestContextAccessor>()?.RequestContext?.TenantId;
        if (!string.IsNullOrEmpty(ambient))
        {
            return requested is null || string.Equals(requested, ambient, StringComparison.Ordinal)
                ? new TenantFilter(ambient)
                : ABACErrors.DecisionAuditTenantMismatch();
        }

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
