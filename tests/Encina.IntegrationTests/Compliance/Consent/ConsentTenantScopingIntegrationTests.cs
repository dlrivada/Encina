using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;

namespace Encina.IntegrationTests.Compliance.Consent;

/// <summary>
/// Integration tests proving that <see cref="DefaultConsentService"/> query methods are scoped by
/// the ambient tenant against a real PostgreSQL/Marten backend (#1315). Uses
/// <see cref="MartenReadModelRepository{TReadModel}"/> directly to persist
/// <see cref="ConsentReadModel"/> documents without going through the full event-sourced
/// aggregate/projection pipeline, which is unnecessary to exercise the tenant-scoped query path.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class ConsentTenantScopingIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public ConsentTenantScopingIntegrationTests(MartenFixture fixture)
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
    public async Task GetAllConsentsAsync_TwoTenants_EachSeesOnlyItsOwnRecord()
    {
        // Arrange — same data subject and purpose in two different tenants.
        var subjectId = $"subject-{Guid.NewGuid():N}";
        const string purpose = "marketing";

        await StoreReadModelAsync(new ConsentReadModel
        {
            Id = Guid.NewGuid(),
            DataSubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            Source = "web-form",
            TenantId = "tenant-a",
            GivenAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        });
        await StoreReadModelAsync(new ConsentReadModel
        {
            Id = Guid.NewGuid(),
            DataSubjectId = subjectId,
            Purpose = purpose,
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            Source = "web-form",
            TenantId = "tenant-b",
            GivenAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        });

        // Act
        var serviceForTenantA = CreateService(tenantId: "tenant-a", tenantProvider: Substitute.For<ITenantProvider>());
        var resultForTenantA = await serviceForTenantA.GetAllConsentsAsync(subjectId);

        var serviceForTenantB = CreateService(tenantId: "tenant-b", tenantProvider: Substitute.For<ITenantProvider>());
        var resultForTenantB = await serviceForTenantB.GetAllConsentsAsync(subjectId);

        // Assert — each tenant observes exactly its own record, never the other tenant's.
        resultForTenantA.IsRight.ShouldBeTrue();
        resultForTenantA.IfRight(models =>
        {
            models.Count.ShouldBe(1);
            models[0].TenantId.ShouldBe("tenant-a");
        });

        resultForTenantB.IsRight.ShouldBeTrue();
        resultForTenantB.IfRight(models =>
        {
            models.Count.ShouldBe(1);
            models[0].TenantId.ShouldBe("tenant-b");
        });
    }

    [Fact]
    public async Task GetConsentAsync_ById_FromOtherTenant_ReturnsNotFound()
    {
        // Arrange — a consent record that belongs to tenant-a.
        var consentId = Guid.NewGuid();
        await StoreReadModelAsync(new ConsentReadModel
        {
            Id = consentId,
            DataSubjectId = $"subject-{Guid.NewGuid():N}",
            Purpose = "marketing",
            Status = ConsentStatus.Active,
            ConsentVersionId = "v1",
            Source = "web-form",
            TenantId = "tenant-a",
            GivenAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        });

        // Act — tenant-b tries to read tenant-a's consent by id.
        var serviceForTenantB = CreateService(tenantId: "tenant-b", tenantProvider: Substitute.For<ITenantProvider>());
        var resultForOtherTenant = await serviceForTenantB.GetConsentAsync(consentId);

        var serviceForTenantA = CreateService(tenantId: "tenant-a", tenantProvider: Substitute.For<ITenantProvider>());
        var resultForOwnTenant = await serviceForTenantA.GetConsentAsync(consentId);

        // Assert — the record is reported as not found for the other tenant, never leaked;
        // the owning tenant reads it successfully.
        resultForOtherTenant.IsLeft.ShouldBeTrue();
        resultForOwnTenant.IsRight.ShouldBeTrue();
        resultForOwnTenant.IfRight(model => model.Id.ShouldBe(consentId));
    }

    [Fact]
    public async Task GetAllConsentsAsync_MultiTenantApplication_NoAmbientTenant_FailsClosed()
    {
        // Arrange — Encina.Tenancy is registered (multi-tenant application) but this call has no
        // ambient tenant (e.g., a background job running without request context).
        var service = CreateService(tenantId: null, tenantProvider: Substitute.For<ITenantProvider>());

        // Act
        var result = await service.GetAllConsentsAsync($"subject-{Guid.NewGuid():N}");

        // Assert — fails closed instead of running an unscoped query (SPEC-002 DEC-006/DEC-009).
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfSome(code => code.ShouldBe(ConsentErrors.TenantRequiredCode)));
    }

    private async Task StoreReadModelAsync(ConsentReadModel model)
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<ConsentReadModel>(
            session, NullLogger<MartenReadModelRepository<ConsentReadModel>>.Instance);
        var result = await repo.StoreAsync(model);
        result.IsRight.ShouldBeTrue($"Seeding a consent read model should succeed: {result}");
    }

    private DefaultConsentService CreateService(string? tenantId, ITenantProvider? tenantProvider)
    {
        var session = _fixture.Store!.LightweightSession();
        var readModelRepository = new MartenReadModelRepository<ConsentReadModel>(
            session, NullLogger<MartenReadModelRepository<ConsentReadModel>>.Instance);

        var requestContextAccessor = Substitute.For<IRequestContextAccessor>();
        requestContextAccessor.RequestContext.Returns(
            tenantId is null ? null : RequestContext.CreateForTest(tenantId: tenantId));

        return new DefaultConsentService(
            Substitute.For<IAggregateRepository<ConsentAggregate>>(),
            readModelRepository,
            Substitute.For<ICacheProvider>(),
            TimeProvider.System,
            requestContextAccessor,
            Options.Create(new ConsentOptions()),
            NullLogger<DefaultConsentService>.Instance,
            tenantProvider);
    }
}
