using Encina.Compliance.Consent;
using Encina.MongoDB;
using Encina.MongoDB.Consent;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Consent;

/// <summary>
/// Integration tests for <see cref="ConsentStoreMongoDB"/> using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ConsentStoreMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public ConsentStoreMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseConsent = true
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var collection = _fixture.Database!.GetCollection<ConsentRecordDocument>(_options.Value.Collections.Consents);
            await collection.DeleteManyAsync(Builders<ConsentRecordDocument>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region RecordConsentAsync Tests

    [Fact]
    public async Task RecordConsentAsync_ValidRecord_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord();

        // Act
        var result = await store.RecordConsentAsync(record);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordConsentAsync_ShouldPersistAllProperties()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord(
            subjectId: "user-persist",
            purpose: "analytics",
            source: "web-form",
            versionId: "v2");

        // Act
        await store.RecordConsentAsync(record);

        // Assert
        var getResult = await store.GetConsentAsync("user-persist", "analytics");
        getResult.IsRight.ShouldBeTrue();
        var opt = (Option<ConsentRecord>)getResult;
        opt.IsSome.ShouldBeTrue();
        var retrieved = (ConsentRecord)opt;
        retrieved.SubjectId.ShouldBe("user-persist");
        retrieved.Purpose.ShouldBe("analytics");
        retrieved.Source.ShouldBe("web-form");
        retrieved.ConsentVersionId.ShouldBe("v2");
        retrieved.Status.ShouldBe(ConsentStatus.Active);
    }

    #endregion

    #region GetConsentAsync Tests

    [Fact]
    public async Task GetConsentAsync_ExistingRecord_ShouldReturnSome()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord(subjectId: "user-get", purpose: "marketing");
        await store.RecordConsentAsync(record);

        // Act
        var result = await store.GetConsentAsync("user-get", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task GetConsentAsync_NonExistingRecord_ShouldReturnNone()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetConsentAsync("non-existent-user", "non-existent-purpose");

        // Assert
        result.IsRight.ShouldBeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetAllConsentsAsync Tests

    [Fact]
    public async Task GetAllConsentsAsync_WithMultipleRecords_ShouldReturnAll()
    {
        // Arrange
        var store = CreateStore();
        var subjectId = "user-multi";
        await store.RecordConsentAsync(CreateConsentRecord(subjectId: subjectId, purpose: "marketing"));
        await store.RecordConsentAsync(CreateConsentRecord(subjectId: subjectId, purpose: "analytics"));

        // Act
        var result = await store.GetAllConsentsAsync(subjectId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var records = result.Match<IReadOnlyList<ConsentRecord>>(Right: r => r, Left: _ => []);
        records.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllConsentsAsync_NoRecords_ShouldReturnEmptyList()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetAllConsentsAsync("no-records-user");

        // Assert
        result.IsRight.ShouldBeTrue();
        var records = result.Match<IReadOnlyList<ConsentRecord>>(Right: r => r, Left: _ => []);
        records.Count.ShouldBe(0);
    }

    #endregion

    #region WithdrawConsentAsync Tests

    [Fact]
    public async Task WithdrawConsentAsync_ActiveConsent_ShouldSucceed()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord(subjectId: "user-withdraw", purpose: "marketing");
        await store.RecordConsentAsync(record);

        // Act
        var result = await store.WithdrawConsentAsync("user-withdraw", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task WithdrawConsentAsync_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord(subjectId: "user-withdraw-verify", purpose: "analytics");
        await store.RecordConsentAsync(record);

        // Act
        await store.WithdrawConsentAsync("user-withdraw-verify", "analytics");

        // Assert
        var getResult = await store.GetConsentAsync("user-withdraw-verify", "analytics");
        var opt = (Option<ConsentRecord>)getResult;
        var retrieved = (ConsentRecord)opt;
        retrieved.Status.ShouldBe(ConsentStatus.Withdrawn);
        retrieved.WithdrawnAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region HasValidConsentAsync Tests

    [Fact]
    public async Task HasValidConsentAsync_ActiveConsent_ShouldReturnTrue()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord(subjectId: "user-valid", purpose: "marketing");
        await store.RecordConsentAsync(record);

        // Act
        var result = await store.HasValidConsentAsync("user-valid", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeTrue();
    }

    [Fact]
    public async Task HasValidConsentAsync_WithdrawnConsent_ShouldReturnFalse()
    {
        // Arrange
        var store = CreateStore();
        var record = CreateConsentRecord(subjectId: "user-invalid", purpose: "marketing");
        await store.RecordConsentAsync(record);
        await store.WithdrawConsentAsync("user-invalid", "marketing");

        // Act
        var result = await store.HasValidConsentAsync("user-invalid", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse();
    }

    [Fact]
    public async Task HasValidConsentAsync_NoConsent_ShouldReturnFalse()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.HasValidConsentAsync("no-consent-user", "marketing");

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse();
    }

    #endregion

    #region BulkRecordConsentAsync Tests

    [Fact]
    public async Task BulkRecordConsentAsync_MultipleRecords_ShouldAllSucceed()
    {
        // Arrange
        var store = CreateStore();
        var records = Enumerable.Range(0, 5)
            .Select(i => CreateConsentRecord(subjectId: $"bulk-user-{i}", purpose: "marketing"))
            .ToList();

        // Act
        var result = await store.BulkRecordConsentAsync(records);

        // Assert
        result.IsRight.ShouldBeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.ShouldBe(5);
        bulkResult.AllSucceeded.ShouldBeTrue();
    }

    #endregion

    #region BulkWithdrawConsentAsync Tests

    [Fact]
    public async Task BulkWithdrawConsentAsync_MultiplePurposes_ShouldWithdrawAll()
    {
        // Arrange
        var store = CreateStore();
        var subjectId = "bulk-withdraw-user";
        await store.RecordConsentAsync(CreateConsentRecord(subjectId: subjectId, purpose: "marketing"));
        await store.RecordConsentAsync(CreateConsentRecord(subjectId: subjectId, purpose: "analytics"));

        // Act
        var result = await store.BulkWithdrawConsentAsync(subjectId, ["marketing", "analytics"]);

        // Assert
        result.IsRight.ShouldBeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.ShouldBe(2);
    }

    #endregion

    #region Full Lifecycle Test

    [Fact]
    public async Task FullLifecycle_RecordCheckWithdrawCheck_ShouldWork()
    {
        // Arrange
        var store = CreateStore();
        var subjectId = "lifecycle-user";
        var purpose = "marketing";
        var record = CreateConsentRecord(subjectId: subjectId, purpose: purpose);

        // Act & Assert: Record consent
        var recordResult = await store.RecordConsentAsync(record);
        recordResult.IsRight.ShouldBeTrue();

        // Act & Assert: Verify active
        var hasValidBefore = await store.HasValidConsentAsync(subjectId, purpose);
        ((bool)hasValidBefore).ShouldBeTrue();

        // Act & Assert: Withdraw
        var withdrawResult = await store.WithdrawConsentAsync(subjectId, purpose);
        withdrawResult.IsRight.ShouldBeTrue();

        // Act & Assert: Verify withdrawn
        var hasValidAfter = await store.HasValidConsentAsync(subjectId, purpose);
        ((bool)hasValidAfter).ShouldBeFalse();

        // Act & Assert: Verify status
        var getResult = await store.GetConsentAsync(subjectId, purpose);
        var opt = (Option<ConsentRecord>)getResult;
        var retrieved = (ConsentRecord)opt;
        retrieved.Status.ShouldBe(ConsentStatus.Withdrawn);
        retrieved.WithdrawnAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Helpers

    private ConsentStoreMongoDB CreateStore()
    {
        return new ConsentStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<ConsentStoreMongoDB>.Instance);
    }

    private static ConsentRecord CreateConsentRecord(
        string subjectId = "user-1",
        string purpose = "marketing",
        ConsentStatus status = ConsentStatus.Active,
        string versionId = "v1",
        string source = "test")
    {
        return new ConsentRecord
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Purpose = purpose,
            Status = status,
            ConsentVersionId = versionId,
            GivenAtUtc = DateTimeOffset.UtcNow,
            WithdrawnAtUtc = null,
            ExpiresAtUtc = null,
            Source = source,
            Metadata = new Dictionary<string, object?>()
        };
    }

    #endregion
}
