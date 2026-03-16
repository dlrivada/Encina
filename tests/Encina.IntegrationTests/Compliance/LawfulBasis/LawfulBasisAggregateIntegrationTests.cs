using Encina.Compliance.LawfulBasis.Aggregates;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Options;

using Shouldly;

using GDPR = Encina.Compliance.GDPR;

namespace Encina.IntegrationTests.Compliance.LawfulBasis;

/// <summary>
/// Integration tests for <see cref="LawfulBasisAggregate"/> and <see cref="LIAAggregate"/>
/// persisted via Marten event sourcing against real PostgreSQL.
/// Verifies event store persistence, aggregate loading, state reconstruction, and lifecycle transitions.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class LawfulBasisAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public LawfulBasisAggregateIntegrationTests(MartenFixture fixture)
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

    #region LawfulBasisAggregate Persistence

    [Fact]
    public async Task RegisterAsync_ShouldPersistAggregate()
    {
        // Arrange
        var repo = CreateRepository<LawfulBasisAggregate>();
        var id = Guid.NewGuid();
        var aggregate = LawfulBasisAggregate.Register(
            id, "MyApp.Commands.ProcessOrder", GDPR.LawfulBasis.Contract,
            "Order fulfillment", null, null, "CONTRACT-001",
            DateTimeOffset.UtcNow, "tenant-1", "orders");

        // Act
        var result = await repo.CreateAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_ShouldReconstructState()
    {
        // Arrange
        var repo = CreateRepository<LawfulBasisAggregate>();
        var id = Guid.NewGuid();
        var aggregate = LawfulBasisAggregate.Register(
            id, "MyApp.Commands.SendNewsletter", GDPR.LawfulBasis.Consent,
            "Marketing communications", null, null, null,
            DateTimeOffset.UtcNow, "tenant-1", "marketing");
        await repo.CreateAsync(aggregate);

        // Act
        var loadRepo = CreateRepository<LawfulBasisAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);

        // Assert
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.RequestTypeName.ShouldBe("MyApp.Commands.SendNewsletter");
            loaded.Basis.ShouldBe(GDPR.LawfulBasis.Consent);
            loaded.Purpose.ShouldBe("Marketing communications");
            loaded.IsRevoked.ShouldBeFalse();
            loaded.TenantId.ShouldBe("tenant-1");
            loaded.ModuleId.ShouldBe("marketing");
        });
    }

    [Fact]
    public async Task ChangeBasis_ShouldPersistAndReload()
    {
        // Arrange
        var repo = CreateRepository<LawfulBasisAggregate>();
        var id = Guid.NewGuid();
        var aggregate = LawfulBasisAggregate.Register(
            id, "MyApp.Commands.DetectFraud", GDPR.LawfulBasis.Consent,
            "Fraud detection", null, null, null, DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — change basis
        var repo2 = CreateRepository<LawfulBasisAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.ChangeBasis(
            GDPR.LawfulBasis.LegitimateInterests, "Fraud prevention",
            "LIA-FRAUD-001", null, null, DateTimeOffset.UtcNow);
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<LawfulBasisAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Basis.ShouldBe(GDPR.LawfulBasis.LegitimateInterests);
        final.Purpose.ShouldBe("Fraud prevention");
        final.LIAReference.ShouldBe("LIA-FRAUD-001");
        final.IsRevoked.ShouldBeFalse();
        final.RequestTypeName.ShouldBe("MyApp.Commands.DetectFraud");
    }

    [Fact]
    public async Task Revoke_ShouldPersistTerminalState()
    {
        // Arrange
        var repo = CreateRepository<LawfulBasisAggregate>();
        var id = Guid.NewGuid();
        var aggregate = LawfulBasisAggregate.Register(
            id, "MyApp.Commands.TrackUser", GDPR.LawfulBasis.Consent,
            "User tracking", null, null, null, DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — revoke
        var repo2 = CreateRepository<LawfulBasisAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Revoke("Consent withdrawn by user", DateTimeOffset.UtcNow);
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<LawfulBasisAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.IsRevoked.ShouldBeTrue();
        final.RevocationReason.ShouldBe("Consent withdrawn by user");
    }

    [Fact]
    public async Task FullLifecycle_RegisterChangeBasisRevoke_ShouldReconstructCorrectly()
    {
        // Arrange — register
        var repo = CreateRepository<LawfulBasisAggregate>();
        var id = Guid.NewGuid();
        var aggregate = LawfulBasisAggregate.Register(
            id, "MyApp.Commands.ProcessPayment", GDPR.LawfulBasis.Consent,
            "Payment processing", null, null, null,
            DateTimeOffset.UtcNow, "tenant-1", "payments");
        await repo.CreateAsync(aggregate);

        // Act — change basis
        var repo2 = CreateRepository<LawfulBasisAggregate>();
        var loaded1 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.ChangeBasis(
            GDPR.LawfulBasis.Contract, "Payment contract fulfillment",
            null, null, "CONTRACT-PAY-001", DateTimeOffset.UtcNow);
        await repo2.SaveAsync(loaded1);

        // Act — revoke
        var repo3 = CreateRepository<LawfulBasisAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Revoke("Service discontinued", DateTimeOffset.UtcNow);
        await repo3.SaveAsync(loaded2);

        // Assert — final state from fresh load
        var verifyRepo = CreateRepository<LawfulBasisAggregate>();
        var finalResult = await verifyRepo.LoadAsync(id);

        finalResult.IsRight.ShouldBeTrue();
        finalResult.IfRight(final =>
        {
            final.Id.ShouldBe(id);
            final.RequestTypeName.ShouldBe("MyApp.Commands.ProcessPayment");
            final.Basis.ShouldBe(GDPR.LawfulBasis.Contract);
            final.Purpose.ShouldBe("Payment contract fulfillment");
            final.ContractReference.ShouldBe("CONTRACT-PAY-001");
            final.IsRevoked.ShouldBeTrue();
            final.RevocationReason.ShouldBe("Service discontinued");
            final.TenantId.ShouldBe("tenant-1");
            final.ModuleId.ShouldBe("payments");
        });
    }

    #endregion

    #region LIAAggregate Persistence

    [Fact]
    public async Task CreateLIA_ShouldPersistAggregate()
    {
        // Arrange
        var repo = CreateRepository<LIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = CreateTestLIA(id, "LIA-2024-FRAUD-001");

        // Act
        var result = await repo.CreateAsync(aggregate);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ApproveLIA_ShouldPersistAndReload()
    {
        // Arrange
        var repo = CreateRepository<LIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = CreateTestLIA(id, "LIA-2024-APPROVE-001");
        await repo.CreateAsync(aggregate);

        // Act — approve
        var repo2 = CreateRepository<LIAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Approve("Legitimate interest outweighs data subject rights", "dpo-1", DateTimeOffset.UtcNow);
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<LIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Outcome.ShouldBe(GDPR.LIAOutcome.Approved);
        final.Conclusion.ShouldBe("Legitimate interest outweighs data subject rights");
        final.Reference.ShouldBe("LIA-2024-APPROVE-001");
    }

    [Fact]
    public async Task RejectLIA_ShouldPersistAndReload()
    {
        // Arrange
        var repo = CreateRepository<LIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = CreateTestLIA(id, "LIA-2024-REJECT-001");
        await repo.CreateAsync(aggregate);

        // Act — reject
        var repo2 = CreateRepository<LIAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Reject("Data subject rights override legitimate interest", "dpo-1", DateTimeOffset.UtcNow);
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<LIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Outcome.ShouldBe(GDPR.LIAOutcome.Rejected);
        final.Conclusion.ShouldBe("Data subject rights override legitimate interest");
    }

    [Fact]
    public async Task ScheduleReview_ShouldPersistAndReload()
    {
        // Arrange — create and approve
        var repo = CreateRepository<LIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = CreateTestLIA(id, "LIA-2024-REVIEW-001");
        aggregate.Approve("Approved for processing", "dpo-1", DateTimeOffset.UtcNow);
        await repo.CreateAsync(aggregate);

        // Act — schedule review
        var nextReview = DateTimeOffset.UtcNow.AddMonths(6);
        var repo2 = CreateRepository<LIAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.ScheduleReview(nextReview, "compliance-officer", DateTimeOffset.UtcNow);
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<LIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Outcome.ShouldBe(GDPR.LIAOutcome.Approved);
        final.NextReviewAtUtc.ShouldNotBeNull();
        final.NextReviewAtUtc!.Value.ShouldBe(nextReview);
    }

    #endregion

    #region Helpers

    private static LIAAggregate CreateTestLIA(Guid id, string reference)
    {
        return LIAAggregate.Create(
            id,
            reference,
            "Fraud Detection Assessment",
            "Fraud prevention processing",
            "Preventing fraudulent transactions to protect customers",
            "Reduced fraud losses, increased customer trust",
            "Increased exposure to fraud, financial losses",
            "Processing is strictly necessary for real-time fraud detection",
            ["Manual review", "Rule-based systems"],
            "Only transaction metadata is processed, no special categories",
            "Transaction amounts, IP addresses, device fingerprints",
            "Customers expect their bank to protect against fraud",
            "Minimal impact with pseudonymization and access controls",
            ["Pseudonymization", "Role-based access", "90-day retention"],
            "Data Protection Officer",
            dpoInvolvement: true,
            DateTimeOffset.UtcNow);
    }

    #endregion
}
