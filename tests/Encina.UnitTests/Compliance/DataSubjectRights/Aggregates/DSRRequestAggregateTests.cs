using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Events;
using FluentAssertions;

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
        aggregate.Id.Should().Be(DefaultId);
        aggregate.SubjectId.Should().Be("subject-1");
        aggregate.RightType.Should().Be(DataSubjectRight.Access);
        aggregate.Status.Should().Be(DSRRequestStatus.Received);
        aggregate.ReceivedAtUtc.Should().Be(Now);
        aggregate.DeadlineAtUtc.Should().Be(Now.AddDays(30));
        aggregate.RequestDetails.Should().Be("I want my data");
        aggregate.TenantId.Should().Be("tenant-1");
        aggregate.ModuleId.Should().Be("module-1");
        aggregate.CompletedAtUtc.Should().BeNull();
        aggregate.VerifiedAtUtc.Should().BeNull();
        aggregate.ExtensionReason.Should().BeNull();
        aggregate.ExtendedDeadlineAtUtc.Should().BeNull();
        aggregate.RejectionReason.Should().BeNull();
        aggregate.ProcessedByUserId.Should().BeNull();
    }

    [Fact]
    public void Submit_ValidParameters_ShouldRaiseDSRRequestSubmittedEvent()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Erasure, Now);

        // Assert
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DSRRequestSubmitted>();
        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void Submit_SubmittedEvent_ShouldContainCorrectData()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Portability, Now,
            "export request", "tenant-1", "module-1");

        // Assert
        var @event = aggregate.UncommittedEvents.Single().Should().BeOfType<DSRRequestSubmitted>().Subject;
        @event.RequestId.Should().Be(DefaultId);
        @event.SubjectId.Should().Be("subject-1");
        @event.RightType.Should().Be(DataSubjectRight.Portability);
        @event.ReceivedAtUtc.Should().Be(Now);
        @event.DeadlineAtUtc.Should().Be(Now.AddDays(30));
        @event.RequestDetails.Should().Be("export request");
        @event.TenantId.Should().Be("tenant-1");
        @event.ModuleId.Should().Be("module-1");
    }

    [Fact]
    public void Submit_NullableParametersAsNull_ShouldCreateAggregate()
    {
        // Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Access, Now);

        // Assert
        aggregate.RequestDetails.Should().BeNull();
        aggregate.TenantId.Should().BeNull();
        aggregate.ModuleId.Should().BeNull();
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
        aggregate.DeadlineAtUtc.Should().Be(receivedAt.AddDays(30));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_InvalidSubjectId_ShouldThrow(string? subjectId)
    {
        // Act
        var act = () => DSRRequestAggregate.Submit(
            DefaultId, subjectId!, DataSubjectRight.Access, Now);

        // Assert
        act.Should().Throw<ArgumentException>();
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
        aggregate.RightType.Should().Be(rightType);
        aggregate.Status.Should().Be(DSRRequestStatus.Received);
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
        aggregate.Status.Should().Be(DSRRequestStatus.IdentityVerified);
        aggregate.VerifiedAtUtc.Should().Be(verifiedAt);
    }

    [Fact]
    public void Verify_ShouldRaiseDSRRequestVerifiedEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Verify("admin-1", Now.AddHours(1));

        // Assert
        aggregate.UncommittedEvents.Should().HaveCount(2);
        aggregate.UncommittedEvents[^1].Should().BeOfType<DSRRequestVerified>();
        aggregate.Version.Should().Be(2);
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
        var act = () => aggregate.Verify(verifiedBy!, Now.AddHours(1));

        // Assert
        act.Should().Throw<ArgumentException>();
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
        var act = () => aggregate.Verify("admin-1", Now.AddHours(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{invalidStatus}'*");
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
        aggregate.Status.Should().Be(DSRRequestStatus.InProgress);
        aggregate.ProcessedByUserId.Should().Be("operator-1");
    }

    [Fact]
    public void StartProcessing_FromExtended_ShouldTransitionToInProgress()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        aggregate.StartProcessing("operator-1", Now.AddHours(2));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.InProgress);
    }

    [Fact]
    public void StartProcessing_NullUserId_ShouldAllowAutomatedProcessing()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.StartProcessing(null, Now.AddHours(2));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.InProgress);
        aggregate.ProcessedByUserId.Should().BeNull();
    }

    [Fact]
    public void StartProcessing_ShouldRaiseDSRRequestProcessingEvent()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.StartProcessing("operator-1", Now.AddHours(2));

        // Assert
        aggregate.UncommittedEvents[^1].Should().BeOfType<DSRRequestProcessing>();
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
        var act = () => aggregate.StartProcessing("operator-1", Now.AddHours(2));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{invalidStatus}'*");
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
        aggregate.Status.Should().Be(DSRRequestStatus.Completed);
        aggregate.CompletedAtUtc.Should().Be(completedAt);
    }

    [Fact]
    public void Complete_ShouldRaiseDSRRequestCompletedEvent()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Complete(Now.AddDays(5));

        // Assert
        aggregate.UncommittedEvents[^1].Should().BeOfType<DSRRequestCompleted>();
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
        var act = () => aggregate.Complete(Now.AddDays(5));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{invalidStatus}'*");
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
        aggregate.Status.Should().Be(DSRRequestStatus.Rejected);
        aggregate.RejectionReason.Should().Be("Manifestly unfounded request");
        aggregate.CompletedAtUtc.Should().Be(deniedAt);
    }

    [Fact]
    public void Deny_FromIdentityVerified_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.Deny("Cannot verify identity sufficiently", Now.AddHours(2));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Rejected);
    }

    [Fact]
    public void Deny_FromInProgress_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Deny("Exemption applies", Now.AddDays(5));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Rejected);
    }

    [Fact]
    public void Deny_FromExtended_ShouldTransitionToRejected()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        aggregate.Deny("No longer applicable", Now.AddDays(35));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Rejected);
    }

    [Fact]
    public void Deny_ShouldRaiseDSRRequestDeniedEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Deny("Reason", Now.AddHours(1));

        // Assert
        aggregate.UncommittedEvents[^1].Should().BeOfType<DSRRequestDenied>();
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
        var act = () => aggregate.Deny(reason!, Now.AddHours(1));

        // Assert
        act.Should().Throw<ArgumentException>();
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
        var act = () => aggregate.Deny("Reason", Now.AddHours(1));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{terminalStatus}'*");
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
        aggregate.Status.Should().Be(DSRRequestStatus.Extended);
        aggregate.ExtensionReason.Should().Be("Complex request requiring more time");
        aggregate.ExtendedDeadlineAtUtc.Should().Be(aggregate.DeadlineAtUtc.AddMonths(2));
    }

    [Fact]
    public void Extend_FromIdentityVerified_ShouldTransitionToExtended()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.Extend("Multiple systems to query", Now.AddDays(10));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Extended);
    }

    [Fact]
    public void Extend_FromInProgress_ShouldTransitionToExtended()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Extend("Data scattered across jurisdictions", Now.AddDays(15));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Extended);
    }

    [Fact]
    public void Extend_ShouldRaiseDSRRequestExtendedEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Extend("Reason", Now.AddDays(20));

        // Assert
        aggregate.UncommittedEvents[^1].Should().BeOfType<DSRRequestExtended>();
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
        var act = () => aggregate.Extend(reason!, Now.AddDays(20));

        // Assert
        act.Should().Throw<ArgumentException>();
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
        var act = () => aggregate.Extend("Reason", Now.AddDays(20));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{invalidStatus}'*");
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
        aggregate.Status.Should().Be(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_FromIdentityVerified_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateVerifiedAggregate();

        // Act
        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_FromInProgress_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();

        // Act
        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_FromExtended_ShouldTransitionToExpired()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        aggregate.Expire(Now.AddDays(91));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Expired);
    }

    [Fact]
    public void Expire_ShouldRaiseDSRRequestExpiredEvent()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act
        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.UncommittedEvents[^1].Should().BeOfType<DSRRequestExpired>();
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
        var act = () => aggregate.Expire(Now.AddDays(31));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{terminalStatus}'*");
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
        deadline.Should().Be(aggregate.DeadlineAtUtc);
    }

    [Fact]
    public void GetEffectiveDeadline_WithExtension_ShouldReturnExtendedDeadline()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act
        var deadline = aggregate.GetEffectiveDeadline();

        // Assert
        deadline.Should().Be(aggregate.ExtendedDeadlineAtUtc!.Value);
    }

    #endregion

    #region IsOverdue

    [Fact]
    public void IsOverdue_BeforeDeadline_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_AfterDeadline_ShouldReturnTrue()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(31)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenCompleted_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateInProgressAggregate();
        aggregate.Complete(Now.AddDays(5));

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenRejected_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        aggregate.Deny("Reason", Now.AddHours(1));

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateReceivedAggregate();
        aggregate.Expire(Now.AddDays(31));

        // Act / Assert
        aggregate.IsOverdue(Now.AddDays(60)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenExtended_BeforeExtendedDeadline_ShouldReturnFalse()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();

        // Act / Assert — extended deadline is original + 2 months
        aggregate.IsOverdue(Now.AddDays(45)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenExtended_AfterExtendedDeadline_ShouldReturnTrue()
    {
        // Arrange
        var aggregate = CreateExtendedAggregate();
        var extendedDeadline = aggregate.ExtendedDeadlineAtUtc!.Value;

        // Act / Assert
        aggregate.IsOverdue(extendedDeadline.AddDays(1)).Should().BeTrue();
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
        aggregate.Status.Should().Be(DSRRequestStatus.Completed);
        aggregate.UncommittedEvents.Should().HaveCount(4);
        aggregate.Version.Should().Be(4);
        aggregate.CompletedAtUtc.Should().Be(Now.AddDays(5));
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
        aggregate.Status.Should().Be(DSRRequestStatus.Completed);
        aggregate.UncommittedEvents.Should().HaveCount(5);
        aggregate.Version.Should().Be(5);
    }

    [Fact]
    public void FullLifecycle_Submit_Deny_ShouldSucceed()
    {
        // Arrange & Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Objection, Now);

        aggregate.Deny("Manifestly unfounded", Now.AddHours(1));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Rejected);
        aggregate.UncommittedEvents.Should().HaveCount(2);
    }

    [Fact]
    public void FullLifecycle_Submit_Expire_ShouldSucceed()
    {
        // Arrange & Act
        var aggregate = DSRRequestAggregate.Submit(
            DefaultId, "subject-1", DataSubjectRight.Restriction, Now);

        aggregate.Expire(Now.AddDays(31));

        // Assert
        aggregate.Status.Should().Be(DSRRequestStatus.Expired);
        aggregate.UncommittedEvents.Should().HaveCount(2);
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
