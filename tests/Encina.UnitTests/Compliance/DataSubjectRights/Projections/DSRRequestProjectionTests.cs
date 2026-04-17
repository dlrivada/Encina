using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Events;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Projections;

/// <summary>
/// Unit tests for <see cref="DSRRequestProjection"/>.
/// </summary>
public class DSRRequestProjectionTests
{
    private readonly DSRRequestProjection _projection = new();
    private static readonly ProjectionContext DefaultContext = new();

    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Deadline = Now.AddDays(30);

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnExpectedName()
    {
        _projection.ProjectionName.ShouldBe("DSRRequestProjection");
    }

    #endregion

    #region Create (DSRRequestSubmitted)

    [Fact]
    public void Create_ShouldReturnReadModelWithCorrectProperties()
    {
        // Arrange
        var @event = new DSRRequestSubmitted(
            DefaultId, "subject-1", DataSubjectRight.Access, Now, Deadline,
            "I want my data", "tenant-1", "module-1");

        // Act
        var model = _projection.Create(@event, DefaultContext);

        // Assert
        model.Id.ShouldBe(DefaultId);
        model.SubjectId.ShouldBe("subject-1");
        model.RightType.ShouldBe(DataSubjectRight.Access);
        model.Status.ShouldBe(DSRRequestStatus.Received);
        model.ReceivedAtUtc.ShouldBe(Now);
        model.DeadlineAtUtc.ShouldBe(Deadline);
        model.RequestDetails.ShouldBe("I want my data");
        model.TenantId.ShouldBe("tenant-1");
        model.ModuleId.ShouldBe("module-1");
        model.LastModifiedAtUtc.ShouldBe(Now);
        model.Version.ShouldBe(1);
    }

    [Fact]
    public void Create_WithNullOptionals_ShouldSetNulls()
    {
        // Arrange
        var @event = new DSRRequestSubmitted(
            DefaultId, "subject-1", DataSubjectRight.Erasure, Now, Deadline,
            null, null, null);

        // Act
        var model = _projection.Create(@event, DefaultContext);

        // Assert
        model.RequestDetails.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.ModuleId.ShouldBeNull();
    }

    #endregion

    #region Apply (DSRRequestVerified)

    [Fact]
    public void Apply_Verified_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        var verifiedAt = Now.AddHours(1);
        var @event = new DSRRequestVerified(DefaultId, "admin-1", verifiedAt, "tenant-1", "module-1");

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.Status.ShouldBe(DSRRequestStatus.IdentityVerified);
        result.VerifiedAtUtc.ShouldBe(verifiedAt);
        result.LastModifiedAtUtc.ShouldBe(verifiedAt);
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Apply (DSRRequestProcessing)

    [Fact]
    public void Apply_Processing_ShouldUpdateStatusAndUserId()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        model.Status = DSRRequestStatus.IdentityVerified;
        model.Version = 2;
        var startedAt = Now.AddHours(2);
        var @event = new DSRRequestProcessing(DefaultId, "operator-1", startedAt, "tenant-1", "module-1");

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.Status.ShouldBe(DSRRequestStatus.InProgress);
        result.ProcessedByUserId.ShouldBe("operator-1");
        result.LastModifiedAtUtc.ShouldBe(startedAt);
        result.Version.ShouldBe(3);
    }

    [Fact]
    public void Apply_Processing_NullUserId_ShouldSetNull()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        var @event = new DSRRequestProcessing(DefaultId, null, Now.AddHours(2), null, null);

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.ProcessedByUserId.ShouldBeNull();
    }

    #endregion

    #region Apply (DSRRequestCompleted)

    [Fact]
    public void Apply_Completed_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        model.Status = DSRRequestStatus.InProgress;
        model.Version = 3;
        var completedAt = Now.AddDays(5);
        var @event = new DSRRequestCompleted(DefaultId, completedAt, "tenant-1", "module-1");

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.Status.ShouldBe(DSRRequestStatus.Completed);
        result.CompletedAtUtc.ShouldBe(completedAt);
        result.LastModifiedAtUtc.ShouldBe(completedAt);
        result.Version.ShouldBe(4);
    }

    #endregion

    #region Apply (DSRRequestDenied)

    [Fact]
    public void Apply_Denied_ShouldUpdateStatusReasonAndTimestamp()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        var deniedAt = Now.AddHours(1);
        var @event = new DSRRequestDenied(DefaultId, "Manifestly unfounded", deniedAt, "tenant-1", "module-1");

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.Status.ShouldBe(DSRRequestStatus.Rejected);
        result.RejectionReason.ShouldBe("Manifestly unfounded");
        result.CompletedAtUtc.ShouldBe(deniedAt);
        result.LastModifiedAtUtc.ShouldBe(deniedAt);
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Apply (DSRRequestExtended)

    [Fact]
    public void Apply_Extended_ShouldUpdateStatusDeadlineAndReason()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        var extendedDeadline = Deadline.AddMonths(2);
        var extendedAt = Now.AddDays(20);
        var @event = new DSRRequestExtended(DefaultId, "Complex case", extendedDeadline, extendedAt, "tenant-1", "module-1");

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.Status.ShouldBe(DSRRequestStatus.Extended);
        result.ExtensionReason.ShouldBe("Complex case");
        result.ExtendedDeadlineAtUtc.ShouldBe(extendedDeadline);
        result.LastModifiedAtUtc.ShouldBe(extendedAt);
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Apply (DSRRequestExpired)

    [Fact]
    public void Apply_Expired_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var model = CreateDefaultReadModel();
        var expiredAt = Now.AddDays(31);
        var @event = new DSRRequestExpired(DefaultId, expiredAt, "tenant-1", "module-1");

        // Act
        var result = _projection.Apply(@event, model, DefaultContext);

        // Assert
        result.Status.ShouldBe(DSRRequestStatus.Expired);
        result.LastModifiedAtUtc.ShouldBe(expiredAt);
        result.Version.ShouldBe(2);
    }

    #endregion

    #region Version Incrementing

    [Fact]
    public void Apply_MultipleEvents_ShouldIncrementVersionCorrectly()
    {
        // Arrange
        var submitted = new DSRRequestSubmitted(
            DefaultId, "subject-1", DataSubjectRight.Erasure, Now, Deadline, null, null, null);
        var verified = new DSRRequestVerified(DefaultId, "admin-1", Now.AddHours(1), null, null);
        var processing = new DSRRequestProcessing(DefaultId, "operator-1", Now.AddHours(2), null, null);
        var completed = new DSRRequestCompleted(DefaultId, Now.AddDays(5), null, null);

        // Act
        var model = _projection.Create(submitted, DefaultContext);
        model = _projection.Apply(verified, model, DefaultContext);
        model = _projection.Apply(processing, model, DefaultContext);
        model = _projection.Apply(completed, model, DefaultContext);

        // Assert
        model.Version.ShouldBe(4);
        model.Status.ShouldBe(DSRRequestStatus.Completed);
    }

    #endregion

    #region Helpers

    private static DSRRequestReadModel CreateDefaultReadModel()
    {
        return new DSRRequestReadModel
        {
            Id = DefaultId,
            SubjectId = "subject-1",
            RightType = DataSubjectRight.Access,
            Status = DSRRequestStatus.Received,
            ReceivedAtUtc = Now,
            DeadlineAtUtc = Deadline,
            LastModifiedAtUtc = Now,
            Version = 1
        };
    }

    #endregion
}
