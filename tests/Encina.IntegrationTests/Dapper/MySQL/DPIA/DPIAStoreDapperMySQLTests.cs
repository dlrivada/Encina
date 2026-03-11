using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Dapper.MySQL.DPIA;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;

namespace Encina.IntegrationTests.Dapper.MySQL.DPIA;

[Collection("Dapper-MySQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.MySQL")]
public sealed class DPIAStoreDapperMySQLTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;
    private DPIAStoreDapper _store = null!;
    private DPIAAuditStoreDapper _auditStore = null!;

    public DPIAStoreDapperMySQLTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        var connection = _fixture.CreateConnection();
        _store = new DPIAStoreDapper(connection);
        _auditStore = new DPIAAuditStoreDapper(connection);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveAssessmentAsync_ValidAssessment_ShouldPersist()
    {
        // Arrange
        var assessment = CreateAssessment();

        // Act
        var result = await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(retrieved.IsRight);
        retrieved.IfRight(opt => opt.IfSome(a => Assert.Equal(assessment.Id, a.Id)));
    }

    [Fact]
    public async Task SaveAssessmentAsync_UpdateExisting_ShouldOverwrite()
    {
        // Arrange
        var assessment = CreateAssessment();
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);
        var updated = assessment with { Reason = "Updated reason" };

        // Act
        var result = await _store.SaveAssessmentAsync(updated, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(retrieved.IsRight);
        retrieved.IfRight(opt => opt.IfSome(a => Assert.Equal("Updated reason", a.Reason)));
    }

    [Fact]
    public async Task GetAssessmentAsync_ExistingByTypeName_ShouldReturnSome()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "UniqueType_GetByName");
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Act
        var result = await _store.GetAssessmentAsync("UniqueType_GetByName", CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(opt =>
        {
            Assert.True(opt.IsSome);
            opt.IfSome(a => Assert.Equal(assessment.Id, a.Id));
        });
    }

    [Fact]
    public async Task GetAssessmentAsync_NonExisting_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetAssessmentAsync("NonExistentType_" + Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(opt => Assert.True(opt.IsNone));
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
        Assert.True(result.IsRight);
        result.IfRight(opt =>
        {
            Assert.True(opt.IsSome);
            opt.IfSome(a => Assert.Equal(assessment.RequestTypeName, a.RequestTypeName));
        });
    }

    [Fact]
    public async Task GetAssessmentByIdAsync_NonExisting_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetAssessmentByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(opt => Assert.True(opt.IsNone));
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_ShouldReturnOnlyExpiredApproved()
    {
        // Arrange
        var expired = CreateAssessment(
            requestTypeName: "Expired_" + Guid.NewGuid(),
            status: DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(-30));
        var valid = CreateAssessment(
            requestTypeName: "Valid_" + Guid.NewGuid(),
            status: DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(30));
        await _store.SaveAssessmentAsync(expired, CancellationToken.None);
        await _store.SaveAssessmentAsync(valid, CancellationToken.None);

        // Act
        var result = await _store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(list =>
        {
            Assert.Contains(list, a => a.Id == expired.Id);
            Assert.DoesNotContain(list, a => a.Id == valid.Id);
        });
    }

    [Fact]
    public async Task GetAllAssessmentsAsync_ShouldReturnAll()
    {
        // Arrange
        var a1 = CreateAssessment(requestTypeName: "All1_" + Guid.NewGuid());
        var a2 = CreateAssessment(requestTypeName: "All2_" + Guid.NewGuid());
        await _store.SaveAssessmentAsync(a1, CancellationToken.None);
        await _store.SaveAssessmentAsync(a2, CancellationToken.None);

        // Act
        var result = await _store.GetAllAssessmentsAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(list =>
        {
            Assert.Contains(list, a => a.Id == a1.Id);
            Assert.Contains(list, a => a.Id == a2.Id);
        });
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
        Assert.True(deleteResult.IsRight);
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(retrieved.IsRight);
        retrieved.IfRight(opt => Assert.True(opt.IsNone));
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
        Assert.True(recordResult.IsRight);
        Assert.True(trailResult.IsRight);
        trailResult.IfRight(trail =>
        {
            Assert.Single(trail);
            Assert.Equal(entry.Id, trail[0].Id);
            Assert.Equal(entry.Action, trail[0].Action);
            Assert.Equal(entry.PerformedBy, trail[0].PerformedBy);
        });
    }

    [Fact]
    public async Task AuditStore_GetAuditTrail_ShouldBeIsolatedByAssessmentId()
    {
        // Arrange
        var assessment1 = CreateAssessment(requestTypeName: "Isolated1_" + Guid.NewGuid());
        var assessment2 = CreateAssessment(requestTypeName: "Isolated2_" + Guid.NewGuid());
        await _store.SaveAssessmentAsync(assessment1, CancellationToken.None);
        await _store.SaveAssessmentAsync(assessment2, CancellationToken.None);

        var entry1 = CreateAuditEntry(assessment1.Id, action: "Action1");
        var entry2 = CreateAuditEntry(assessment2.Id, action: "Action2");
        await _auditStore.RecordAuditEntryAsync(entry1, CancellationToken.None);
        await _auditStore.RecordAuditEntryAsync(entry2, CancellationToken.None);

        // Act
        var trail1Result = await _auditStore.GetAuditTrailAsync(assessment1.Id, CancellationToken.None);
        var trail2Result = await _auditStore.GetAuditTrailAsync(assessment2.Id, CancellationToken.None);

        // Assert
        Assert.True(trail1Result.IsRight);
        Assert.True(trail2Result.IsRight);
        trail1Result.IfRight(trail =>
        {
            Assert.Single(trail);
            Assert.Equal("Action1", trail[0].Action);
        });
        trail2Result.IfRight(trail =>
        {
            Assert.Single(trail);
            Assert.Equal("Action2", trail[0].Action);
        });
    }

    [Fact]
    public async Task FullLifecycle_SaveAuditRetrieveDelete_ShouldWork()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "Lifecycle_" + Guid.NewGuid());

        // Act - Save
        var saveResult = await _store.SaveAssessmentAsync(assessment, CancellationToken.None);
        Assert.True(saveResult.IsRight);

        // Act - Audit
        var auditEntry = CreateAuditEntry(assessment.Id, action: "Created");
        var auditResult = await _auditStore.RecordAuditEntryAsync(auditEntry, CancellationToken.None);
        Assert.True(auditResult.IsRight);

        // Act - Retrieve by type name
        var byNameResult = await _store.GetAssessmentAsync(assessment.RequestTypeName, CancellationToken.None);
        Assert.True(byNameResult.IsRight);
        byNameResult.IfRight(opt =>
        {
            Assert.True(opt.IsSome);
            opt.IfSome(a => Assert.Equal(assessment.Id, a.Id));
        });

        // Act - Retrieve by ID
        var byIdResult = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(byIdResult.IsRight);
        byIdResult.IfRight(opt =>
        {
            Assert.True(opt.IsSome);
            opt.IfSome(a => Assert.Equal(assessment.RequestTypeName, a.RequestTypeName));
        });

        // Act - Get audit trail
        var trailResult = await _auditStore.GetAuditTrailAsync(assessment.Id, CancellationToken.None);
        Assert.True(trailResult.IsRight);
        trailResult.IfRight(trail => Assert.Single(trail));

        // Act - Delete
        var deleteResult = await _store.DeleteAssessmentAsync(assessment.Id, CancellationToken.None);
        Assert.True(deleteResult.IsRight);

        // Assert - Verify deleted
        var afterDelete = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(afterDelete.IsRight);
        afterDelete.IfRight(opt => Assert.True(opt.IsNone));
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
}
