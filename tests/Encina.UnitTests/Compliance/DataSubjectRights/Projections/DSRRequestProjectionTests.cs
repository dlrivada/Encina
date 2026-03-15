using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Events;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Marten.Projections;
using FluentAssertions;

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
        _projection.ProjectionName.Should().Be("DSRRequestProjection");
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
        model.Id.Should().Be(DefaultId);
        model.SubjectId.Should().Be("subject-1");
        model.RightType.Should().Be(DataSubjectRight.Access);
        model.Status.Should().Be(DSRRequestStatus.Received);
        model.ReceivedAtUtc.Should().Be(Now);
        model.DeadlineAtUtc.Should().Be(Deadline);
        model.RequestDetails.Should().Be("I want my data");
        model.TenantId.Should().Be("tenant-1");
        model.ModuleId.Should().Be("module-1");
        model.LastModifiedAtUtc.Should().Be(Now);
        model.Version.Should().Be(1);
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
        model.RequestDetails.Should().BeNull();
        model.TenantId.Should().BeNull();
        model.ModuleId.Should().BeNull();
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
        result.Status.Should().Be(DSRRequestStatus.IdentityVerified);
        result.VerifiedAtUtc.Should().Be(verifiedAt);
        result.LastModifiedAtUtc.Should().Be(verifiedAt);
        result.Version.Should().Be(2);
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
        result.Status.Should().Be(DSRRequestStatus.InProgress);
        result.ProcessedByUserId.Should().Be("operator-1");
        result.LastModifiedAtUtc.Should().Be(startedAt);
        result.Version.Should().Be(3);
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
        result.ProcessedByUserId.Should().BeNull();
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
        result.Status.Should().Be(DSRRequestStatus.Completed);
        result.CompletedAtUtc.Should().Be(completedAt);
        result.LastModifiedAtUtc.Should().Be(completedAt);
        result.Version.Should().Be(4);
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
        result.Status.Should().Be(DSRRequestStatus.Rejected);
        result.RejectionReason.Should().Be("Manifestly unfounded");
        result.CompletedAtUtc.Should().Be(deniedAt);
        result.LastModifiedAtUtc.Should().Be(deniedAt);
        result.Version.Should().Be(2);
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
        result.Status.Should().Be(DSRRequestStatus.Extended);
        result.ExtensionReason.Should().Be("Complex case");
        result.ExtendedDeadlineAtUtc.Should().Be(extendedDeadline);
        result.LastModifiedAtUtc.Should().Be(extendedAt);
        result.Version.Should().Be(2);
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
        result.Status.Should().Be(DSRRequestStatus.Expired);
        result.LastModifiedAtUtc.Should().Be(expiredAt);
        result.Version.Should().Be(2);
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
        model.Version.Should().Be(4);
        model.Status.Should().Be(DSRRequestStatus.Completed);
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
