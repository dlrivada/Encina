#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using System.Text;
using System.Text.Json;

using Encina.Security.ABAC;
using Encina.Security.ABAC.DecisionAudit;
using Encina.Security.Audit;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.DecisionAudit;

/// <summary>
/// Unit tests for the decision audit reader (#751 Phase 3): the five tenant-gate cases, the
/// mismatch denial, query validation, filter normalization and the JSON Lines export.
/// </summary>
public sealed class ABACDecisionAuditReaderTests
{
    private static readonly DateTimeOffset Base = new(2026, 10, 9, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemoryOperationAuditStore _store = new();
    private readonly FakeLogCollector _logs = new();

    private ABACDecisionAuditReader Reader(
        string? ambientTenant = null,
        bool multiTenant = false,
        bool allowCrossTenant = false,
        IOperationAuditStore? store = null,
        bool withStore = true)
    {
        var services = new ServiceCollection();
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(ambientTenant);
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        services.AddSingleton(accessor);

        if (withStore)
        {
            services.AddSingleton(store ?? _store);
        }

        if (multiTenant)
        {
            services.AddSingleton<MultiTenancyMarker>();
        }

        var options = new ABACOptions();
        options.DecisionAudit.AllowCrossTenantQueries = allowCrossTenant;

        return new ABACDecisionAuditReader(
            services.BuildServiceProvider().CreateScope().ServiceProvider,
            Options.Create(options),
            new FakeLogger<ABACDecisionAuditReader>(_logs));
    }

    private async Task SeedAsync(
        string? tenant,
        string userId = "user-1",
        string requestType = "GetOrderQuery",
        string? resourceId = "order-1",
        ABACEnforcedOutcome outcome = ABACEnforcedOutcome.Granted,
        string reason = ABACDecisionAuditSchema.PermitReasonCode,
        int minutes = 0,
        string correlationId = "corr-1")
    {
        var record = new ABACDecisionRecord
        {
            DecisionId = Guid.CreateVersion7(),
            UserId = userId,
            IdentityKind = IdentityKind.User,
            TenantId = tenant,
            CorrelationId = correlationId,
            RequestType = requestType,
            ResourceId = resourceId,
            EnforcedOutcome = outcome,
            ReasonCode = reason,
            EnforcementMode = ABACEnforcementMode.Block,
            StartedAtUtc = Base.AddMinutes(minutes),
            CompletedAtUtc = Base.AddMinutes(minutes)
        };

        (await _store.RecordAsync(ABACDecisionAuditEntryMapper.ToOperationAuditEntry(record))).IsRight.ShouldBeTrue();
    }

    private static PagedResult<ABACDecisionAuditRecord> Page(Either<EncinaError, PagedResult<ABACDecisionAuditRecord>> result) =>
        result.Match(Right: page => page, Left: error => throw new ShouldAssertException($"Expected Right, got {error.GetCode()}"));

    private static string Code<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => "<right>", Left: error => error.GetCode().IfNone("<none>"));

    // ── Tenant gate: the five cases ──────────────────────────────────

