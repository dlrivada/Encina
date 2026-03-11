using Encina.ADO.PostgreSQL.DPIA;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.TestInfrastructure.Fixtures;
using LanguageExt;

namespace Encina.IntegrationTests.ADO.PostgreSQL.DPIA;

[Collection("ADO-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.PostgreSQL")]
public class DPIAStoreADOPostgreSQLTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private DPIAStoreADO _store = null!;
    private DPIAAuditStoreADO _auditStore = null!;

    public DPIAStoreADOPostgreSQLTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        var connection = _fixture.CreateConnection();
        _store = new DPIAStoreADO(connection);
        _auditStore = new DPIAAuditStoreADO(connection);
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
        retrieved.IfRight(option =>
        {
            Assert.True(option.IsSome);
            option.IfSome(a =>
            {
                Assert.Equal(assessment.Id, a.Id);
                Assert.Equal(assessment.RequestTypeName, a.RequestTypeName);
                Assert.Equal(assessment.Status, a.Status);
            });
        });
    }

    [Fact]
    public async Task SaveAssessmentAsync_UpdateExisting_ShouldOverwrite()
    {
        // Arrange
        var id = Guid.NewGuid();
        var original = CreateAssessment(id: id, requestTypeName: "OriginalCommand");
        await _store.SaveAssessmentAsync(original, CancellationToken.None);

        var updated = CreateAssessment(id: id, requestTypeName: "UpdatedCommand");

        // Act
        var result = await _store.SaveAssessmentAsync(updated, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        var retrieved = await _store.GetAssessmentByIdAsync(id, CancellationToken.None);
        Assert.True(retrieved.IsRight);
        retrieved.IfRight(option =>
        {
            Assert.True(option.IsSome);
            option.IfSome(a => Assert.Equal("UpdatedCommand", a.RequestTypeName));
        });
    }

    [Fact]
    public async Task GetAssessmentAsync_ExistingByTypeName_ShouldReturnSome()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "UniqueTypeName_PostgreSQL_1");
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Act
        var result = await _store.GetAssessmentAsync("UniqueTypeName_PostgreSQL_1", CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(option =>
        {
            Assert.True(option.IsSome);
            option.IfSome(a => Assert.Equal(assessment.Id, a.Id));
        });
    }

    [Fact]
    public async Task GetAssessmentAsync_NonExisting_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetAssessmentAsync("NonExistentTypeName_PostgreSQL", CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(option => Assert.True(option.IsNone));
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
        result.IfRight(option =>
        {
            Assert.True(option.IsSome);
            option.IfSome(a => Assert.Equal(assessment.Id, a.Id));
        });
    }

    [Fact]
    public async Task GetAssessmentByIdAsync_NonExisting_ShouldReturnNone()
    {
        // Act
        var result = await _store.GetAssessmentByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(option => Assert.True(option.IsNone));
    }

    [Fact]
    public async Task GetExpiredAssessmentsAsync_ShouldReturnOnlyExpiredApproved()
    {
        // Arrange
        var expiredAssessment = CreateAssessment(
            requestTypeName: "ExpiredCommand_PostgreSQL",
            status: DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(-30));
        await _store.SaveAssessmentAsync(expiredAssessment, CancellationToken.None);

        var futureAssessment = CreateAssessment(
            requestTypeName: "FutureCommand_PostgreSQL",
            status: DPIAAssessmentStatus.Approved,
            nextReviewAtUtc: DateTimeOffset.UtcNow.AddDays(365));
        await _store.SaveAssessmentAsync(futureAssessment, CancellationToken.None);

        // Act
        var result = await _store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(assessments =>
        {
            Assert.Contains(assessments, a => a.Id == expiredAssessment.Id);
            Assert.DoesNotContain(assessments, a => a.Id == futureAssessment.Id);
        });
    }

    [Fact]
    public async Task GetAllAssessmentsAsync_ShouldReturnAll()
    {
        // Arrange
        var assessment1 = CreateAssessment(requestTypeName: "AllTest1_PostgreSQL");
        var assessment2 = CreateAssessment(requestTypeName: "AllTest2_PostgreSQL");
        await _store.SaveAssessmentAsync(assessment1, CancellationToken.None);
        await _store.SaveAssessmentAsync(assessment2, CancellationToken.None);

        // Act
        var result = await _store.GetAllAssessmentsAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsRight);
        result.IfRight(assessments =>
        {
            Assert.True(assessments.Count >= 2);
            Assert.Contains(assessments, a => a.Id == assessment1.Id);
            Assert.Contains(assessments, a => a.Id == assessment2.Id);
        });
    }

    [Fact]
    public async Task DeleteAssessmentAsync_Existing_ShouldRemove()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "DeleteTest_PostgreSQL");
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        // Act
        var deleteResult = await _store.DeleteAssessmentAsync(assessment.Id, CancellationToken.None);

        // Assert
        Assert.True(deleteResult.IsRight);
        var retrieved = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(retrieved.IsRight);
        retrieved.IfRight(option => Assert.True(option.IsNone));
    }

    [Fact]
    public async Task AuditStore_RecordAndRetrieve_ShouldRoundTrip()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "AuditRoundTrip_PostgreSQL");
        await _store.SaveAssessmentAsync(assessment, CancellationToken.None);

        var auditEntry = CreateAuditEntry(assessment.Id, action: "Created");

        // Act
        var recordResult = await _auditStore.RecordAuditEntryAsync(auditEntry, CancellationToken.None);

        // Assert
        Assert.True(recordResult.IsRight);
        var trailResult = await _auditStore.GetAuditTrailAsync(assessment.Id, CancellationToken.None);
        Assert.True(trailResult.IsRight);
        trailResult.IfRight(entries =>
        {
            Assert.Single(entries);
            Assert.Equal(auditEntry.Id, entries[0].Id);
            Assert.Equal(auditEntry.Action, entries[0].Action);
            Assert.Equal(auditEntry.PerformedBy, entries[0].PerformedBy);
        });
    }

    [Fact]
    public async Task AuditStore_GetAuditTrail_ShouldBeIsolatedByAssessmentId()
    {
        // Arrange
        var assessment1 = CreateAssessment(requestTypeName: "AuditIsolation1_PostgreSQL");
        var assessment2 = CreateAssessment(requestTypeName: "AuditIsolation2_PostgreSQL");
        await _store.SaveAssessmentAsync(assessment1, CancellationToken.None);
        await _store.SaveAssessmentAsync(assessment2, CancellationToken.None);

        var entry1 = CreateAuditEntry(assessment1.Id, action: "Created");
        var entry2 = CreateAuditEntry(assessment2.Id, action: "Reviewed");
        await _auditStore.RecordAuditEntryAsync(entry1, CancellationToken.None);
        await _auditStore.RecordAuditEntryAsync(entry2, CancellationToken.None);

        // Act
        var trail1Result = await _auditStore.GetAuditTrailAsync(assessment1.Id, CancellationToken.None);
        var trail2Result = await _auditStore.GetAuditTrailAsync(assessment2.Id, CancellationToken.None);

        // Assert
        Assert.True(trail1Result.IsRight);
        trail1Result.IfRight(entries =>
        {
            Assert.Single(entries);
            Assert.Equal("Created", entries[0].Action);
        });

        Assert.True(trail2Result.IsRight);
        trail2Result.IfRight(entries =>
        {
            Assert.Single(entries);
            Assert.Equal("Reviewed", entries[0].Action);
        });
    }

    [Fact]
    public async Task FullLifecycle_SaveAuditRetrieveDelete_ShouldWork()
    {
        // Arrange
        var assessment = CreateAssessment(requestTypeName: "FullLifecycle_PostgreSQL");

        // Act - Save
        var saveResult = await _store.SaveAssessmentAsync(assessment, CancellationToken.None);
        Assert.True(saveResult.IsRight);

        // Act - Record audit entries
        var auditEntry1 = CreateAuditEntry(assessment.Id, action: "Created", performedBy: "Admin");
        var auditEntry2 = CreateAuditEntry(assessment.Id, action: "Approved", performedBy: "DPO");
        await _auditStore.RecordAuditEntryAsync(auditEntry1, CancellationToken.None);
        await _auditStore.RecordAuditEntryAsync(auditEntry2, CancellationToken.None);

        // Assert - Retrieve assessment
        var retrieveResult = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(retrieveResult.IsRight);
        retrieveResult.IfRight(option =>
        {
            Assert.True(option.IsSome);
            option.IfSome(a =>
            {
                Assert.Equal(assessment.RequestTypeName, a.RequestTypeName);
                Assert.Equal(DPIAAssessmentStatus.Approved, a.Status);
            });
        });

        // Assert - Retrieve audit trail
        var trailResult = await _auditStore.GetAuditTrailAsync(assessment.Id, CancellationToken.None);
        Assert.True(trailResult.IsRight);
        trailResult.IfRight(entries =>
        {
            Assert.Equal(2, entries.Count);
        });

        // Act - Delete
        var deleteResult = await _store.DeleteAssessmentAsync(assessment.Id, CancellationToken.None);
        Assert.True(deleteResult.IsRight);

        // Assert - Verify deleted
        var afterDelete = await _store.GetAssessmentByIdAsync(assessment.Id, CancellationToken.None);
        Assert.True(afterDelete.IsRight);
        afterDelete.IfRight(option => Assert.True(option.IsNone));
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
