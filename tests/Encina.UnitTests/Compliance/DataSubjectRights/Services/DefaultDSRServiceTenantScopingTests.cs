using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Services;

/// <summary>
/// Tenant isolation rules of <see cref="DefaultDSRService"/> (#1963): ambient-tenant query filters,
/// not-found-never-mutated for another tenant's request, tenant-scoped cache keys, the
/// <see cref="DataSubjectRightsOptions.RequireTenantContext"/> auto-detect and logged opt-out, and the
/// ambient-tenant handling of <see cref="IDSRService.SubmitRequestAsync"/>.
/// </summary>
public class DefaultDSRServiceTenantScopingTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IAggregateRepository<DSRRequestAggregate> _repository =
        Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
    private readonly IReadModelRepository<DSRRequestReadModel> _readModels =
        Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly IRequestContextAccessor _accessor = Substitute.For<IRequestContextAccessor>();
    private readonly List<DSRRequestReadModel> _store = [];

    public DefaultDSRServiceTenantScopingTests()
    {
        // The repository applies the caller's query to the in-memory store, so the tenant
        // predicate of the service is what is exercised.
        _readModels.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>();
                IReadOnlyList<DSRRequestReadModel> rows = query(_store.AsQueryable()).ToList();
                return Task.FromResult(Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>(rows));
            });
    }

    private void SetTenant(string? tenantId) =>
        _accessor.RequestContext.Returns(tenantId is null ? null : RequestContext.CreateForTest(tenantId: tenantId));

    private DefaultDSRService CreateService(
        bool multiTenant = true, bool? requireTenantContext = null, ILogger<DefaultDSRService>? logger = null) =>
        new(
            _repository,
            _readModels,
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            _cache,
            Options.Create(new DataSubjectRightsOptions { RequireTenantContext = requireTenantContext }),
            _time,
            _accessor,
            logger ?? NullLogger<DefaultDSRService>.Instance,
            encina: null,
            tenantProvider: multiTenant ? Substitute.For<ITenantProvider>() : null);

    private DSRRequestReadModel Row(
        string tenantId,
        string subjectId = "subject-1",
        DSRRequestStatus status = DSRRequestStatus.Received,
        DataSubjectRight right = DataSubjectRight.Restriction,
        DateTimeOffset? deadline = null)
    {
        var row = new DSRRequestReadModel
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            RightType = right,
            Status = status,
            TenantId = tenantId.Length == 0 ? null : tenantId,
            ReceivedAtUtc = Now.AddDays(-40),
            DeadlineAtUtc = deadline ?? Now.AddDays(-10)
        };
        _store.Add(row);
        return row;
    }

    private static string CodeOf(EncinaError error) => error.GetCode().IfNone(string.Empty);

    // ---------------------------------------------------------------- queries

    [Fact]
    public async Task GetRequestsBySubject_ReturnsOnlyAmbientTenantsRequests()
    {
        var own = Row("tenant-b");
        Row("tenant-a");
        SetTenant("tenant-b");

        var result = await CreateService().GetRequestsBySubjectAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        result.IfRight(rows => rows.Select(r => r.Id).ShouldBe([own.Id]));
    }

    [Fact]
    public async Task GetPendingRequests_ReturnsOnlyAmbientTenantsRequests()
    {
        var own = Row("tenant-a");
        Row("tenant-b");
        SetTenant("tenant-a");

        var result = await CreateService().GetPendingRequestsAsync();

        result.IfRight(rows => rows.Select(r => r.Id).ShouldBe([own.Id]));
    }

    [Fact]
    public async Task GetOverdueRequests_ReturnsOnlyAmbientTenantsRequests()
    {
        var own = Row("tenant-a");
        Row("tenant-b");
        SetTenant("tenant-a");

        var result = await CreateService().GetOverdueRequestsAsync();

        result.IfRight(rows => rows.Select(r => r.Id).ShouldBe([own.Id]));
    }

    [Fact]
    public async Task HasActiveRestriction_OtherTenantsRestriction_IsNotVisible()
    {
        Row("tenant-a");
        SetTenant("tenant-b");

        var result = await CreateService().HasActiveRestrictionAsync("subject-1");

        result.IfRight(active => active.ShouldBeFalse());
    }

    [Fact]
    public async Task HasActiveRestriction_OwnTenantsRestriction_IsVisible()
    {
        Row("tenant-a");
        SetTenant("tenant-a");

        var result = await CreateService().HasActiveRestrictionAsync("subject-1");

        result.IfRight(active => active.ShouldBeTrue());
    }

    [Fact]
    public async Task HasActiveRestriction_CacheKey_IsScopedByTenant()
    {
        SetTenant("tenant-a");
        var subjectId = "subject-1";

        await CreateService().HasActiveRestrictionAsync(subjectId);

        await _cache.Received(1).GetAsync<bool?>($"dsr:tenant:tenant-a:restriction:{subjectId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasActiveRestriction_NoTenant_UsesDashSegment()
    {
        SetTenant(null);

        await CreateService(multiTenant: false).HasActiveRestrictionAsync("subject-1");

        await _cache.Received(1).GetAsync<bool?>("dsr:tenant:-:restriction:subject-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRequest_OtherTenantsRequest_IsNotFoundAndNotCached()
    {
        var other = Row("tenant-a");
        _readModels.GetByIdAsync(other.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestReadModel>(other)));
        SetTenant("tenant-b");

        var result = await CreateService().GetRequestAsync(other.Id);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.RequestNotFoundCode));
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<DSRRequestReadModel>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRequest_OwnTenantsRequest_IsReturnedAndCachedUnderTenantKey()
    {
        var own = Row("tenant-a");
        _readModels.GetByIdAsync(own.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestReadModel>(own)));
        SetTenant("tenant-a");

        var result = await CreateService().GetRequestAsync(own.Id);

        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).SetAsync(
            $"dsr:tenant:tenant-a:request:{own.Id}", own, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRequest_CachedEntryOfOtherTenant_IsTreatedAsMiss()
    {
        var other = Row("tenant-a");
        SetTenant("tenant-b");
        _cache.GetAsync<DSRRequestReadModel>($"dsr:tenant:tenant-b:request:{other.Id}", Arg.Any<CancellationToken>())
            .Returns(other);
        _readModels.GetByIdAsync(other.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestReadModel>(other)));

        var result = await CreateService().GetRequestAsync(other.Id);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRequest_NullAndEmptyTenantMatch()
    {
        var row = Row(string.Empty);
        _readModels.GetByIdAsync(row.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestReadModel>(row)));
        SetTenant(null);

        var result = await CreateService(multiTenant: false).GetRequestAsync(row.Id);

        result.IsRight.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- lifecycle commands

    private DSRRequestAggregate Aggregate(string? tenantId)
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, Now, tenantId: tenantId);
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));
        return aggregate;
    }

    public static TheoryData<string> Commands =>
        ["Verify", "Start", "Complete", "Deny", "Extend", "Expire"];

    private static ValueTask<Either<EncinaError, Unit>> Run(DefaultDSRService sut, string command, Guid id) =>
        command switch
        {
            "Verify" => sut.VerifyIdentityAsync(id, "admin"),
            "Start" => sut.StartProcessingAsync(id),
            "Complete" => sut.CompleteRequestAsync(id),
            "Deny" => sut.DenyRequestAsync(id, DSRRejectionReason.ManifestlyUnfounded, "explanation"),
            "Extend" => sut.ExtendDeadlineAsync(id, "complex"),
            "Expire" => sut.ExpireRequestAsync(id),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task LifecycleCommand_OtherTenantsRequest_IsNotFoundAndNeverSaved(string command)
    {
        var aggregate = Aggregate("tenant-a");
        SetTenant("tenant-b");

        var result = await Run(CreateService(), command, aggregate.Id);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.RequestNotFoundCode));
        await _repository.DidNotReceive().SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task LifecycleCommand_NoAmbientTenant_MultiTenantApp_FailsClosedWithoutLoading(string command)
    {
        SetTenant(null);

        var result = await Run(CreateService(), command, Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.TenantRequiredCode));
        await _repository.DidNotReceive().LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyIdentity_OwnTenantsRequest_IsSavedAndCacheKeyedByTenant()
    {
        var aggregate = Aggregate("tenant-a");
        SetTenant("tenant-a");

        var result = await CreateService().VerifyIdentityAsync(aggregate.Id, "admin");

        result.IsRight.ShouldBeTrue();
        await _repository.Received(1).SaveAsync(aggregate, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"dsr:tenant:tenant-a:request:{aggregate.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExpireRequest_OwnTenantsRequest_InvalidatesTenantScopedRestrictionKey()
    {
        var aggregate = Aggregate("tenant-a");
        SetTenant("tenant-a");

        (await CreateService().ExpireRequestAsync(aggregate.Id)).IsRight.ShouldBeTrue();

        await _cache.Received(1).RemoveAsync("dsr:tenant:tenant-a:restriction:subject-1", Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- RequireTenantContext

    [Fact]
    public async Task Query_MultiTenantApp_NoTenant_AutoDetectFailsClosed()
    {
        SetTenant(null);

        var result = await CreateService(multiTenant: true, requireTenantContext: null).GetPendingRequestsAsync();

        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.TenantRequiredCode));
        await _readModels.DidNotReceive().QueryAsync(
            Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_SingleTenantApp_NoTenant_AutoDetectAllows()
    {
        SetTenant(null);

        var result = await CreateService(multiTenant: false, requireTenantContext: null).GetPendingRequestsAsync();

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Query_SingleTenantApp_ExplicitTrue_FailsClosed()
    {
        SetTenant(null);

        var result = await CreateService(multiTenant: false, requireTenantContext: true).GetPendingRequestsAsync();

        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.TenantRequiredCode));
    }

    [Fact]
    public async Task Query_MultiTenantApp_ExplicitFalse_AllowsAndLogsOptOutOnce()
    {
        SetTenant(null);
        var logger = new CapturingLogger();

        var sut = CreateService(multiTenant: true, requireTenantContext: false, logger: logger);
        (await sut.GetPendingRequestsAsync()).IsRight.ShouldBeTrue();
        (await sut.GetOverdueRequestsAsync()).IsRight.ShouldBeTrue();

        logger.Entries.Count(e => e.Id == 8314 && e.Level == LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void Constructor_SingleTenantApp_ExplicitFalse_DoesNotLogOptOut()
    {
        var logger = new CapturingLogger();

        _ = CreateService(multiTenant: false, requireTenantContext: false, logger: logger);

        logger.Entries.Count(e => e.Id == 8314).ShouldBe(0);
    }

    private sealed class CapturingLogger : ILogger<DefaultDSRService>
    {
        public List<(int Id, LogLevel Level)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((eventId.Id, logLevel));
    }

    [Fact]
    public void Constructor_NullRequestContextAccessor_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new DefaultDSRService(
                _repository, _readModels, Substitute.For<IPersonalDataLocator>(),
                Substitute.For<IDataErasureExecutor>(), Substitute.For<IDataPortabilityExporter>(),
                Substitute.For<IProcessingActivityRegistry>(), _cache,
                Options.Create(new DataSubjectRightsOptions()), _time, null!,
                NullLogger<DefaultDSRService>.Instance))
            .ParamName.ShouldBe("requestContextAccessor");
    }

    // ---------------------------------------------------------------- SubmitRequest / HandleRestriction

    private DSRRequestAggregate? Captured;

    private void CaptureCreate() =>
        _repository.CreateAsync(Arg.Do<DSRRequestAggregate>(a => Captured = a), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

    [Fact]
    public async Task SubmitRequest_NoArgument_TakesAmbientTenant()
    {
        CaptureCreate();
        SetTenant("tenant-a");

        var result = await CreateService().SubmitRequestAsync("subject-1", DataSubjectRight.Restriction);

        result.IsRight.ShouldBeTrue();
        Captured!.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task SubmitRequest_MatchingArgument_IsAccepted()
    {
        CaptureCreate();
        SetTenant("tenant-a");

        var result = await CreateService().SubmitRequestAsync(
            "subject-1", DataSubjectRight.Restriction, tenantId: "tenant-a");

        result.IsRight.ShouldBeTrue();
        Captured!.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task SubmitRequest_ContradictingArgument_IsRefusedAndNothingCreated()
    {
        SetTenant("tenant-b");

        var result = await CreateService().SubmitRequestAsync(
            "subject-1", DataSubjectRight.Restriction, tenantId: "tenant-a");

        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.TenantRequiredCode));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitRequest_ArgumentWithoutAmbientTenant_SingleTenantApp_UsesArgument()
    {
        CaptureCreate();
        SetTenant(null);

        var result = await CreateService(multiTenant: false).SubmitRequestAsync(
            "subject-1", DataSubjectRight.Restriction, tenantId: "tenant-a");

        result.IsRight.ShouldBeTrue();
        Captured!.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task SubmitRequest_NoTenantAnywhere_MultiTenantApp_FailsClosed()
    {
        SetTenant(null);

        var result = await CreateService().SubmitRequestAsync("subject-1", DataSubjectRight.Restriction);

        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.TenantRequiredCode));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRestriction_RecordsRestrictionUnderAmbientTenantAndInvalidatesTenantKey()
    {
        CaptureCreate();
        SetTenant("tenant-a");

        var result = await CreateService().HandleRestrictionAsync(
            new RestrictionRequest("subject-1", "accuracy contested", null));

        result.IsRight.ShouldBeTrue();
        Captured!.TenantId.ShouldBe("tenant-a");
        await _cache.Received(1).RemoveAsync("dsr:tenant:tenant-a:restriction:subject-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRestriction_NoTenant_MultiTenantApp_FailsClosed()
    {
        SetTenant(null);

        var result = await CreateService().HandleRestrictionAsync(
            new RestrictionRequest("subject-1", "accuracy contested", null));

        result.IfLeft(e => CodeOf(e).ShouldBe(DSRErrors.TenantRequiredCode));
        await _repository.DidNotReceive().CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>());
    }
}