    [Fact]
    public async Task QueryAsync_AmbientTenantPresent_IsForced()
    {
        await SeedAsync("tenant-a");
        await SeedAsync("tenant-b");

        var page = Page(await Reader(ambientTenant: "tenant-a", multiTenant: true).QueryAsync(new ABACDecisionAuditQuery()));

        page.Items.ShouldHaveSingleItem().TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task QueryAsync_MultiTenantWithoutAmbientTenant_DeniesWithTenantRequired()
    {
        await SeedAsync("tenant-a");

        var result = await Reader(multiTenant: true).QueryAsync(new ABACDecisionAuditQuery { TenantId = "tenant-a" });

        Code(result).ShouldBe(ABACErrors.DecisionAuditTenantRequiredCode);
    }

    [Fact]
    public async Task QueryAsync_SingleTenantWithoutMarker_QueriesWithoutATenantFilter()
    {
        await SeedAsync(tenant: null);
        await SeedAsync("tenant-b");

        var page = Page(await Reader().QueryAsync(new ABACDecisionAuditQuery()));

        page.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task QueryAsync_MultiTenantWithCrossTenantOptOut_QueriesAndLogsTheOptOut()
    {
        await SeedAsync("tenant-a");
        await SeedAsync("tenant-b");

        var page = Page(await Reader(multiTenant: true, allowCrossTenant: true).QueryAsync(new ABACDecisionAuditQuery()));

        page.TotalCount.ShouldBe(2);
        _logs.GetSnapshot().ShouldContain(log => log.Id.Id == 9090 && log.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task QueryAsync_ExplicitTenantOtherThanTheAmbientOne_DeniesWithTenantMismatch()
    {
        await SeedAsync("tenant-b");

        var result = await Reader(ambientTenant: "tenant-a", multiTenant: true).QueryAsync(new ABACDecisionAuditQuery { TenantId = "tenant-b" });

        Code(result).ShouldBe(ABACErrors.DecisionAuditTenantMismatchCode);
    }

    [Fact]
    public async Task QueryAsync_ExplicitTenantOtherThanTheAmbientOneWithoutTheMarker_IsStillDenied()
    {
        var result = await Reader(ambientTenant: "tenant-a").QueryAsync(new ABACDecisionAuditQuery { TenantId = "tenant-b" });

        Code(result).ShouldBe(ABACErrors.DecisionAuditTenantMismatchCode);
    }

    [Fact]
    public async Task QueryAsync_BlankAmbientTenantInAMultiTenantApplication_CountsAsNoTenant()
    {
        await SeedAsync("tenant-a");

        var result = await Reader(ambientTenant: "   ", multiTenant: true).QueryAsync(new ABACDecisionAuditQuery());

        Code(result).ShouldBe(ABACErrors.DecisionAuditTenantRequiredCode);
    }

    [Fact]
    public async Task QueryAsync_BlankTenantFilter_IsRejectedRatherThanIgnoredByTheStore()
    {
        var result = await Reader().QueryAsync(new ABACDecisionAuditQuery { TenantId = " " });

        Code(result).ShouldBe(ABACErrors.InvalidDecisionAuditQueryCode);
        result.IfLeft(error => error.GetDetails()["reason"].ShouldBe("tenantId"));
    }

    [Fact]
    public async Task QueryAsync_ExplicitTenantEqualToTheAmbientOne_Queries()
    {
        await SeedAsync("tenant-a");

        var page = Page(await Reader(ambientTenant: "tenant-a", multiTenant: true).QueryAsync(new ABACDecisionAuditQuery { TenantId = "tenant-a" }));

        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task QueryAsync_AmbientTenantWithCrossTenantOptOut_StillForcesTheAmbientTenant()
    {
        await SeedAsync("tenant-a");
        await SeedAsync("tenant-b");

        var page = Page(await Reader(ambientTenant: "tenant-a", multiTenant: true, allowCrossTenant: true).QueryAsync(new ABACDecisionAuditQuery()));

        page.Items.ShouldHaveSingleItem().TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task QueryAsync_AmbientTenantOverTheColumnLimit_FindsTheHashedEntries()
    {
        var longTenant = new string('t', ABACDecisionAuditSchema.TenantIdMaxLength + 1);
        await SeedAsync(longTenant);
        await SeedAsync("tenant-b");

        var page = Page(await Reader(ambientTenant: longTenant, multiTenant: true).QueryAsync(new ABACDecisionAuditQuery()));

        page.Items.ShouldHaveSingleItem().TenantId.ShouldBe(ABACDecisionAuditEntryMapper.Hash(longTenant));
    }

    // ── Filters ──────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_FiltersBySubjectRequestResourceOutcomeAndCorrelation()
    {
        await SeedAsync(null, userId: "user-1", requestType: "GetOrderQuery", resourceId: "order-1",
            outcome: ABACEnforcedOutcome.Denied, reason: ABACErrors.AccessDeniedCode, correlationId: "corr-x");
        await SeedAsync(null, userId: "user-2");
        await SeedAsync(null, userId: "user-1", requestType: "DeleteOrderCommand");
        await SeedAsync(null, userId: "user-1", resourceId: "order-2");

        var page = Page(await Reader().QueryAsync(new ABACDecisionAuditQuery
        {
            UserId = "user-1",
            RequestType = "GetOrderQuery",
            ResourceId = "order-1",
            Outcome = AuditOutcome.Denied,
            CorrelationId = "corr-x"
        }));

        var item = page.Items.ShouldHaveSingleItem();
        item.ReasonCode.ShouldBe(ABACErrors.AccessDeniedCode);
        item.Outcome.ShouldBe(AuditOutcome.Denied);
    }

    [Fact]
    public async Task QueryAsync_SubjectOverTheColumnLimit_IsFoundByItsOriginalValue()
    {
        var longUser = new string('u', ABACDecisionAuditSchema.UserIdMaxLength + 1);
        var longType = new string('q', ABACDecisionAuditSchema.EntityTypeMaxLength + 1);
        var longResource = new string('r', ABACDecisionAuditSchema.EntityIdMaxLength + 1);
        await SeedAsync(null, userId: longUser, requestType: longType, resourceId: longResource);
        await SeedAsync(null);

        var page = Page(await Reader().QueryAsync(new ABACDecisionAuditQuery
        {
            UserId = longUser,
            RequestType = longType,
            ResourceId = longResource
        }));

        page.Items.ShouldHaveSingleItem().HashedFields.ShouldBe(["UserId", "EntityType", "EntityId"]);
    }

    [Fact]
    public async Task QueryAsync_ReturnsOnlyDecisionAuditEntries()
    {
        await SeedAsync(null);
        await _store.RecordAsync(new OperationAuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1",
            Action = "Create",
            EntityType = "Order",
            Outcome = AuditOutcome.Success,
            TimestampUtc = Base.UtcDateTime,
            StartedAtUtc = Base,
            CompletedAtUtc = Base
        });

        var page = Page(await Reader().QueryAsync(new ABACDecisionAuditQuery()));

        page.Items.ShouldHaveSingleItem().RequestType.ShouldBe("GetOrderQuery");
    }

    [Fact]
    public async Task QueryAsync_TimeRange_IsPassedToTheStore()
    {
        await SeedAsync(null, minutes: 0);
        await SeedAsync(null, minutes: 30);

        var page = Page(await Reader().QueryAsync(new ABACDecisionAuditQuery
        {
            FromUtc = Base.AddMinutes(10).UtcDateTime,
            ToUtc = Base.AddMinutes(60).UtcDateTime
        }));

        page.Items.ShouldHaveSingleItem().StartedAtUtc.ShouldBe(Base.AddMinutes(30));
    }

    // ── Validation and errors ────────────────────────────────────────

    [Theory]
    [InlineData(0, 10, "pageNumber")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, OperationAuditQuery.MaxPageSize + 1, "pageSize")]
    public async Task QueryAsync_InvalidPaging_ReturnsInvalidQuery(int pageNumber, int pageSize, string reason)
    {
        var result = await Reader().QueryAsync(new ABACDecisionAuditQuery { PageNumber = pageNumber, PageSize = pageSize });

        Code(result).ShouldBe(ABACErrors.InvalidDecisionAuditQueryCode);
        result.IfLeft(error => error.GetDetails()["reason"].ShouldBe(reason));
    }

    [Fact]
    public async Task QueryAsync_FromAfterTo_ReturnsInvalidQuery()
    {
        var result = await Reader().QueryAsync(new ABACDecisionAuditQuery
        {
            FromUtc = Base.AddDays(1).UtcDateTime,
            ToUtc = Base.UtcDateTime
        });

        Code(result).ShouldBe(ABACErrors.InvalidDecisionAuditQueryCode);
    }

    [Fact]
    public async Task QueryAsync_WithoutAStore_ReturnsStoreUnavailable()
    {
        Code(await Reader(withStore: false).QueryAsync(new ABACDecisionAuditQuery())).ShouldBe(ABACErrors.DecisionAuditStoreUnavailableCode);
    }

    [Fact]
    public async Task QueryAsync_StoreFails_ReturnsTheStoreError()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.QueryAsync(Arg.Any<OperationAuditQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, PagedResult<OperationAuditEntry>>(EncinaErrors.Create("store.down", "x"))));

        Code(await Reader(store: store).QueryAsync(new ABACDecisionAuditQuery())).ShouldBe("store.down");
    }

    [Fact]
    public async Task QueryAsync_NullQuery_Throws() =>
        await Should.ThrowAsync<ArgumentNullException>(async () => await Reader().QueryAsync(null!));

    // ── Export ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExportAsync_WritesEveryMatchingDecisionAsOneJsonLineAcrossPages()
    {
        var total = OperationAuditQuery.MaxPageSize + 2;
        for (var i = 0; i < total; i++)
        {
            await SeedAsync(null, minutes: i % 50);
        }

        using var stream = new MemoryStream();
        var result = await Reader().ExportAsync(new ABACDecisionAuditQuery { PageNumber = 7, PageSize = 3 }, stream);

        result.Match(Right: count => count, Left: _ => -1).ShouldBe(total);
        var lines = Encoding.UTF8.GetString(stream.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(total);
        using var first = JsonDocument.Parse(lines[0]);
        first.RootElement.GetProperty("schema").GetString().ShouldBe(ABACDecisionAuditSchema.SchemaVersion);
        first.RootElement.GetProperty("outcome").GetString().ShouldBe("Success");
        first.RootElement.GetProperty("requestType").GetString().ShouldBe("GetOrderQuery");
        lines.Select(line => JsonDocument.Parse(line).RootElement.GetProperty("decisionId").GetGuid()).Distinct().Count().ShouldBe(total);
    }

    [Fact]
    public async Task ExportAsync_CrossTenantOptOut_LogsTheOptOutOncePerExport()
    {
        for (var i = 0; i < OperationAuditQuery.MaxPageSize + 1; i++)
        {
            await SeedAsync($"tenant-{i % 2}", minutes: i % 50);
        }

        using var stream = new MemoryStream();
        var result = await Reader(multiTenant: true, allowCrossTenant: true).ExportAsync(new ABACDecisionAuditQuery(), stream);

        result.Match(Right: count => count, Left: _ => -1).ShouldBe(OperationAuditQuery.MaxPageSize + 1);
        _logs.GetSnapshot().Count(log => log.Id.Id == 9090).ShouldBe(1);
    }

    [Fact]
    public async Task ExportAsync_StoreFails_ReturnsTheStoreError()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.QueryAsync(Arg.Any<OperationAuditQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, PagedResult<OperationAuditEntry>>(EncinaErrors.Create("store.down", "x"))));
        using var stream = new MemoryStream();

        Code(await Reader(store: store).ExportAsync(new ABACDecisionAuditQuery(), stream)).ShouldBe("store.down");
    }

    [Fact]
    public async Task ExportAsync_NothingMatches_WritesNothing()
    {
        using var stream = new MemoryStream();

        var result = await Reader().ExportAsync(new ABACDecisionAuditQuery { UserId = "nobody" }, stream);

        result.Match(Right: count => count, Left: _ => -1).ShouldBe(0);
        stream.Length.ShouldBe(0);
    }

    [Fact]
    public async Task ExportAsync_TenantGateDenies_ReturnsTheDenialAndWritesNothing()
    {
        await SeedAsync("tenant-a");
        using var stream = new MemoryStream();

        var result = await Reader(multiTenant: true).ExportAsync(new ABACDecisionAuditQuery(), stream);

        Code(result).ShouldBe(ABACErrors.DecisionAuditTenantRequiredCode);
        stream.Length.ShouldBe(0);
    }

    [Fact]
    public async Task ExportAsync_NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () => await Reader().ExportAsync(null!, Stream.Null));
        await Should.ThrowAsync<ArgumentNullException>(async () => await Reader().ExportAsync(new ABACDecisionAuditQuery(), null!));
    }
}
