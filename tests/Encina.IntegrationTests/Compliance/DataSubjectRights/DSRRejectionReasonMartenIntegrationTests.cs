using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Marten.Projections;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DataSubjectRights;

/// <summary>
/// Proves that the standardised refusal reason code and the explanation of a denied data subject
/// request survive Marten serialization of <c>DSRRequestDenied</c> and reach the inline
/// <see cref="DSRRequestReadModel"/> against a real PostgreSQL database (#814).
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class DSRRejectionReasonMartenIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public DSRRejectionReasonMartenIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public static TheoryData<DSRRejectionReason> AllReasons => new(Enum.GetValues<DSRRejectionReason>());

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

    [Theory]
    [MemberData(nameof(AllReasons))]
    public async Task Deny_EveryReason_RoundTripsThroughEventStoreAndInlineProjection(DSRRejectionReason reason)
    {
        await using var provider = BuildServiceProvider();
        var id = Guid.NewGuid();
        var explanation = $"Explanation for {reason}";

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<DSRRequestAggregate>>();
            var aggregate = DSRRequestAggregate.Submit(
                id, $"subject-{id:N}", DataSubjectRight.Access, DateTimeOffset.UtcNow, tenantId: "tenant-1", moduleId: "module-1");
            (await repository.CreateAsync(aggregate)).IsRight.ShouldBeTrue();
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<DSRRequestAggregate>>();
            var aggregate = (await repository.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
            aggregate.Deny(reason, explanation, DateTimeOffset.UtcNow);
            (await repository.SaveAsync(aggregate)).IsRight.ShouldBeTrue();
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<DSRRequestAggregate>>();
            var reloaded = (await repository.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
            reloaded.Status.ShouldBe(DSRRequestStatus.Rejected);
            reloaded.RejectionReason.ShouldBe(reason);
            reloaded.RejectionExplanation.ShouldBe(explanation);

            var readModels = scope.ServiceProvider.GetRequiredService<IReadModelRepository<DSRRequestReadModel>>();
            var readModel = (await readModels.GetByIdAsync(id)).Match(m => m, _ => throw new InvalidOperationException("Read model missing"));
            readModel.Status.ShouldBe(DSRRequestStatus.Rejected);
            readModel.RejectionReason.ShouldBe(reason);
            readModel.RejectionExplanation.ShouldBe(explanation);
            readModel.TenantId.ShouldBe("tenant-1");
            readModel.ModuleId.ShouldBe("module-1");
        }
    }

    [Fact]
    public async Task DenyRequestAsync_ThroughService_PersistsCodeAndExplanationAndGatesDraftGround()
    {
        await using var provider = BuildServiceProvider();
        var id = Guid.NewGuid();

        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<DSRRequestAggregate>>();
        var readModels = scope.ServiceProvider.GetRequiredService<IReadModelRepository<DSRRequestReadModel>>();
        (await repository.CreateAsync(DSRRequestAggregate.Submit(
            id, $"subject-{id:N}", DataSubjectRight.Erasure, DateTimeOffset.UtcNow))).IsRight.ShouldBeTrue();

        var service = new DefaultDSRService(
            repository,
            readModels,
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            Options.Create(new DataSubjectRightsOptions()),
            TimeProvider.System,
            new RequestContextAccessor(),
            NullLogger<DefaultDSRService>.Instance);

        var gated = await service.DenyRequestAsync(id, DSRRejectionReason.AbusiveIntent, "Not enabled");
        gated.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.RejectionReasonNotEnabledCode);

        var denied = await service.DenyRequestAsync(id, DSRRejectionReason.RestrictedByLaw, "Art. 23 restriction");
        denied.IsRight.ShouldBeTrue();

        var readModel = (await readModels.GetByIdAsync(id)).Match(m => m, _ => throw new InvalidOperationException("Read model missing"));
        readModel.Status.ShouldBe(DSRRequestStatus.Rejected);
        readModel.RejectionReason.ShouldBe(DSRRejectionReason.RestrictedByLaw);
        readModel.RejectionExplanation.ShouldBe("Art. 23 restriction");
    }
}
