using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DataSubjectRights;

/// <summary>
/// Proves against a real PostgreSQL/Marten event store (event-sourced module: no InMemory store)
/// that the requests and processing restrictions of one tenant can never be read, cached, mutated
/// or listed by another tenant through <see cref="DefaultDSRService"/> (#1963). A cache shared by
/// both tenants' services makes a cache-key leak observable.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class DSRTenantScopingIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;
    private readonly SharedCache _cache = new();

    public DSRTenantScopingIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TwoTenants_SameSubject_NeverSeeEachOthersRequestsRestrictionsOrCacheEntries()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var subjectId = $"subject-{Guid.NewGuid():N}";

        var tenantA = CreateService(scope, "tenant-a");
        var tenantB = CreateService(scope, "tenant-b");

        // Tenant A submits a restriction request for the subject (tenant taken from the ambient context).
        var idA = (await tenantA.SubmitRequestAsync(subjectId, DataSubjectRight.Restriction))
            .Match(id => id, _ => throw new InvalidOperationException("Submit failed"));

        // Tenant A sees its own request, its restriction, and warms the caches.
        (await tenantA.HasActiveRestrictionAsync(subjectId)).ShouldBeRightAnd(active => active.ShouldBeTrue());
        (await tenantA.GetRequestAsync(idA)).IsRight.ShouldBeTrue();
        (await tenantA.GetRequestsBySubjectAsync(subjectId)).ShouldBeRightAnd(rows => rows.Select(r => r.Id).ShouldBe([idA]));

        // Tenant B, same subject id: no restriction (not even from the shared cache), no request by
        // id, no listing, nothing pending or overdue.
        (await tenantB.HasActiveRestrictionAsync(subjectId)).ShouldBeRightAnd(active => active.ShouldBeFalse());
        (await tenantB.GetRequestAsync(idA)).ShouldBeLeftAnd(e => e.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.RequestNotFoundCode));
        (await tenantB.GetRequestsBySubjectAsync(subjectId)).ShouldBeRightAnd(rows => rows.ShouldBeEmpty());
        (await tenantB.GetPendingRequestsAsync()).ShouldBeRightAnd(rows => rows.ShouldNotContain(r => r.Id == idA));
        (await tenantB.GetOverdueRequestsAsync()).ShouldBeRightAnd(rows => rows.ShouldNotContain(r => r.Id == idA));
        (await tenantA.GetPendingRequestsAsync()).ShouldBeRightAnd(rows => rows.ShouldContain(r => r.Id == idA));

        // The cache holds tenant-scoped keys only.
        _cache.Keys.ShouldNotContain(key => key == $"dsr:request:{idA}" || key == $"dsr:restriction:{subjectId}");
        _cache.Keys.ShouldContain($"dsr:tenant:tenant-a:request:{idA}");
        _cache.Keys.ShouldContain($"dsr:tenant:tenant-a:restriction:{subjectId}");
        _cache.Keys.ShouldNotContain($"dsr:tenant:tenant-b:request:{idA}");
    }

    [Fact]
    public async Task OtherTenantsRequest_EveryLifecycleCommand_IsNotFoundAndNeverMutated()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<DSRRequestAggregate>>();
        var readModels = scope.ServiceProvider.GetRequiredService<IReadModelRepository<DSRRequestReadModel>>();

        var tenantA = CreateService(scope, "tenant-a");
        var tenantB = CreateService(scope, "tenant-b");
        var id = (await tenantA.SubmitRequestAsync($"subject-{Guid.NewGuid():N}", DataSubjectRight.Access))
            .Match(i => i, _ => throw new InvalidOperationException("Submit failed"));

        var results = new[]
        {
            await tenantB.VerifyIdentityAsync(id, "attacker"),
            await tenantB.StartProcessingAsync(id),
            await tenantB.CompleteRequestAsync(id),
            await tenantB.DenyRequestAsync(id, DSRRejectionReason.Other, "denied by another tenant"),
            await tenantB.ExtendDeadlineAsync(id, "extended by another tenant"),
            await tenantB.ExpireRequestAsync(id)
        };

        results.ShouldAllBe(r => r.IsLeft);
        results.ShouldAllBe(r => r.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty) == DSRErrors.RequestNotFoundCode);

        // Nothing was written to the owning tenant's request.
        var aggregate = (await repository.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        aggregate.Status.ShouldBe(DSRRequestStatus.Received);
        aggregate.TenantId.ShouldBe("tenant-a");
        var readModel = (await readModels.GetByIdAsync(id)).Match(m => m, _ => throw new InvalidOperationException("Read model missing"));
        readModel.Status.ShouldBe(DSRRequestStatus.Received);
        readModel.VerifiedAtUtc.ShouldBeNull();

        // The owning tenant still can.
        (await tenantA.VerifyIdentityAsync(id, "controller")).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SubmitRequest_ContradictingTenantArgument_IsRefusedAndNothingPersisted()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tenantB = CreateService(scope, "tenant-b");
        var subjectId = $"subject-{Guid.NewGuid():N}";

        var result = await tenantB.SubmitRequestAsync(subjectId, DataSubjectRight.Restriction, tenantId: "tenant-a");

        result.ShouldBeLeftAnd(e => e.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.TenantRequiredCode));
        var tenantA = CreateService(scope, "tenant-a");
        (await tenantA.GetRequestsBySubjectAsync(subjectId)).ShouldBeRightAnd(rows => rows.ShouldBeEmpty());
    }

    [Fact]
    public async Task HandleRestriction_RecordsRestrictionUnderAmbientTenantOnly()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tenantA = CreateService(scope, "tenant-a");
        var tenantB = CreateService(scope, "tenant-b");
        var subjectId = $"subject-{Guid.NewGuid():N}";

        (await tenantA.HandleRestrictionAsync(new RestrictionRequest(subjectId, "accuracy contested", null)))
            .IsRight.ShouldBeTrue();

        (await tenantA.HasActiveRestrictionAsync(subjectId)).ShouldBeRightAnd(active => active.ShouldBeTrue());
        (await tenantB.HasActiveRestrictionAsync(subjectId)).ShouldBeRightAnd(active => active.ShouldBeFalse());
    }

    [Fact]
    public async Task MultiTenantApplication_NoAmbientTenant_FailsClosed()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var noTenant = CreateService(scope, tenantId: null);

        var restriction = await noTenant.HasActiveRestrictionAsync("subject-1");
        var submit = await noTenant.SubmitRequestAsync("subject-1", DataSubjectRight.Access);
        var pending = await noTenant.GetPendingRequestsAsync();

        foreach (var result in new[] { restriction.Map(_ => 0), submit.Map(_ => 0), pending.Map(_ => 0) })
        {
            result.ShouldBeLeftAnd(e => e.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.TenantRequiredCode));
            result.IsLeft.ShouldBeTrue();
        }
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped(_ => Substitute.For<IRequestContext>());

        services.AddEncinaMarten(_ => { });
        services.AddDSRRequestAggregates();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Builds a service whose ambient request context carries <paramref name="tenantId"/> in an
    /// application that registered Encina.Tenancy (auto-detect: fails closed without a tenant).
    /// </summary>
    private DefaultDSRService CreateService(IServiceScope scope, string? tenantId)
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(tenantId is null ? null : RequestContext.CreateForTest(tenantId: tenantId));

        return new DefaultDSRService(
            scope.ServiceProvider.GetRequiredService<IAggregateRepository<DSRRequestAggregate>>(),
            scope.ServiceProvider.GetRequiredService<IReadModelRepository<DSRRequestReadModel>>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            _cache,
            Options.Create(new DataSubjectRightsOptions()),
            TimeProvider.System,
            accessor,
            NullLogger<DefaultDSRService>.Instance,
            encina: null,
            tenantProvider: Substitute.For<ITenantProvider>());
    }

    private sealed class SharedCache : ICacheProvider
    {
        private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Keys => _entries.Keys;

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_entries.TryGetValue(key, out var value) ? (T?)value : default);

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
        {
            _entries[key] = value;
            return Task.CompletedTask;
        }

        public Task SetWithSlidingExpirationAsync<T>(
            string key, T value, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration, CancellationToken cancellationToken) =>
            SetAsync(key, value, slidingExpiration, cancellationToken);

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _entries.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_entries.ContainsKey(key));

        public Task<T> GetOrSetAsync<T>(
            string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration, CancellationToken cancellationToken) =>
            factory(cancellationToken);

        public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}

file static class EitherAssertions
{
    public static void ShouldBeRightAnd<L, R>(this LanguageExt.Either<L, R> either, Action<R> assert)
    {
        either.IsRight.ShouldBeTrue("expected Right but the result was Left");
        either.IfRight(assert);
    }

    public static void ShouldBeLeftAnd<L, R>(this LanguageExt.Either<L, R> either, Action<L> assert)
    {
        either.IsLeft.ShouldBeTrue("expected Left but the result was Right");
        either.IfLeft(assert);
    }
}
