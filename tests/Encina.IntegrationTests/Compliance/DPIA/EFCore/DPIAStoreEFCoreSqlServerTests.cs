using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.EntityFrameworkCore.DPIA;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Encina.IntegrationTests.Compliance.DPIA.EFCore;

[Collection("EFCore-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class DPIAStoreEFCoreSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;
    private DPIATestDbContext _dbContext = null!;
    private DPIAStoreEF _store = null!;
    private DPIAAuditStoreEF _auditStore = null!;

    public DPIAStoreEFCoreSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _dbContext = _fixture.CreateDbContext<DPIATestDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();
        _store = new DPIAStoreEF(_dbContext);
        _auditStore = new DPIAAuditStoreEF(_dbContext);
    }

    public ValueTask DisposeAsync()
    {
        _dbContext?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SaveAssessmentAsync_ValidAssessment_ShouldPersist()
    {
        // Arrange
        var assessment = CreateAssessment();

        // Act
        var result = await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        retrieved.IsRight.ShouldBeTrue();
        retrieved.Match(
            Right: opt => opt.IsSome.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task SaveAssessmentAsync_UpdateExisting_ShouldOverwrite()
    {
        // Arrange
        var assessment = CreateAssessment();
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);
        _dbContext.ChangeTracker.Clear();

        var updated = assessment with { Reason = "Updated reason" };

        // Act
        var result = await _store.SaveAssessmentAsync(updated, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        retrieved.Match(
            Right: opt => opt.Match(
                Some: a => a.Reason.ShouldBe("Updated reason"),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAssessmentAsync_ExistingByTypeName_ShouldReturnSome()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "UniqueCommand");
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Act
        var result = await _store.GetAssessmentAsync("UniqueCommand", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: opt => opt.IsSome.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAssessmentAsync_NonExisting_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetAssessmentAsync("NonExistentCommand", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: opt => opt.IsNone.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAssessmentByIdAsync_Existing_ShouldReturnSome()
    {
        // Arrange
        var assessment = CreateAssessment();
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Act
        var result = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: opt => opt.Match(
                Some: a => a.RequestTypeName.ShouldBe(assessment.RequestTypeName),
                None: () => Assert.Fail("Expected Some")),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAssessmentByIdAsync_NonExisting_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetAssessmentByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: opt => opt.IsNone.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_ShouldReturnOnlyExpiredApproved()
    {
        // Arrange
        var expired = CreateAssessment(
            requestTypeName: "ExpiredCommand",
            status: DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(-1));
        var notExpired = CreateAssessment(
            requestTypeName: "ValidCommand",
            status: DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(365));
        var draft = CreateAssessment(
            requestTypeName: "DraftCommand",
            status: DPIAAssessmentStatus.Draft,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(-1));

        await _store.SaveAssessmentAsync(expired, CancellationToken.None);
        await _store.SaveAssessmentAsync(notExpired, CancellationToken.None);
        await _store.SaveAssessmentAsync(draft, CancellationToken.None);

        // Act
        var result = await _store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: list =>
            {
                list.Count.ShouldBe(1);
                list[0].RequestTypeName.ShouldBe("ExpiredCommand");
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task GetAllAssessmentsAsync_ShouldReturnAll()
    {
        // Arrange
        var a1 = CreateAssessment(requestTypeName: "Command1");
        var a2 = CreateAssessment(requestTypeName: "Command2");
        var a3 = CreateAssessment(requestTypeName: "Command3");

        await _store.SaveAssessmentAsync(a1, CancellationToken.None);
        await _store.SaveAssessmentAsync(a2, CancellationToken.None);
        await _store.SaveAssessmentAsync(a3, CancellationToken.None);

        // Act
        var result = await _store.GetAllAssessmentsAsync(CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: list => list.Count.ShouldBe(3),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task DeleteAssessmentAsync_Existing_ShouldRemove()
    {
        // Arrange
        var assessment = CreateAssessment();
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Act
        var deleteResult = await _store.DeleteAssessmentAsync(assessment.Id, CancellationToken.None);

        // Assert
        deleteResult.IsRight.ShouldBeTrue();
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        retrieved.Match(
            Right: opt => opt.IsNone.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AuditStore_RecordAndRetrieve_ShouldRoundTrip()
    {
        // Arrange
        var assessment = CreateAssessment();
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);
        var entry = CreateAuditEntry(assessment.Id);

        // Act
        var recordResult = await _auditStore.RecordAuditEntryAsync(entry, CancellationToken.None);
        var trailResult = await _auditStore.GetAuditTrailAsync(assessment.Id, CancellationToken.None);

        // Assert
        recordResult.IsRight.ShouldBeTrue();
        trailResult.IsRight.ShouldBeTrue();
        trailResult.Match(
            Right: list =>
            {
                list.Count.ShouldBe(1);
                list[0].Action.ShouldBe("Created");
                list[0].PerformedBy.ShouldBe("TestUser");
            },
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task AuditStore_GetAuditTrail_ShouldBeIsolatedByAssessmentId()
    {
        // Arrange
        var assessment1 = CreateAssessment(requestTypeName: "IsolationTest1");
        var assessment2 = CreateAssessment(requestTypeName: "IsolationTest2");
        await _store.SaveAssessmentAsync(assessment1, CancellationToken.None);
        await _store.SaveAssessmentAsync(assessment2, CancellationToken.None);

        await _auditStore.RecordAuditEntryAsync(
            CreateAuditEntry(assessment1.Id, "Action1"), CancellationToken.None);
        await _auditStore.RecordAuditEntryAsync(
            CreateAuditEntry(assessment1.Id, "Action2"), CancellationToken.None);
        await _auditStore.RecordAuditEntryAsync(
            CreateAuditEntry(assessment2.Id, "Action3"), CancellationToken.None);

        // Act
        var trail1 = await _auditStore.GetAuditTrailAsync(assessment1.Id, CancellationToken.None);
        var trail2 = await _auditStore.GetAuditTrailAsync(assessment2.Id, CancellationToken.None);

        // Assert
        trail1.Match(
            Right: list => list.Count.ShouldBe(2),
            Left: _ => Assert.Fail("Expected Right"));
        trail2.Match(
            Right: list => list.Count.ShouldBe(1),
            Left: _ => Assert.Fail("Expected Right"));
    }

    [Fact]
    public async Task FullLifecycle_SaveAuditRetrieveDelete_ShouldWork()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "LifecycleCommand");

        // Act - Save
        var saveResult = await _store.SaveAssessmentAsync(assessment, CancellationToken.None);
        saveResult.IsRight.ShouldBeTrue();

        // Act - Audit
        var auditEntry = CreateAuditEntry(assessment.Id, "Created");
        var auditResult = await _auditStore.RecordAuditEntryAsync(auditEntry, CancellationToken.None);
        auditResult.IsRight.ShouldBeTrue();

        // Act - Retrieve
        var getResult = await _store.GetAssessmentAsync("LifecycleCommand", CancellationToken.None);
        getResult.IsRight.ShouldBeTrue();
        getResult.Match(
            Right: opt => opt.IsSome.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));

        // Act - Get audit trail
        var trailResult = await _auditStore.GetAuditTrailAsync(assessment.Id, CancellationToken.None);
        trailResult.Match(
            Right: list => list.Count.ShouldBe(1),
            Left: _ => Assert.Fail("Expected Right"));

        // Act - Delete
        var deleteResult = await _store.DeleteAssessmentAsync(assessment.Id, CancellationToken.None);
        deleteResult.IsRight.ShouldBeTrue();

        // Assert - Verify deleted
        var afterDelete = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        afterDelete.Match(
            Right: opt => opt.IsNone.ShouldBeTrue(),
            Left: _ => Assert.Fail("Expected Right"));
    }

    private static DPIAAssessment CreateAssessment(
        Guid? id = null,
        string requestTypeName = "TestCommand",
        DPIAAssessmentStatus status = DPIAAssessmentStatus.Approved,
        DateTimeOffset? nextReviewAtUtc = null)
    {
        return new DPIAAssessment
        {
            Id = id ?? Guid.NewGuid(),
            RequestTypeName = requestTypeName,
            Status = status,
            ProcessingType = "Automated",
            Reason = "Test assessment",
            Result = new DPIAResult
            {
                OverallRisk = RiskLevel.Medium,
                IdentifiedRisks = [new RiskItem("Security", RiskLevel.Medium, "Data exposure risk", "Apply encryption")],
                ProposedMitigations = [new Mitigation("Apply encryption at rest", "Technical", false, null)],
                RequiresPriorConsultation = false,
                AssessedAtUtc = DateTimeOffset.UtcNow,
                AssessedBy = "TestUser"
            },
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ApprovedAtUtc = status == DPIAAssessmentStatus.Approved ? DateTimeOffset.UtcNow : null,
            NextReviewAtUtc = nextReviewAtUtc ?? DateTimeOffset.UtcNow.AddDays(365)
        };
    }

    private static DPIAAuditEntry CreateAuditEntry(
        Guid assessmentId,
        string action = "Created",
        string? performedBy = "TestUser")
    {
        return new DPIAAuditEntry
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            Action = action,
            PerformedBy = performedBy,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Test audit entry"
        };
    }

    private sealed class DPIATestDbContext : DbContext
    {
        public DPIATestDbContext(DbContextOptions<DPIATestDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyDPIAConfiguration();
        }
    }
}
