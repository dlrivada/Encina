using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Events;
using Shouldly;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Aggregates;

/// <summary>
/// Unit tests for <see cref="DSRRequestAggregate"/>.
/// </summary>
public class DSRRequestAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Submit (Static Factory)

    [Fact]
    public void Submit_ValidParameters_ShouldCreateReceivedAggregate()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Access, Now,
            "I want my data", "tenant-1", "module-1");

        // Assert
        aggregate.Id.ShouldBe(DefaultId);
        aggregate.SubjectId.ShouldBe("subject-1");
        aggregate.RightType.ShouldBe(DataSubjectRight.Access);
        aggregate.Status.ShouldBe(DSRRequestStatus.Received);
        aggregate.ReceivedAtUtc.ShouldBe(Now);
        aggregate.DeadlineAtUtc.ShouldBe(Now.AddDays(30));
        aggregate.RequestDetails.ShouldBe("I want my data");
        aggregate.TenantId.ShouldBe("tenant-1");
        aggregate.ModuleId.ShouldBe("module-1");
        aggregate.CompletedAtUtc.ShouldBeNull();
        aggregate.VerifiedAtUtc.ShouldBeNull();
        aggregate.ExtensionReason.ShouldBeNull();
        aggregate.ExtendedDeadlineAtUtc.ShouldBeNull();
        aggregate.RejectionReason.ShouldBeNull();
        aggregate.ProcessedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Submit_ValidParameters_ShouldRaiseDSRRequestSubmittedEvent()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Erasure, Now);

        // Assert
        aggregate.UncommittedEvents.ShouldHaveSingleItem().ShouldBeOfType<DSRRequestSubmitted>();
        aggregate.Version.ShouldBe(1);
    }

    [Fact]
    public void Submit_SubmittedEvent_ShouldContainCorrectData()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Portability, Now,
            "export request", "tenant-1", "module-1");

        // Assert
        var @event = aggregate.UncommittedEvents.Single().ShouldBeOfType<DSRRequestSubmitted>();
        @event.RequestId.ShouldBe(DefaultId);
        @event.SubjectId.ShouldBe("subject-1");
        @event.RightType.ShouldBe(DataSubjectRight.Portability);
        @event.ReceivedAtUtc.ShouldBe(Now);
        @event.DeadlineAtUtc.ShouldBe(Now.AddDays(30));
        @event.RequestDetails.ShouldBe("export request");
        @event.TenantId.ShouldBe("tenant-1");
        @event.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Submit_NullableParametersAsNull_ShouldCreateAggregate()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Access, Now);

        // Assert
        aggregate.RequestDetails.ShouldBeNull();
        aggregate.TenantId.ShouldBeNull();
        aggregate.ModuleId.ShouldBeNull();
    }

    [Fact]
    public void Submit_ShouldCalculate30DayDeadline()
    {
        // Arrange
        var receivedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Access, receivedAt);

        // Assert
        aggregate.DeadlineAtUtc.ShouldBe(receivedAt.AddDays(30));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_InvalidSubjectId_ShouldThrow(string? subjectId)
    {
        // Act
        Action act = () => DSRRequestAggregate.Submit(
            DefaultId, subjectId!, DataSubjectRight.Access, Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(DataSubjectRight.Access)]
    [InlineData(DataSubjectRight.Rectification)]
    [InlineData(DataSubjectRight.Erasure)]
    [InlineData(DataSubjectRight.Restriction)]
    [InlineData(DataSubjectRight.Portability)]
    [InlineData(DataSubjectRight.Objection)]
    [InlineData(DataSubjectRight.AutomatedDecisionMaking)]
    [InlineData(DataSubjectRight.Notification)]
    public void Submit_AllRightTypes_ShouldSucceed(DataSubjectRight rightType)
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", rightType, Now);

        // Assert
        aggregate.RightType.ShouldBe(rightType);
        aggregate.Status.ShouldBe(DSRRequestStatus.Received);
    }

    #endregion

    #region Verify

    [Fact]
    public void Verify_FromReceived_ShouldTransitionToIdentityVerified()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        var verifiedAt = Now.AddHours(1);

        // Act
        aggregate.Verify("admin-1", verifiedAt);

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.IdentityVerified);
        aggregate.VerifiedAtUtc.ShouldBe(verifiedAt);
    }

    [Fact]
    public void Verify_ShouldRaiseDSRRequestVerifiedEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Verify("admin-1", Now.AddHours(1));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DSRRequestVerified>();
        aggregate.Version.ShouldBe(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_InvalidVerifiedBy_ShouldThrow(string? verifiedBy)
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        Action act = () => aggregate.Verify(verifiedBy!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(DSRRequestStatus.IdentityVerified)]
    [InlineData(DSRRequestStatus.InProgress)]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Extended)]
    [InlineData(DSRRequestStatus.Expired)]
    public void Verify_FromInvalidStatus_ShouldThrow(DSRRequestStatus invalidStatus)
    {
        // Arrange
        var aggregate = CreateAggregateInStatus(invalidStatus);

        // Act
        Action act = () => aggregate.Verify("admin-1", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain($"'{invalidStatus}'");
    }

    #endregion

    #region StartProcessing

    [Fact]
    public void StartProcessing_FromIdentityVerified_ShouldTransitionToInProgress()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();
        var startedAt = Now.AddHours(2);

        // Act
        aggregate.StartProcessing("operator-1", startedAt);

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.InProgress);
        aggregate.ProcessedByUserId.ShouldBe("operator-1");
    }

    [Fact]
    public void StartProcessing_FromExtended_ShouldTransitionToInProgress()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        aggregate.StartProcessing("operator-1", Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.InProgress);
    }

    [Fact]
    public void StartProcessing_NullUserId_ShouldAllowAutomatedProcessing()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.StartProcessing(null, Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.InProgress);
        aggregate.ProcessedByUserId.ShouldBeNull();
    }

    [Fact]
    public void StartProcessing_ShouldRaiseDSRRequestProcessingEvent()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.StartProcessing("operator-1", Now.AddHours(2));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DSRRequestProcessing>();
    }

    [Theory]
    [InlineData(DSRRequestStatus.Received)]
    [InlineData(DSRRequestStatus.InProgress)]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Expired)]
    public void StartProcessing_FromInvalidStatus_ShouldThrow(DSRRequestStatus invalidStatus)
    {
        // Arrange
        var aggregate = CreateAggregateInStatus(invalidStatus);

        // Act
        Action act = () => aggregate.StartProcessing("operator-1", Now.AddHours(2));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain($"'{invalidStatus}'");
    }

    #endregion

    #region Complete

    [Fact]
    public void Complete_FromInProgress_ShouldTransitionToCompleted()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();
        var completedAt = Now.AddDays(5);

        // Act
        aggregate.Complete(completedAt);

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Completed);
        aggregate.CompletedAtUtc.ShouldBe(completedAt);
    }

    [Fact]
    public void Complete_ShouldRaiseDSRRequestCompletedEvent()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Complete(Now.AddDays(5));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DSRRequestCompleted>();
    }

    [Theory]
    [InlineData(DSRRequestStatus.Received)]
    [InlineData(DSRRequestStatus.IdentityVerified)]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Extended)]
    [InlineData(DSRRequestStatus.Expired)]
    public void Complete_FromInvalidStatus_ShouldThrow(DSRRequestStatus invalidStatus)
    {
        // Arrange
        var aggregate = CreateAggregateInStatus(invalidStatus);

        // Act
        Action act = () => aggregate.Complete(Now.AddDays(5));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain($"'{invalidStatus}'");
    }

    #endregion

    #region Deny

    [Fact]
    public void Deny_FromReceived_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        var deniedAt = Now.AddHours(1);

        // Act
        aggregate.Deny("Manifestly unfounded request", deniedAt);

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Rejected);
        aggregate.RejectionReason.ShouldBe("Manifestly unfounded request");
        aggregate.CompletedAtUtc.ShouldBe(deniedAt);
    }

    [Fact]
    public void Deny_FromIdentityVerified_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.Deny("Cannot verify identity sufficiently", Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Rejected);
    }

    [Fact]
    public void Deny_FromInProgress_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Deny("Exemption applies", Now.AddDays(5));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Rejected);
    }

    [Fact]
    public void Deny_FromExtended_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        aggregate.Deny("No longer applicable", Now.AddDays(35));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Rejected);
    }

    [Fact]
    public void Deny_ShouldRaiseDSRRequestDeniedEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Deny("Reason", Now.AddHours(1));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DSRRequestDenied>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deny_InvalidRejectionReason_ShouldThrow(string? reason)
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        Action act = () => aggregate.Deny(reason!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Expired)]
    public void Deny_FromTerminalStatus_ShouldThrow(DSRRequestStatus terminalStatus)
    {
        // Arrange
        var aggregate = CreateAggregateInStatus(terminalStatus);

        // Act
        Action act = () => aggregate.Deny("Reason", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain($"'{terminalStatus}'");
    }

    #endregion

    #region Extend

    [Fact]
    public void Extend_FromReceived_ShouldTransitionToExtended()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        var extendedAt = Now.AddDays(20);

        // Act
        aggregate.Extend("Complex request requiring more time", extendedAt);

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Extended);
        aggregate.ExtensionReason.ShouldBe("Complex request requiring more time");
        aggregate.ExtendedDeadlineAtUtc.ShouldBe(aggregate.DeadlineAtUtc.AddMonths(2));
    }

    [Fact]
    public void Extend_FromIdentityVerified_ShouldTransitionToExtended()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.Extend("Multiple systems to query", Now.AddDays(10));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Extended);
    }

    [Fact]
    public void Extend_FromInProgress_ShouldTransitionToExtended()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Extend("Data scattered across jurisdictions", Now.AddDays(15));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Extended);
    }

    [Fact]
    public void Extend_ShouldRaiseDSRRequestExtendedEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Extend("Reason", Now.AddDays(20));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DSRRequestExtended>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Extend_InvalidExtensionReason_ShouldThrow(string? reason)
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        Action act = () => aggregate.Extend(reason!, Now.AddDays(20));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Expired)]
    [InlineData(DSRRequestStatus.Extended)]
    public void Extend_FromInvalidStatus_ShouldThrow(DSRRequestStatus invalidStatus)
    {
        // Arrange
        var aggregate = CreateAggregateInStatus(invalidStatus);

        // Act
        Action act = () => aggregate.Extend("Reason", Now.AddDays(20));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain($"'{invalidStatus}'");
    }

    #endregion

    #region Expire

    [Fact]
    public void Expire_FromReceived_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        var expiredAt = Now.AddDays(31);

        // Act
        aggregate.Expire(expiredAt);

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_FromIdentityVerified_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_FromInProgress_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_FromExtended_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        aggregate.Expire(Now.AddDays(91));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_ShouldRaiseDSRRequestExpiredEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DSRRequestExpired>();
    }

    [Theory]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Expired)]
    public void Expire_FromTerminalStatus_ShouldThrow(DSRRequestStatus terminalStatus)
    {
        // Arrange
        var aggregate = CreateAggregateInStatus(terminalStatus);

        // Act
        Action act = () => aggregate.Expire(Now.AddDays(31));

        // Assert
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain($"'{terminalStatus}'");
    }

    #endregion

    #region GetEffectiveDeadline

    [Fact]
    public void GetEffectiveDeadline_WithoutExtension_ShouldReturnOriginalDeadline()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        var deadline = aggregate.GetEffectiveDeadline();

        // Assert
        deadline.ShouldBe(aggregate.DeadlineAtUtc);
    }

    [Fact]
    public void GetEffectiveDeadline_WithExtension_ShouldReturnExtendedDeadline()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        var deadline = aggregate.GetEffectiveDeadline();

        // Assert
        deadline.ShouldBe(aggregate.ExtendedDeadlineAtUtc!.Value);
    }

    #endregion

    #region IsOverdue

    [Fact]
    public void IsOverdue_BeforeDeadline_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(15)).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_AfterDeadline_ShouldReturnTrue()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(31)).ShouldBeTrue();
    }

    [Fact]
    public void IsOverdue_WhenCompleted_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();
        aggregate.Complete(Now.AddDays(5));

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(31)).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_WhenRejected_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        aggregate.Deny("Reason", Now.AddHours(1));

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(31)).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        aggregate.Expire(Now.AddDays(31));

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(60)).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_WhenExtended_BeforeExtendedDeadline_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act / Assert — extended deadline is original + 2 months
        aggregate.IsOverdue(Now.AddDays(45)).ShouldBeFalse();
    }

    [Fact]
    public void IsOverdue_WhenExtended_AfterExtendedDeadline_ShouldReturnTrue()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();
        var extendedDeadline = aggregate.ExtendedDeadlineAtUtc!.Value;

        // Act / Assert
        aggregate.IsOverdue(extendedDeadline.AddDays(1)).ShouldBeTrue();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_Submit_Verify_Process_Complete_ShouldSucceed()
    {
        // Arrange & Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Erasure, Now,
            "Delete my data", "tenant-1", "module-1");

        aggregate.Verify("admin-1", Now.AddHours(1));
        aggregate.StartProcessing("operator-1", Now.AddHours(2));
        aggregate.Complete(Now.AddDays(5));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Completed);
        aggregate.UncommittedEvents.Count.ShouldBe(4);
        aggregate.Version.ShouldBe(4);
        aggregate.CompletedAtUtc.ShouldBe(Now.AddDays(5));
    }

    [Fact]
    public void FullLifecycle_Submit_Verify_Extend_Process_Complete_ShouldSucceed()
    {
        // Arrange & Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Access, Now);

        aggregate.Verify("admin-1", Now.AddHours(1));
        aggregate.Extend("Complex case", Now.AddDays(20));
        aggregate.StartProcessing("operator-1", Now.AddDays(21));
        aggregate.Complete(Now.AddDays(50));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Completed);
        aggregate.UncommittedEvents.Count.ShouldBe(5);
        aggregate.Version.ShouldBe(5);
    }

    [Fact]
    public void FullLifecycle_Submit_Deny_ShouldSucceed()
    {
        // Arrange & Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Objection, Now);

        aggregate.Deny("Manifestly unfounded", Now.AddHours(1));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Rejected);
        aggregate.UncommittedEvents.Count.ShouldBe(2);
    }

    [Fact]
    public void FullLifecycle_Submit_Expire_ShouldSucceed()
    {
        // Arrange & Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Restriction, Now);

        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.Status.ShouldBe(DSRRequestStatus.Expired);
        aggregate.UncommittedEvents.Count.ShouldBe(2);
    }

    #endregion

    #region Helpers

    private static DSRRequestAggregate CreateReceivedAggregate()
    {
        return DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Access, Now,
            tenantId: "tenant-1", moduleId: "module-1");
    }

    private static DSRRequestAggregate CreateVerifiedAggregate()
    {
        var aggregate = CreateReceivedAggregate();
        aggregate.Verify("admin-1", Now.AddHours(1));
        return aggregate;
    }

    private static DSRRequestAggregate CreateInProgressAggregate()
    {
        var aggregate = CreateVerifiedAggregate();
        aggregate.StartProcessing("operator-1", Now.AddHours(2));
        return aggregate;
    }

    private static DSRRequestAggregate CreateExtendedAggregate()
    {
        var aggregate = CreateVerifiedAggregate();
        aggregate.Extend("Complex request", Now.AddDays(20));
        return aggregate;
    }

    private static DSRRequestAggregate CreateAggregateInStatus(DSRRequestStatus status)
    {
        return status switch
        {
            DSRRequestStatus.Received => CreateReceivedAggregate(),
            DSRRequestStatus.IdentityVerified => CreateVerifiedAggregate(),
            DSRRequestStatus.InProgress => CreateInProgressAggregate(),
            DSRRequestStatus.Extended => CreateExtendedAggregate(),
            DSRRequestStatus.Completed => CreateCompletedAggregate(),
            DSRRequestStatus.Rejected => CreateRejectedAggregate(),
            DSRRequestStatus.Expired => CreateExpiredAggregate(),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
    }

    private static DSRRequestAggregate CreateCompletedAggregate()
    {
        var aggregate = CreateInProgressAggregate();
        aggregate.Complete(Now.AddDays(5));
        return aggregate;
    }

    private static DSRRequestAggregate CreateRejectedAggregate()
    {
        var aggregate = CreateReceivedAggregate();
        aggregate.Deny("Manifestly unfounded", Now.AddHours(1));
        return aggregate;
    }

    private static DSRRequestAggregate CreateExpiredAggregate()
    {
        var aggregate = CreateReceivedAggregate();
        aggregate.Expire(Now.AddDays(31));
        return aggregate;
    }

    #endregion
}
