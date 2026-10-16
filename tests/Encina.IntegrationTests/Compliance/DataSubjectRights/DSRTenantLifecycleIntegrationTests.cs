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
using Encina.Testing.Shouldly;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DataSubjectRights;

/// <summary>
/// Drives the whole request lifecycle of a tenant through <see cref="DefaultDSRService"/> against
/// a real PostgreSQL/Marten event store (#1963): the owning tenant keeps full use of its requests,
/// the inline read model carries the tenant, and a single-tenant application (no
/// <c>Encina.Tenancy</c>, no ambient tenant) still finds its own tenant-less requests, which proves
/// the <c>TenantId == null</c> predicate against PostgreSQL.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class DSRTenantLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public DSRTenantLifecycleIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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

    private static DefaultDSRService CreateService(IServiceScope scope, string? tenantId, bool multiTenant)
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
            Substitute.For<ICacheProvider>(),
            Options.Create(new DataSubjectRightsOptions()),
            TimeProvider.System,
            accessor,
            NullLogger<DefaultDSRService>.Instance,
            encina: null,
            tenantProvider: multiTenant ? Substitute.For<ITenantProvider>() : null);
    }

    [Fact]
    public async Task OwningTenant_DrivesVerifyStartCompleteAndTheReadModelCarriesTheTenant()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = CreateService(scope, "tenant-a", multiTenant: true);

        var id = (await service.SubmitRequestAsync($"subject-{Guid.NewGuid():N}", DataSubjectRight.Access)).ShouldBeSuccess();

        (await service.VerifyIdentityAsync(id, "controller")).ShouldBeSuccess();
        (await service.StartProcessingAsync(id)).ShouldBeSuccess();
        (await service.CompleteRequestAsync(id)).ShouldBeSuccess();

        var readModel = (await service.GetRequestAsync(id)).ShouldBeSuccess();
        readModel.Status.ShouldBe(DSRRequestStatus.Completed);
        readModel.TenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public async Task OwningTenant_CanExtendDenyAndExpireItsOwnRequests()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = CreateService(scope, "tenant-a", multiTenant: true);
        var subjectId = $"subject-{Guid.NewGuid():N}";

        var extended = (await service.SubmitRequestAsync(subjectId, DataSubjectRight.Access)).ShouldBeSuccess();
        var denied = (await service.SubmitRequestAsync(subjectId, DataSubjectRight.Erasure)).ShouldBeSuccess();
        var expired = (await service.SubmitRequestAsync(subjectId, DataSubjectRight.Portability)).ShouldBeSuccess();

        (await service.ExtendDeadlineAsync(extended, "complex request")).ShouldBeSuccess();
        (await service.DenyRequestAsync(denied, DSRRejectionReason.Other, "no legal ground")).ShouldBeSuccess();
        (await service.ExpireRequestAsync(expired)).ShouldBeSuccess();

        (await service.GetRequestAsync(extended)).ShouldBeSuccess(m => m.Status.ShouldBe(DSRRequestStatus.Extended));
        (await service.GetRequestAsync(denied)).ShouldBeSuccess(m => m.Status.ShouldBe(DSRRequestStatus.Rejected));
        (await service.GetRequestAsync(expired)).ShouldBeSuccess(m => m.Status.ShouldBe(DSRRequestStatus.Expired));
    }

    [Fact]
    public async Task SingleTenantApplication_NoTenantAnywhere_StillFindsItsOwnRequests()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = CreateService(scope, tenantId: null, multiTenant: false);
        var subjectId = $"subject-{Guid.NewGuid():N}";

        var id = (await service.SubmitRequestAsync(subjectId, DataSubjectRight.Restriction)).ShouldBeSuccess();

        (await service.GetRequestsBySubjectAsync(subjectId)).ShouldBeSuccess(rows => rows.Select(r => r.Id).ShouldBe([id]));
        (await service.HasActiveRestrictionAsync(subjectId)).ShouldBeSuccess(active => active.ShouldBeTrue());
        (await service.GetPendingRequestsAsync()).ShouldBeSuccess(rows => rows.ShouldContain(r => r.Id == id));
        (await service.GetRequestAsync(id)).ShouldBeSuccess(m => m.TenantId.ShouldBeNull());
    }

    [Fact]
    public async Task TenantlessRequest_IsInvisibleToATenantAndATenantsRequestIsInvisibleToTheSingleTenantView()
    {
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var tenantless = CreateService(scope, tenantId: null, multiTenant: false);
        var tenantA = CreateService(scope, "tenant-a", multiTenant: true);
        var subjectId = $"subject-{Guid.NewGuid():N}";

        var tenantlessId = (await tenantless.SubmitRequestAsync(subjectId, DataSubjectRight.Restriction)).ShouldBeSuccess();
        var tenantAId = (await tenantA.SubmitRequestAsync(subjectId, DataSubjectRight.Restriction)).ShouldBeSuccess();

        (await tenantA.GetRequestsBySubjectAsync(subjectId)).ShouldBeSuccess(rows => rows.Select(r => r.Id).ShouldBe([tenantAId]));
        (await tenantless.GetRequestsBySubjectAsync(subjectId)).ShouldBeSuccess(rows => rows.Select(r => r.Id).ShouldBe([tenantlessId]));
        (await tenantA.GetRequestAsync(tenantlessId)).ShouldBeErrorWithCode(DSRErrors.RequestNotFoundCode);
        (await tenantless.GetRequestAsync(tenantAId)).ShouldBeErrorWithCode(DSRErrors.RequestNotFoundCode);
    }
}
