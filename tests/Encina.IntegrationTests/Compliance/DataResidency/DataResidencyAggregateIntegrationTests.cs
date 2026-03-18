using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DataResidency;

/// <summary>
/// Integration tests for data residency aggregates persisted via Marten against real PostgreSQL.
/// Verifies event store persistence, aggregate loading, state reconstruction, and lifecycle transitions.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DataResidencyAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public DataResidencyAggregateIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private MartenAggregateRepository<T> CreateRepository<T>() where T : class, IAggregate
    {
        var session = _fixture.Store!.LightweightSession();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        var logger = NullLoggerFactory.Instance.CreateLogger<MartenAggregateRepository<T>>();
        var options = Options.Create(new EncinaMartenOptions());
        return new MartenAggregateRepository<T>(
            session,
            requestContext,
            (Microsoft.Extensions.Logging.ILogger<MartenAggregateRepository<T>>)logger,
            options);
    }

    #region ResidencyPolicy Aggregate Persistence

    [Fact]
    public async Task Policy_CreateAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<ResidencyPolicyAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ResidencyPolicyAggregate.Create(
            id,
            "healthcare-data",
            ["DE", "FR", "NL"],
            requireAdequacyDecision: true,
            [TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses],
            "tenant-1",
            "compliance");

        // Act
        var createResult = await repo.CreateAsync(aggregate);

        // Assert — creation succeeds
        createResult.IsRight.ShouldBeTrue();

        // Act — load from event store
        var loadRepo = CreateRepository<ResidencyPolicyAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);

        // Assert — state reconstructed from events
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.DataCategory.ShouldBe("healthcare-data");
            loaded.AllowedRegionCodes.ShouldBe(["DE", "FR", "NL"]);
            loaded.RequireAdequacyDecision.ShouldBeTrue();
            loaded.AllowedTransferBases.ShouldBe([TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses]);
            loaded.IsActive.ShouldBeTrue();
            loaded.TenantId.ShouldBe("tenant-1");
            loaded.ModuleId.ShouldBe("compliance");
        });
    }

    [Fact]
    public async Task Policy_FullLifecycle_CreateUpdateDelete()
    {
        // Arrange — create policy
        var repo = CreateRepository<ResidencyPolicyAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ResidencyPolicyAggregate.Create(
            id, "financial-data", ["DE", "CH"], false,
            [TransferLegalBasis.StandardContractualClauses, TransferLegalBasis.BindingCorporateRules]);
        await repo.CreateAsync(aggregate);

        // Act — update policy
        var loadRepo1 = CreateRepository<ResidencyPolicyAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.Update(
            ["DE", "CH", "AT", "LU"],
            true,
            [TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses]);
        await loadRepo1.SaveAsync(loaded1);

        // Act — delete policy
        var loadRepo2 = CreateRepository<ResidencyPolicyAggregate>();
        var loaded2 = (await loadRepo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Delete("Replaced by EU-wide policy");
        await loadRepo2.SaveAsync(loaded2);

        // Assert — final state from fresh load
        var verifyRepo = CreateRepository<ResidencyPolicyAggregate>();
        var finalResult = await verifyRepo.LoadAsync(id);

        finalResult.IsRight.ShouldBeTrue();
        finalResult.IfRight(final =>
        {
            final.IsActive.ShouldBeFalse();
            final.AllowedRegionCodes.ShouldBe(["DE", "CH", "AT", "LU"]);
            final.RequireAdequacyDecision.ShouldBeTrue();
            final.AllowedTransferBases.ShouldBe([TransferLegalBasis.AdequacyDecision, TransferLegalBasis.StandardContractualClauses]);
        });
    }

    [Fact]
    public async Task Policy_MultipleUpdates_AllPersisted()
    {
        // Arrange
        var repo = CreateRepository<ResidencyPolicyAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ResidencyPolicyAggregate.Create(
            id, "personal-data", ["DE"], false, [TransferLegalBasis.ExplicitConsent]);
        await repo.CreateAsync(aggregate);

        // Act — three sequential updates
        for (var i = 0; i < 3; i++)
        {
            var loadRepo = CreateRepository<ResidencyPolicyAggregate>();
            var loaded = (await loadRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
            var regions = new List<string> { "DE", "FR", "NL", "BE", "LU" }.Take(2 + i).ToList();
            loaded.Update(
                regions,
                i >= 2,
                [TransferLegalBasis.ExplicitConsent, TransferLegalBasis.StandardContractualClauses]);
            await loadRepo.SaveAsync(loaded);
        }

        // Assert
        var verifyRepo = CreateRepository<ResidencyPolicyAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.AllowedRegionCodes.Count.ShouldBe(4); // DE, FR, NL, BE
        final.RequireAdequacyDecision.ShouldBeTrue();
    }

    #endregion

    #region DataLocation Aggregate Persistence

    [Fact]
    public async Task Location_RegisterAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<DataLocationAggregate>();
        var id = Guid.NewGuid();
        var storedAt = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, string>
        {
            ["provider"] = "azure",
            ["region-name"] = "West Europe"
        };
        var aggregate = DataLocationAggregate.Register(
            id, "customer-123", "personal-data", "DE",
            StorageType.Primary, storedAt, metadata, "tenant-1", "crm");

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var verifyRepo = CreateRepository<DataLocationAggregate>();
        var loaded = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Id.ShouldBe(id);
        loaded.EntityId.ShouldBe("customer-123");
        loaded.DataCategory.ShouldBe("personal-data");
        loaded.RegionCode.ShouldBe("DE");
        loaded.StorageType.ShouldBe(StorageType.Primary);
        loaded.StoredAtUtc.ShouldBe(storedAt);
        loaded.Metadata.ShouldNotBeNull();
        loaded.Metadata!["provider"].ShouldBe("azure");
        loaded.IsRemoved.ShouldBeFalse();
        loaded.HasViolation.ShouldBeFalse();
        loaded.TenantId.ShouldBe("tenant-1");
        loaded.ModuleId.ShouldBe("crm");
    }

    [Fact]
    public async Task Location_MigrateAndVerify_PersistsRegionChange()
    {
        // Arrange — register location
        var repo = CreateRepository<DataLocationAggregate>();
        var id = Guid.NewGuid();
        var aggregate = DataLocationAggregate.Register(
            id, "order-456", "financial-data", "US",
            StorageType.Primary, DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — migrate DE → FR
        var loadRepo1 = CreateRepository<DataLocationAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.Migrate("DE", "Data sovereignty requirements");
        await loadRepo1.SaveAsync(loaded1);

        // Act — verify location
        var verifiedAt = DateTimeOffset.UtcNow;
        var loadRepo2 = CreateRepository<DataLocationAggregate>();
        var loaded2 = (await loadRepo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Verify(verifiedAt);
        await loadRepo2.SaveAsync(loaded2);

        // Assert
        var verifyRepo = CreateRepository<DataLocationAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.RegionCode.ShouldBe("DE");
        final.LastVerifiedAtUtc.ShouldNotBeNull();
        final.LastVerifiedAtUtc!.Value.ShouldBe(verifiedAt);
    }

    [Fact]
    public async Task Location_ViolationDetectAndResolve_PersistsLifecycle()
    {
        // Arrange — register location
        var repo = CreateRepository<DataLocationAggregate>();
        var id = Guid.NewGuid();
        var aggregate = DataLocationAggregate.Register(
            id, "patient-789", "healthcare-data", "US",
            StorageType.Primary, DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — detect violation
        var loadRepo1 = CreateRepository<DataLocationAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.DetectViolation("healthcare-data", "US", "Healthcare data stored outside EU/EEA without adequacy decision");
        await loadRepo1.SaveAsync(loaded1);

        // Assert — violation active
        var midRepo = CreateRepository<DataLocationAggregate>();
        var mid = (await midRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        mid.HasViolation.ShouldBeTrue();
        mid.ViolationDetails.ShouldBe("Healthcare data stored outside EU/EEA without adequacy decision");

        // Act — resolve violation
        var loadRepo2 = CreateRepository<DataLocationAggregate>();
        var loaded2 = (await loadRepo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.ResolveViolation("Data migrated to DE region, SCC agreement established");
        await loadRepo2.SaveAsync(loaded2);

        // Assert — violation resolved
        var verifyRepo = CreateRepository<DataLocationAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.HasViolation.ShouldBeFalse();
        final.ViolationDetails.ShouldBeNull();
    }

    [Fact]
    public async Task Location_FullLifecycle_RegisterMigrateVerifyRemove()
    {
        // Arrange — register
        var repo = CreateRepository<DataLocationAggregate>();
        var id = Guid.NewGuid();
        var aggregate = DataLocationAggregate.Register(
            id, "employee-101", "employee-data", "US",
            StorageType.Primary, DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — migrate
        var loadRepo1 = CreateRepository<DataLocationAggregate>();
        var loaded1 = (await loadRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.Migrate("DE", "GDPR compliance migration");
        await loadRepo1.SaveAsync(loaded1);

        // Act — verify
        var loadRepo2 = CreateRepository<DataLocationAggregate>();
        var loaded2 = (await loadRepo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Verify(DateTimeOffset.UtcNow);
        await loadRepo2.SaveAsync(loaded2);

        // Act — remove
        var loadRepo3 = CreateRepository<DataLocationAggregate>();
        var loaded3 = (await loadRepo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded3.Remove("Employee data deleted per Art. 17 erasure request");
        await loadRepo3.SaveAsync(loaded3);

        // Assert
        var verifyRepo = CreateRepository<DataLocationAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsRemoved.ShouldBeTrue();
        final.RegionCode.ShouldBe("DE");
    }

    [Fact]
    public async Task Location_MultipleMigrations_TracksRegionHistory()
    {
        // Arrange
        var repo = CreateRepository<DataLocationAggregate>();
        var id = Guid.NewGuid();
        var aggregate = DataLocationAggregate.Register(
            id, "doc-555", "classified-data", "US",
            StorageType.Primary, DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — migrate through 3 regions: US → DE → FR → CH
        string[] destinations = ["DE", "FR", "CH"];
        string[] reasons = ["Initial GDPR migration", "Cross-border consolidation", "Swiss data haven"];
        for (var i = 0; i < destinations.Length; i++)
        {
            var loadRepo = CreateRepository<DataLocationAggregate>();
            var loaded = (await loadRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
            loaded.Migrate(destinations[i], reasons[i]);
            await loadRepo.SaveAsync(loaded);
        }

        // Assert — final region is CH
        var verifyRepo = CreateRepository<DataLocationAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.RegionCode.ShouldBe("CH");
    }

    #endregion

    #region Cross-Aggregate Isolation

    [Fact]
    public async Task PolicyAndLocation_AreIsolated()
    {
        // Arrange — create one policy and one location
        var policyId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        var policyRepo = CreateRepository<ResidencyPolicyAggregate>();
        var locationRepo = CreateRepository<DataLocationAggregate>();

        var policy = ResidencyPolicyAggregate.Create(
            policyId, "biometric-data", ["DE", "FR"], true,
            [TransferLegalBasis.AdequacyDecision]);
        var location = DataLocationAggregate.Register(
            locationId, "user-42", "biometric-data", "DE",
            StorageType.Primary, DateTimeOffset.UtcNow);

        await policyRepo.CreateAsync(policy);
        await locationRepo.CreateAsync(location);

        // Act — load each from fresh repositories
        var verifyPolicy = CreateRepository<ResidencyPolicyAggregate>();
        var verifyLocation = CreateRepository<DataLocationAggregate>();

        var policyResult = await verifyPolicy.LoadAsync(policyId);
        var locationResult = await verifyLocation.LoadAsync(locationId);

        // Assert — each loaded independently
        policyResult.IsRight.ShouldBeTrue();
        locationResult.IsRight.ShouldBeTrue();

        policyResult.IfRight(p => p.DataCategory.ShouldBe("biometric-data"));
        locationResult.IfRight(l => l.EntityId.ShouldBe("user-42"));
    }

    [Fact]
    public async Task LoadNonExistentAggregate_ReturnsLeft()
    {
        // Arrange
        var policyRepo = CreateRepository<ResidencyPolicyAggregate>();
        var locationRepo = CreateRepository<DataLocationAggregate>();

        // Act
        var policyResult = await policyRepo.LoadAsync(Guid.NewGuid());
        var locationResult = await locationRepo.LoadAsync(Guid.NewGuid());

        // Assert
        policyResult.IsLeft.ShouldBeTrue();
        locationResult.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Concurrent Operations

    [Fact]
    public async Task ConcurrentPolicyCreation_AllSucceed()
    {
        // Arrange
        const int concurrentPolicies = 10;
        var ids = Enumerable.Range(0, concurrentPolicies).Select(_ => Guid.NewGuid()).ToArray();

        // Act — create policies concurrently
        var tasks = ids.Select((id, index) => Task.Run(async () =>
        {
            var repo = CreateRepository<ResidencyPolicyAggregate>();
            var aggregate = ResidencyPolicyAggregate.Create(
                id, $"category-{index}", ["DE"], false, [TransferLegalBasis.StandardContractualClauses]);
            return await repo.CreateAsync(aggregate);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert — all creations succeed
        results.ShouldAllBe(r => r.IsRight);

        // Verify all loadable
        foreach (var id in ids)
        {
            var verifyRepo = CreateRepository<ResidencyPolicyAggregate>();
            var loaded = await verifyRepo.LoadAsync(id);
            loaded.IsRight.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ConcurrentLocationRegistration_AllSucceed()
    {
        // Arrange
        const int concurrentLocations = 10;
        var ids = Enumerable.Range(0, concurrentLocations).Select(_ => Guid.NewGuid()).ToArray();

        // Act
        var tasks = ids.Select((id, index) => Task.Run(async () =>
        {
            var repo = CreateRepository<DataLocationAggregate>();
            var aggregate = DataLocationAggregate.Register(
                id, $"entity-{index}", "personal-data", "DE",
                StorageType.Primary, DateTimeOffset.UtcNow);
            return await repo.CreateAsync(aggregate);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(r => r.IsRight);
    }

    #endregion
}
