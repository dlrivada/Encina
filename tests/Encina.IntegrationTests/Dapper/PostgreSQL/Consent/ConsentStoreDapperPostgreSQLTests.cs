using Encina.Dapper.PostgreSQL.Consent;
using Encina.Compliance.Consent;
using Encina.TestInfrastructure.Fixtures;

using LanguageExt;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.Consent;

/// <summary>
/// Integration tests for <see cref="ConsentStoreDapper"/> with PostgreSQL.
/// Tests the Dapper implementation of the consent store using a real PostgreSQL database via Testcontainers.
/// </summary>
[Collection("Dapper-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.PostgreSQL")]
public sealed class ConsentStoreDapperPostgreSQLTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private ConsentStoreDapper _store = null!;

    public ConsentStoreDapperPostgreSQLTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _store = new ConsentStoreDapper(_fixture.CreateConnection());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region RecordConsentAsync Tests

    [Fact]
    public async Task RecordConsentAsync_ValidRecord_ShouldSucceed()
    {
        var record = CreateConsentRecord();
        var result = await _store.RecordConsentAsync(record);
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordConsentAsync_ShouldPersistAllProperties()
    {
        var record = CreateConsentRecord(
            subjectId: "user-persist",
            purpose: "analytics",
            source: "web-form",
            versionId: "v2");

        await _store.RecordConsentAsync(record);

        var getResult = await _store.GetConsentAsync("user-persist", "analytics");
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
        var record = CreateConsentRecord(subjectId: "user-get", purpose: "marketing");
        await _store.RecordConsentAsync(record);

        var result = await _store.GetConsentAsync("user-get", "marketing");
        result.IsRight.ShouldBeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task GetConsentAsync_NonExistingRecord_ShouldReturnNone()
    {
        var result = await _store.GetConsentAsync("nonexistent-user", "marketing");
        result.IsRight.ShouldBeTrue();
        var opt = (Option<ConsentRecord>)result;
        opt.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetAllConsentsAsync Tests

    [Fact]
    public async Task GetAllConsentsAsync_MultipleRecords_ShouldReturnAll()
    {
        var record1 = CreateConsentRecord(subjectId: "user-all", purpose: "marketing");
        var record2 = CreateConsentRecord(subjectId: "user-all", purpose: "analytics");
        await _store.RecordConsentAsync(record1);
        await _store.RecordConsentAsync(record2);

        var result = await _store.GetAllConsentsAsync("user-all");
        result.IsRight.ShouldBeTrue();
        var consents = result.Match<IReadOnlyList<ConsentRecord>>(Right: r => r, Left: _ => []);
        consents.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllConsentsAsync_NoRecords_ShouldReturnEmptyList()
    {
        var result = await _store.GetAllConsentsAsync("empty-user");
        result.IsRight.ShouldBeTrue();
        var consents = result.Match<IReadOnlyList<ConsentRecord>>(Right: r => r, Left: _ => []);
        consents.Count.ShouldBe(0);
    }

    #endregion

    #region WithdrawConsentAsync Tests

    [Fact]
    public async Task WithdrawConsentAsync_ActiveConsent_ShouldSucceed()
    {
        var record = CreateConsentRecord(subjectId: "user-withdraw", purpose: "marketing");
        await _store.RecordConsentAsync(record);

        var result = await _store.WithdrawConsentAsync("user-withdraw", "marketing");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task WithdrawConsentAsync_ShouldUpdateStatusAndTimestamp()
    {
        var record = CreateConsentRecord(subjectId: "user-withdraw-check", purpose: "marketing");
        await _store.RecordConsentAsync(record);

        await _store.WithdrawConsentAsync("user-withdraw-check", "marketing");

        var getResult = await _store.GetConsentAsync("user-withdraw-check", "marketing");
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
        var record = CreateConsentRecord(subjectId: "user-valid", purpose: "marketing");
        await _store.RecordConsentAsync(record);

        var result = await _store.HasValidConsentAsync("user-valid", "marketing");
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeTrue();
    }

    [Fact]
    public async Task HasValidConsentAsync_WithdrawnConsent_ShouldReturnFalse()
    {
        var record = CreateConsentRecord(subjectId: "user-invalid", purpose: "marketing");
        await _store.RecordConsentAsync(record);
        await _store.WithdrawConsentAsync("user-invalid", "marketing");

        var result = await _store.HasValidConsentAsync("user-invalid", "marketing");
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse();
    }

    [Fact]
    public async Task HasValidConsentAsync_NoConsent_ShouldReturnFalse()
    {
        var result = await _store.HasValidConsentAsync("no-consent-user", "marketing");
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse();
    }

    #endregion

    #region BulkRecordConsentAsync Tests

    [Fact]
    public async Task BulkRecordConsentAsync_MultipleRecords_ShouldAllSucceed()
    {
        var records = Enumerable.Range(0, 5)
            .Select(i => CreateConsentRecord(subjectId: $"bulk-user-{i}", purpose: "marketing"))
            .ToList();

        var result = await _store.BulkRecordConsentAsync(records);
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
        var subjectId = "bulk-withdraw-user";
        await _store.RecordConsentAsync(CreateConsentRecord(subjectId: subjectId, purpose: "marketing"));
        await _store.RecordConsentAsync(CreateConsentRecord(subjectId: subjectId, purpose: "analytics"));

        var result = await _store.BulkWithdrawConsentAsync(subjectId, ["marketing", "analytics"]);
        result.IsRight.ShouldBeTrue();
        var bulkResult = (BulkOperationResult)result;
        bulkResult.SuccessCount.ShouldBe(2);
    }

    #endregion

    #region Full Lifecycle Test

    [Fact]
    public async Task FullLifecycle_RecordCheckWithdrawCheck_ShouldWork()
    {
        var subjectId = "lifecycle-user";
        var purpose = "marketing";
        var record = CreateConsentRecord(subjectId: subjectId, purpose: purpose);

        var recordResult = await _store.RecordConsentAsync(record);
        recordResult.IsRight.ShouldBeTrue();

        var hasValidBefore = await _store.HasValidConsentAsync(subjectId, purpose);
        ((bool)hasValidBefore).ShouldBeTrue();

        var withdrawResult = await _store.WithdrawConsentAsync(subjectId, purpose);
        withdrawResult.IsRight.ShouldBeTrue();

        var hasValidAfter = await _store.HasValidConsentAsync(subjectId, purpose);
        ((bool)hasValidAfter).ShouldBeFalse();

        var getResult = await _store.GetConsentAsync(subjectId, purpose);
        var opt = (Option<ConsentRecord>)getResult;
        var retrieved = (ConsentRecord)opt;
        retrieved.Status.ShouldBe(ConsentStatus.Withdrawn);
        retrieved.WithdrawnAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Helpers

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
