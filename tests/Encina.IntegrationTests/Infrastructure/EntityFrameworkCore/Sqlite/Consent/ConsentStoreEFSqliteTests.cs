using Encina.Compliance.Consent;
using Encina.EntityFrameworkCore.Consent;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Consent;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Sqlite.Consent;

/// <summary>
/// Integration tests for <see cref="ConsentStoreEF"/> with SQLite.
/// Tests the EF Core implementation of the consent store using a real SQLite in-memory database.
/// </summary>
/// <remarks>
/// The ConsentRecords table is created by <see cref="Encina.TestInfrastructure.Schemas.SqliteSchema.CreateConsentSchemaAsync"/>
/// during fixture initialization. EF Core's <c>EnsureCreatedAsync</c> is NOT used because it
/// is a no-op when the database already has tables from other schemas.
/// </remarks>
[Collection("EFCore-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Provider", "EFCore.Sqlite")]
public sealed class ConsentStoreEFSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;

    public ConsentStoreEFSqliteTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region RecordConsentAsync Tests

    [Fact]
    public async Task RecordConsentAsync_ValidRecord_ShouldSucceed()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
        var record = CreateConsentRecord(
            subjectId: "user-persist",
            purpose: "analytics",
            source: "web-form",
            versionId: "v2");

        // Act
        await store.RecordConsentAsync(record);

        // Assert - use a new context to verify persistence
        await using var verifyContext = _fixture.CreateDbContext<ConsentTestDbContext>();
        var verifyStore = new ConsentStoreEF(verifyContext);
        var getResult = await verifyStore.GetConsentAsync("user-persist", "analytics");
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);

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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);

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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
        var record = CreateConsentRecord(subjectId: "user-withdraw-verify", purpose: "analytics");
        await store.RecordConsentAsync(record);

        // Act
        await store.WithdrawConsentAsync("user-withdraw-verify", "analytics");

        // Assert - use same context since EF tracks changes
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);

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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
        await using var context = _fixture.CreateDbContext<ConsentTestDbContext>();
        var store = new ConsentStoreEF(context);
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
