using Encina.Compliance.GDPR;
using Encina.EntityFrameworkCore.LawfulBasis;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.LawfulBasis;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.LawfulBasis;

[Collection("EFCore-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Provider", "EFCore.SqlServer")]
public sealed class LIAStoreEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public LIAStoreEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
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
        await using var context = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store = new LIAStoreEF(context);

        var record = CreateRecord();
        var result = await store.StoreAsync(record);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_SameId_ShouldUpsert()
    {
        await using var ctx1 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store1 = new LIAStoreEF(ctx1);
        await store1.StoreAsync(CreateRecord("LIA-UPS", LIAOutcome.RequiresReview));

        await using var ctx2 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store2 = new LIAStoreEF(ctx2);
        var result = await store2.StoreAsync(CreateRecord("LIA-UPS", LIAOutcome.Approved));
        result.IsRight.Should().BeTrue();

        await using var ctx3 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store3 = new LIAStoreEF(ctx3);
        var retrieved = await store3.GetByReferenceAsync("LIA-UPS");
        var option = (Option<LIARecord>)retrieved;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Outcome.Should().Be(LIAOutcome.Approved));
    }

    [Fact]
    public async Task GetByReferenceAsync_Stored_ShouldReturnSome()
    {
        await using var ctx1 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store1 = new LIAStoreEF(ctx1);
        await store1.StoreAsync(CreateRecord("LIA-GET"));

        await using var ctx2 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store2 = new LIAStoreEF(ctx2);
        var result = await store2.GetByReferenceAsync("LIA-GET");
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Id.Should().Be("LIA-GET"));
    }

    [Fact]
    public async Task GetByReferenceAsync_NotStored_ShouldReturnNone()
    {
        await using var context = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store = new LIAStoreEF(context);

        var result = await store.GetByReferenceAsync("NON-EXISTENT");
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingReviewAsync_WithPendingReviews_ShouldReturnOnlyPending()
    {
        await using var ctx1 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store1 = new LIAStoreEF(ctx1);
        await store1.StoreAsync(CreateRecord("LIA-A01", LIAOutcome.Approved));
        await store1.StoreAsync(CreateRecord("LIA-P01", LIAOutcome.RequiresReview));
        await store1.StoreAsync(CreateRecord("LIA-P02", LIAOutcome.RequiresReview));

        await using var ctx2 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store2 = new LIAStoreEF(ctx2);
        var result = await store2.GetPendingReviewAsync();
        result.IsRight.Should().BeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().HaveCount(2);
        records.Should().AllSatisfy(r => r.Outcome.Should().Be(LIAOutcome.RequiresReview));
    }
}
