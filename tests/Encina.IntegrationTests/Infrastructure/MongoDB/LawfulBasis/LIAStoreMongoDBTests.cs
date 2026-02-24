using Encina.Compliance.GDPR;
using Encina.MongoDB.LawfulBasis;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.LawfulBasis;

[Collection("MongoDB")]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class LIAStoreMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public LIAStoreMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private LIAStoreMongoDB CreateStore() =>
        new(_fixture.ConnectionString, MongoDbFixture.DatabaseName);

    private static LIARecord CreateRecord(
        string id = "LIA-001",
        LIAOutcome outcome = LIAOutcome.Approved) => new()
        {
            Id = id,
            Name = "Test LIA",
            Purpose = "Integration testing",
            LegitimateInterest = "Testing interest",
            Benefits = "Better quality",
            ConsequencesIfNotProcessed = "Bugs",
            NecessityJustification = "Required for testing",
            AlternativesConsidered = ["Manual testing"],
            DataMinimisationNotes = "Only test data",
            NatureOfData = "Test identifiers",
            ReasonableExpectations = "Expected by developers",
            ImpactAssessment = "Minimal impact",
            Safeguards = ["Encryption", "Access control"],
            Outcome = outcome,
            Conclusion = "Approved for testing",
            AssessedAtUtc = DateTimeOffset.UtcNow,
            AssessedBy = "Test DPO"
        };

    [Fact]
    public async Task StoreAsync_ValidRecord_ShouldPersist()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var record = CreateRecord();
        var result = await store.StoreAsync(record);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_SameId_ShouldUpsert()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();
        await store.StoreAsync(CreateRecord("LIA-UPS", LIAOutcome.RequiresReview));
        var result = await store.StoreAsync(CreateRecord("LIA-UPS", LIAOutcome.Approved));
        result.IsRight.Should().BeTrue();

        var retrieved = await store.GetByReferenceAsync("LIA-UPS");
        var option = (Option<LIARecord>)retrieved;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Outcome.Should().Be(LIAOutcome.Approved));
    }

    [Fact]
    public async Task GetByReferenceAsync_Stored_ShouldReturnSome()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();
        await store.StoreAsync(CreateRecord("LIA-GET"));

        var result = await store.GetByReferenceAsync("LIA-GET");
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Id.Should().Be("LIA-GET"));
    }

    [Fact]
    public async Task GetByReferenceAsync_NotStored_ShouldReturnNone()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var result = await store.GetByReferenceAsync("NON-EXISTENT");
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingReviewAsync_WithPendingReviews_ShouldReturnOnlyPending()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();
        await store.StoreAsync(CreateRecord("LIA-A01", LIAOutcome.Approved));
        await store.StoreAsync(CreateRecord("LIA-P01", LIAOutcome.RequiresReview));
        await store.StoreAsync(CreateRecord("LIA-P02", LIAOutcome.RequiresReview));

        var result = await store.GetPendingReviewAsync();
        result.IsRight.Should().BeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().HaveCount(2);
        records.Should().AllSatisfy(r => r.Outcome.Should().Be(LIAOutcome.RequiresReview));
    }
}
