using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.MongoDB;
using Encina.MongoDB.DPIA;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.IntegrationTests.Compliance.DPIA.MongoDB;

/// <summary>
/// Integration tests for <see cref="DPIAStoreMongoDB"/> and <see cref="DPIAAuditStoreMongoDB"/>
/// using real MongoDB via Testcontainers.
/// </summary>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class DPIAStoreMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public DPIAStoreMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = MongoDbFixture.DatabaseName,
            UseDPIA = true
        });
    }

    public async ValueTask InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            var assessmentCollection = _fixture.Database!.GetCollection<DPIAAssessmentDocument>(
                _options.Value.Collections.DPIAAssessments);
            await assessmentCollection.DeleteManyAsync(Builders<DPIAAssessmentDocument>.Filter.Empty);

            var auditCollection = _fixture.Database!.GetCollection<DPIAAuditEntryDocument>(
                _options.Value.Collections.DPIAAuditEntries);
            await auditCollection.DeleteManyAsync(Builders<DPIAAuditEntryDocument>.Filter.Empty);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region SaveAssessmentAsync Tests

    [Fact]
    public async Task SaveAssessmentAsync_ValidAssessment_ShouldSucceed()
    {
        // Arrange
        var store = CreateDPIAStore();
        var assessment = CreateAssessment();

        // Act
        var result = await store.SaveAssessmentAsync(assessment);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAssessmentAsync_ShouldPersistAllProperties()
    {
        // Arrange
        var store = CreateDPIAStore();
        var assessment = CreateAssessment(
            requestTypeName: "MyNamespace.ProcessDataCommand",
            status: DPIAAssessmentStatus.InReview,
            processingType: "AutomatedDecisionMaking",
            reason: "High risk biometric processing");

        // Act
        await store.SaveAssessmentAsync(assessment);

        // Assert
        var getResult = await store.GetAssessmentByIdAsync(assessment.Id);
        getResult.IsRight.ShouldBeTrue();
        var opt = (Option<DPIAAssessment>)getResult;
        opt.IsSome.ShouldBeTrue();
        var retrieved = (DPIAAssessment)opt;
        retrieved.RequestTypeName.ShouldBe("MyNamespace.ProcessDataCommand");
        retrieved.Status.ShouldBe(DPIAAssessmentStatus.InReview);
        retrieved.ProcessingType.ShouldBe("AutomatedDecisionMaking");
        retrieved.Reason.ShouldBe("High risk biometric processing");
    }

    [Fact]
    public async Task SaveAssessmentAsync_ExistingId_ShouldUpsert()
    {
        // Arrange
        var store = CreateDPIAStore();
        var id = Guid.NewGuid();
        var original = CreateAssessment(id: id, status: DPIAAssessmentStatus.Draft);
        await store.SaveAssessmentAsync(original);

        var updated = CreateAssessment(
            id: id,
            status: DPIAAssessmentStatus.Approved,
            approvedAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = await store.SaveAssessmentAsync(updated);

        // Assert
        result.IsRight.ShouldBeTrue();
        var getResult = await store.GetAssessmentByIdAsync(id);
        var opt = (Option<DPIAAssessment>)getResult;
        var retrieved = (DPIAAssessment)opt;
        retrieved.Status.ShouldBe(DPIAAssessmentStatus.Approved);
    }

    [Fact]
    public async Task SaveAssessmentAsync_WithResult_ShouldPersistResultJson()
    {
        // Arrange
        var store = CreateDPIAStore();
        var result = new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Security", RiskLevel.High, "Data breach risk", "Apply encryption"), new RiskItem("Privacy", RiskLevel.Medium, "Re-identification risk", "Anonymize data")],
            ProposedMitigations = [new Mitigation("Encryption", "Technical", false, null), new Mitigation("Access control", "Organizational", false, null)],
            RequiresPriorConsultation = true,
            AssessedAtUtc = DateTimeOffset.UtcNow,
            AssessedBy = "dpo@company.com"
        };
        var assessment = CreateAssessment(result: result);

        // Act
        await store.SaveAssessmentAsync(assessment);

        // Assert
        var getResult = await store.GetAssessmentByIdAsync(assessment.Id);
        var opt = (Option<DPIAAssessment>)getResult;
        var retrieved = (DPIAAssessment)opt;
        retrieved.Result.ShouldNotBeNull();
        retrieved.Result.OverallRisk.ShouldBe(RiskLevel.High);
        retrieved.Result.IdentifiedRisks.Count.ShouldBe(2);
        retrieved.Result.ProposedMitigations.Count.ShouldBe(2);
        retrieved.Result.RequiresPriorConsultation.ShouldBeTrue();
        retrieved.Result.IsAcceptable.ShouldBeFalse();
    }

    #endregion

    #region GetAssessmentAsync Tests

    [Fact]
    public async Task GetAssessmentAsync_ExistingRequestType_ShouldReturnSome()
    {
        // Arrange
        var store = CreateDPIAStore();
        var assessment = CreateAssessment(requestTypeName: "Namespace.GetRequest");
        await store.SaveAssessmentAsync(assessment);

        // Act
        var result = await store.GetAssessmentAsync("Namespace.GetRequest");

        // Assert
        result.IsRight.ShouldBeTrue();
        var opt = (Option<DPIAAssessment>)result;
        opt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAssessmentAsync_NonExistingRequestType_ShouldReturnNone()
    {
        // Arrange
        var store = CreateDPIAStore();

        // Act
        var result = await store.GetAssessmentAsync("NonExistent.RequestType");

        // Assert
        result.IsRight.ShouldBeTrue();
        var opt = (Option<DPIAAssessment>)result;
        opt.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetAssessmentByIdAsync Tests

    [Fact]
    public async Task GetAssessmentByIdAsync_ExistingId_ShouldReturnSome()
    {
        // Arrange
        var store = CreateDPIAStore();
        var assessment = CreateAssessment();
        await store.SaveAssessmentAsync(assessment);

        // Act
        var result = await store.GetAssessmentByIdAsync(assessment.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        var opt = (Option<DPIAAssessment>)result;
        opt.IsSome.ShouldBeTrue();
        var retrieved = (DPIAAssessment)opt;
        retrieved.Id.ShouldBe(assessment.Id);
    }

    [Fact]
    public async Task GetAssessmentByIdAsync_NonExistingId_ShouldReturnNone()
    {
        // Arrange
        var store = CreateDPIAStore();

        // Act
        var result = await store.GetAssessmentByIdAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
        var opt = (Option<DPIAAssessment>)result;
        opt.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetExpiredAssessmentsAsync Tests

    [Fact]
    public async Task GetExpiredAssessmentsAsync_WithExpiredAssessment_ShouldReturn()
    {
        // Arrange
        var store = CreateDPIAStore();
        var pastReview = DateTimeOffset.UtcNow.AddDays(-7);
        var assessment = CreateAssessment(
            status: DPIAAssessmentStatus.Approved,
            approvedAtUtc: DateTimeOffset.UtcNow.AddMonths(-6),
            nextReviewAtUtc: pastReview);
        await store.SaveAssessmentAsync(assessment);

        // Act
        var result = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessments = result.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        assessments.Count.ShouldBeGreaterThanOrEqualTo(1);
        assessments.ShouldContain(a => a.Id == assessment.Id);
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_WithFutureReview_ShouldNotReturn()
    {
        // Arrange
        var store = CreateDPIAStore();
        var futureReview = DateTimeOffset.UtcNow.AddDays(30);
        var assessment = CreateAssessment(
            status: DPIAAssessmentStatus.Approved,
            approvedAtUtc: DateTimeOffset.UtcNow,
            nextReviewAtUtc: futureReview);
        await store.SaveAssessmentAsync(assessment);

        // Act
        var result = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessments = result.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        assessments.ShouldNotContain(a => a.Id == assessment.Id);
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_DraftStatus_ShouldNotReturn()
    {
        // Arrange
        var store = CreateDPIAStore();
        var assessment = CreateAssessment(
            status: DPIAAssessmentStatus.Draft,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(-1));
        await store.SaveAssessmentAsync(assessment);

        // Act
        var result = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessments = result.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        assessments.ShouldNotContain(a => a.Id == assessment.Id);
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_NoExpired_ShouldReturnEmptyList()
    {
        // Arrange
        var store = CreateDPIAStore();

        // Act
        var result = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessments = result.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        assessments.Count.ShouldBe(0);
    }

    #endregion

    #region GetAllAssessmentsAsync Tests

    [Fact]
    public async Task GetAllAssessmentsAsync_MultipleAssessments_ShouldReturnAll()
    {
        // Arrange
        var store = CreateDPIAStore();
        await store.SaveAssessmentAsync(CreateAssessment(requestTypeName: "Type.A"));
        await store.SaveAssessmentAsync(CreateAssessment(requestTypeName: "Type.B"));
        await store.SaveAssessmentAsync(CreateAssessment(requestTypeName: "Type.C"));

        // Act
        var result = await store.GetAllAssessmentsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessments = result.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        assessments.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllAssessmentsAsync_NoAssessments_ShouldReturnEmptyList()
    {
        // Arrange
        var store = CreateDPIAStore();

        // Act
        var result = await store.GetAllAssessmentsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessments = result.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        assessments.Count.ShouldBe(0);
    }

    #endregion

    #region DeleteAssessmentAsync Tests

    [Fact]
    public async Task DeleteAssessmentAsync_ExistingAssessment_ShouldSucceed()
    {
        // Arrange
        var store = CreateDPIAStore();
        var assessment = CreateAssessment();
        await store.SaveAssessmentAsync(assessment);

        // Act
        var result = await store.DeleteAssessmentAsync(assessment.Id);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify deletion
        var getResult = await store.GetAssessmentByIdAsync(assessment.Id);
        var opt = (Option<DPIAAssessment>)getResult;
        opt.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAssessmentAsync_NonExistingId_ShouldReturnError()
    {
        // Arrange
        var store = CreateDPIAStore();

        // Act
        var result = await store.DeleteAssessmentAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DPIAAuditStoreMongoDB - RecordAuditEntryAsync Tests

    [Fact]
    public async Task RecordAuditEntryAsync_ValidEntry_ShouldSucceed()
    {
        // Arrange
        var auditStore = CreateAuditStore();
        var entry = CreateAuditEntry();

        // Act
        var result = await auditStore.RecordAuditEntryAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RecordAuditEntryAsync_ShouldPersistAllProperties()
    {
        // Arrange
        var auditStore = CreateAuditStore();
        var assessmentId = Guid.NewGuid();
        var entry = CreateAuditEntry(
            assessmentId: assessmentId,
            action: "Approved",
            performedBy: "dpo@company.com",
            details: "Assessment approved after DPO review");

        // Act
        await auditStore.RecordAuditEntryAsync(entry);

        // Assert
        var trailResult = await auditStore.GetAuditTrailAsync(assessmentId);
        trailResult.IsRight.ShouldBeTrue();
        var entries = trailResult.Match<IReadOnlyList<DPIAAuditEntry>>(Right: r => r, Left: _ => []);
        entries.Count.ShouldBe(1);
        entries[0].Action.ShouldBe("Approved");
        entries[0].PerformedBy.ShouldBe("dpo@company.com");
        entries[0].Details.ShouldBe("Assessment approved after DPO review");
    }

    #endregion

    #region DPIAAuditStoreMongoDB - GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_MultipleEntries_ShouldReturnChronologically()
    {
        // Arrange
        var auditStore = CreateAuditStore();
        var assessmentId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow;

        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(
            assessmentId: assessmentId,
            action: "Created",
            occurredAtUtc: baseTime.AddMinutes(-10)));
        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(
            assessmentId: assessmentId,
            action: "SubmittedForReview",
            occurredAtUtc: baseTime.AddMinutes(-5)));
        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(
            assessmentId: assessmentId,
            action: "Approved",
            occurredAtUtc: baseTime));

        // Act
        var result = await auditStore.GetAuditTrailAsync(assessmentId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var entries = result.Match<IReadOnlyList<DPIAAuditEntry>>(Right: r => r, Left: _ => []);
        entries.Count.ShouldBe(3);
        entries[0].Action.ShouldBe("Created");
        entries[1].Action.ShouldBe("SubmittedForReview");
        entries[2].Action.ShouldBe("Approved");
    }

    [Fact]
    public async Task GetAuditTrailAsync_NoEntries_ShouldReturnEmptyList()
    {
        // Arrange
        var auditStore = CreateAuditStore();

        // Act
        var result = await auditStore.GetAuditTrailAsync(Guid.NewGuid());

        // Assert
        result.IsRight.ShouldBeTrue();
        var entries = result.Match<IReadOnlyList<DPIAAuditEntry>>(Right: r => r, Left: _ => []);
        entries.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAuditTrailAsync_ShouldOnlyReturnEntriesForSpecifiedAssessment()
    {
        // Arrange
        var auditStore = CreateAuditStore();
        var assessmentId1 = Guid.NewGuid();
        var assessmentId2 = Guid.NewGuid();

        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(assessmentId: assessmentId1, action: "Created"));
        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(assessmentId: assessmentId2, action: "Created"));
        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(assessmentId: assessmentId1, action: "Approved"));

        // Act
        var result = await auditStore.GetAuditTrailAsync(assessmentId1);

        // Assert
        result.IsRight.ShouldBeTrue();
        var entries = result.Match<IReadOnlyList<DPIAAuditEntry>>(Right: r => r, Left: _ => []);
        entries.Count.ShouldBe(2);
        entries.ShouldAllBe(e => e.AssessmentId == assessmentId1);
    }

    #endregion

    #region Full Lifecycle Test

    [Fact]
    public async Task FullLifecycle_CreateReviewApproveExpireDelete_ShouldWork()
    {
        // Arrange
        var store = CreateDPIAStore();
        var auditStore = CreateAuditStore();
        var assessmentId = Guid.NewGuid();
        var requestTypeName = "Lifecycle.ProcessBiometricDataCommand";

        // Step 1: Create draft assessment
        var draft = CreateAssessment(
            id: assessmentId,
            requestTypeName: requestTypeName,
            status: DPIAAssessmentStatus.Draft);
        var saveResult = await store.SaveAssessmentAsync(draft);
        saveResult.IsRight.ShouldBeTrue();

        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(
            assessmentId: assessmentId,
            action: "Created",
            performedBy: "system"));

        // Step 2: Submit for review
        var inReview = draft with { Status = DPIAAssessmentStatus.InReview };
        await store.SaveAssessmentAsync(inReview);

        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(
            assessmentId: assessmentId,
            action: "SubmittedForReview",
            performedBy: "analyst@company.com"));

        // Step 3: Approve with future review date
        var futureReview = DateTimeOffset.UtcNow.AddMonths(6);
        var approved = inReview with
        {
            Status = DPIAAssessmentStatus.Approved,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            NextReviewAtUtc = futureReview,
            Result = new DPIAResult
            {
                OverallRisk = RiskLevel.Medium,
                IdentifiedRisks = [new RiskItem("Security", RiskLevel.Medium, "Data breach risk", "Apply encryption")],
                ProposedMitigations = [new Mitigation("Encryption at rest", "Technical", false, null)],
                RequiresPriorConsultation = false,
                AssessedAtUtc = DateTimeOffset.UtcNow,
                AssessedBy = "dpo@company.com"
            }
        };
        await store.SaveAssessmentAsync(approved);

        await auditStore.RecordAuditEntryAsync(CreateAuditEntry(
            assessmentId: assessmentId,
            action: "Approved",
            performedBy: "dpo@company.com"));

        // Verify: Assessment should be retrievable by request type
        var getByTypeResult = await store.GetAssessmentAsync(requestTypeName);
        var optByType = (Option<DPIAAssessment>)getByTypeResult;
        optByType.IsSome.ShouldBeTrue();
        var retrieved = (DPIAAssessment)optByType;
        retrieved.Status.ShouldBe(DPIAAssessmentStatus.Approved);
        retrieved.Result.ShouldNotBeNull();

        // Verify: Not expired yet
        var expiredResult = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);
        var expired = expiredResult.Match<IReadOnlyList<DPIAAssessment>>(Right: r => r, Left: _ => []);
        expired.ShouldNotContain(a => a.Id == assessmentId);

        // Verify: Audit trail has 3 entries
        var trailResult = await auditStore.GetAuditTrailAsync(assessmentId);
        var trail = trailResult.Match<IReadOnlyList<DPIAAuditEntry>>(Right: r => r, Left: _ => []);
        trail.Count.ShouldBe(3);

        // Step 4: Delete
        var deleteResult = await store.DeleteAssessmentAsync(assessmentId);
        deleteResult.IsRight.ShouldBeTrue();

        // Verify: No longer retrievable
        var afterDeleteResult = await store.GetAssessmentByIdAsync(assessmentId);
        var optAfterDelete = (Option<DPIAAssessment>)afterDeleteResult;
        optAfterDelete.IsNone.ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private DPIAStoreMongoDB CreateDPIAStore()
    {
        return new DPIAStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<DPIAStoreMongoDB>.Instance);
    }

    private DPIAAuditStoreMongoDB CreateAuditStore()
    {
        return new DPIAAuditStoreMongoDB(
            _fixture.Client!,
            _options,
            NullLogger<DPIAAuditStoreMongoDB>.Instance);
    }

    private static DPIAAssessment CreateAssessment(
        Guid? id = null,
        string requestTypeName = "TestNamespace.TestCommand",
        DPIAAssessmentStatus status = DPIAAssessmentStatus.Draft,
        string? processingType = null,
        string? reason = null,
        DPIAResult? result = null,
        DPOConsultation? dpoConsultation = null,
        DateTimeOffset? approvedAtUtc = null,
        DateTimeOffset? nextReviewAtUtc = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        return new DPIAAssessment
        {
            Id = id ?? Guid.NewGuid(),
            RequestTypeName = requestTypeName,
            Status = status,
            ProcessingType = processingType,
            Reason = reason,
            Result = result,
            DPOConsultation = dpoConsultation,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ApprovedAtUtc = approvedAtUtc,
            NextReviewAtUtc = nextReviewAtUtc,
            TenantId = tenantId,
            ModuleId = moduleId
        };
    }

    private static DPIAAuditEntry CreateAuditEntry(
        Guid? id = null,
        Guid? assessmentId = null,
        string action = "Created",
        string? performedBy = "system",
        DateTimeOffset? occurredAtUtc = null,
        string? details = null)
    {
        return new DPIAAuditEntry
        {
            Id = id ?? Guid.NewGuid(),
            AssessmentId = assessmentId ?? Guid.NewGuid(),
            Action = action,
            PerformedBy = performedBy,
            OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow,
            Details = details
        };
    }

    #endregion
}
