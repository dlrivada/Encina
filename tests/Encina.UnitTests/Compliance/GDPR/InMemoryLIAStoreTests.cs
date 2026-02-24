using Encina.Compliance.GDPR;
using FluentAssertions;
using LanguageExt;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="InMemoryLIAStore"/>.
/// </summary>
public class InMemoryLIAStoreTests
{
    private readonly InMemoryLIAStore _sut = new();

    private static LIARecord CreateRecord(
        string id = "LIA-001",
        LIAOutcome outcome = LIAOutcome.Approved) => new()
        {
            Id = id,
            Name = "Test LIA",
            Purpose = "Unit testing",
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

    // -- StoreAsync --

    [Fact]
    public async Task StoreAsync_ValidRecord_ShouldSucceed()
    {
        // Arrange
        var record = CreateRecord();

        // Act
        var result = await _sut.StoreAsync(record);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_SameId_ShouldUpsert()
    {
        // Arrange
        var original = CreateRecord("LIA-001", LIAOutcome.RequiresReview);
        var updated = CreateRecord("LIA-001", LIAOutcome.Approved);
        await _sut.StoreAsync(original);

        // Act
        var result = await _sut.StoreAsync(updated);

        // Assert — upsert succeeds
        result.IsRight.Should().BeTrue();

        // Verify the updated record is stored
        var retrieved = await _sut.GetByReferenceAsync("LIA-001");
        var option = (Option<LIARecord>)retrieved;
        option.IsSome.Should().BeTrue();
        option.IfSome(r => r.Outcome.Should().Be(LIAOutcome.Approved));
    }

    [Fact]
    public async Task StoreAsync_NullRecord_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.StoreAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("record");
    }

    [Fact]
    public async Task StoreAsync_MultipleRecords_ShouldStoreAll()
    {
        // Arrange & Act
        await _sut.StoreAsync(CreateRecord("LIA-001"));
        await _sut.StoreAsync(CreateRecord("LIA-002"));
        await _sut.StoreAsync(CreateRecord("LIA-003"));

        // Assert
        var r1 = await _sut.GetByReferenceAsync("LIA-001");
        var r2 = await _sut.GetByReferenceAsync("LIA-002");
        var r3 = await _sut.GetByReferenceAsync("LIA-003");

        ((Option<LIARecord>)r1).IsSome.Should().BeTrue();
        ((Option<LIARecord>)r2).IsSome.Should().BeTrue();
        ((Option<LIARecord>)r3).IsSome.Should().BeTrue();
    }

    // -- GetByReferenceAsync --

    [Fact]
    public async Task GetByReferenceAsync_Stored_ShouldReturnSome()
    {
        // Arrange
        var record = CreateRecord("LIA-FRAUD-001");
        await _sut.StoreAsync(record);

        // Act
        var result = await _sut.GetByReferenceAsync("LIA-FRAUD-001");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(found =>
        {
            found.Id.Should().Be("LIA-FRAUD-001");
            found.Name.Should().Be("Test LIA");
        });
    }

    [Fact]
    public async Task GetByReferenceAsync_NotStored_ShouldReturnNone()
    {
        // Act
        var result = await _sut.GetByReferenceAsync("NON-EXISTENT");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<LIARecord>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetByReferenceAsync_NullReference_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetByReferenceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("liaReference");
    }

    [Fact]
    public async Task GetByReferenceAsync_CaseSensitive_ShouldReturnNoneForDifferentCase()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord("LIA-001"));

        // Act
        var result = await _sut.GetByReferenceAsync("lia-001");

        // Assert — store uses StringComparer.Ordinal (case-sensitive)
        var option = (Option<LIARecord>)result;
        option.IsNone.Should().BeTrue();
    }

    // -- GetPendingReviewAsync --

    [Fact]
    public async Task GetPendingReviewAsync_EmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetPendingReviewAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingReviewAsync_NoPendingReviews_ShouldReturnEmptyList()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord("LIA-001", LIAOutcome.Approved));
        await _sut.StoreAsync(CreateRecord("LIA-002", LIAOutcome.Rejected));

        // Act
        var result = await _sut.GetPendingReviewAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingReviewAsync_WithPendingReviews_ShouldReturnOnlyPending()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord("LIA-001", LIAOutcome.Approved));
        await _sut.StoreAsync(CreateRecord("LIA-002", LIAOutcome.RequiresReview));
        await _sut.StoreAsync(CreateRecord("LIA-003", LIAOutcome.Rejected));
        await _sut.StoreAsync(CreateRecord("LIA-004", LIAOutcome.RequiresReview));

        // Act
        var result = await _sut.GetPendingReviewAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().HaveCount(2);
        records.Should().AllSatisfy(r => r.Outcome.Should().Be(LIAOutcome.RequiresReview));
    }

    [Fact]
    public async Task GetPendingReviewAsync_AfterUpsertToApproved_ShouldNotIncludeUpdatedRecord()
    {
        // Arrange
        await _sut.StoreAsync(CreateRecord("LIA-001", LIAOutcome.RequiresReview));

        // Act — update to Approved
        await _sut.StoreAsync(CreateRecord("LIA-001", LIAOutcome.Approved));
        var result = await _sut.GetPendingReviewAsync();

        // Assert
        var records = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LIARecord>)[]);
        records.Should().BeEmpty();
    }

    // -- Thread safety --

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var storeTasks = Enumerable.Range(0, 50)
            .Select(i => _sut.StoreAsync(CreateRecord($"LIA-{i:D3}")).AsTask());

        // Act & Assert — should not throw
        await Task.WhenAll(storeTasks);

        var all = Enumerable.Range(0, 50)
            .Select(i => _sut.GetByReferenceAsync($"LIA-{i:D3}").AsTask());

        var results = await Task.WhenAll(all);
        results.Should().AllSatisfy(r =>
        {
            r.IsRight.Should().BeTrue();
            ((Option<LIARecord>)r).IsSome.Should().BeTrue();
        });
    }
}
