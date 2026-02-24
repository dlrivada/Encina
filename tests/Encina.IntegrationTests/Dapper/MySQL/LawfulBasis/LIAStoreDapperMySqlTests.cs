using Encina.Dapper.MySQL.LawfulBasis;
using Encina.Compliance.GDPR;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Dapper.MySQL.LawfulBasis;

[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.MySQL")]
public sealed class LIAStoreDapperMySqlTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private LIAStoreDapper _store = null!;

    public LIAStoreDapperMySqlTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _store = new LIAStoreDapper(_fixture.ConnectionString);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
        var record = CreateRecord();
        var result = await _store.StoreAsync(record);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_SameId_ShouldUpsert()
    {
        await _store.StoreAsync(CreateRecord("LIA-UPS", LIAOutcome.RequiresReview));
        var result = await _store.StoreAsync(CreateRecord("LIA-UPS", LIAOutcome.Approved));
        result.IsRight.Should().BeTrue();

        var retrieved = await _store.GetByReferenceAsync("LIA-UPS");
        var option = (Option<LIARecord>)retrieved;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Outcome.Should().Be(LIAOutcome.Approved));
    }

    [Fact]
    public async Task GetByReferenceAsync_Stored_ShouldReturnSome()
    {
        await _store.StoreAsync(CreateRecord("LIA-GET"));
        var result = await _store.GetByReferenceAsync("LIA-GET");
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Id.Should().Be("LIA-GET"));
    }

    [Fact]
    public async Task GetByReferenceAsync_NotStored_ShouldReturnNone()
    {
        var result = await _store.GetByReferenceAsync("NON-EXISTENT");
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingReviewAsync_WithPendingReviews_ShouldReturnOnlyPending()
    {
        await _store.StoreAsync(CreateRecord("LIA-A01", LIAOutcome.Approved));
        await _store.StoreAsync(CreateRecord("LIA-P01", LIAOutcome.RequiresReview));
        await _store.StoreAsync(CreateRecord("LIA-P02", LIAOutcome.RequiresReview));

        var result = await _store.GetPendingReviewAsync();
        result.IsRight.Should().BeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().HaveCount(2);
        records.Should().AllSatisfy(r => r.Outcome.Should().Be(LIAOutcome.RequiresReview));
    }
}
