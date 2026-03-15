using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Projections;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Projections;

/// <summary>
/// Unit tests for <see cref="DSRRequestReadModel"/>.
/// </summary>
public class DSRRequestReadModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Deadline = Now.AddDays(30);

    #region IsOverdue

    [Fact]
    public void IsOverdue_BeforeDeadline_ShouldReturnFalse()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Received);

        // Act / Assert
        model.IsOverdue(Now.AddDays(15)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_AfterDeadline_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Received);

        // Act / Assert
        model.IsOverdue(Now.AddDays(31)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_ExactlyAtDeadline_ShouldReturnFalse()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Received);

        // Act / Assert
        model.IsOverdue(Deadline).Should().BeFalse();
    }

    [Theory]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Expired)]
    public void IsOverdue_TerminalStatus_ShouldReturnFalse(DSRRequestStatus status)
    {
        // Arrange
        var model = CreateModel(status);

        // Act / Assert
        model.IsOverdue(Now.AddDays(60)).Should().BeFalse();
    }

    [Theory]
    [InlineData(DSRRequestStatus.Received)]
    [InlineData(DSRRequestStatus.IdentityVerified)]
    [InlineData(DSRRequestStatus.InProgress)]
    [InlineData(DSRRequestStatus.Extended)]
    public void IsOverdue_ActiveStatus_AfterDeadline_ShouldReturnTrue(DSRRequestStatus status)
    {
        // Arrange
        var model = CreateModel(status);

        // Act / Assert
        model.IsOverdue(Now.AddDays(31)).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WithExtendedDeadline_BeforeExtended_ShouldReturnFalse()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Extended);
        model.ExtendedDeadlineAtUtc = Deadline.AddMonths(2);

        // Act / Assert — past original deadline but before extended
        model.IsOverdue(Now.AddDays(45)).Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WithExtendedDeadline_AfterExtended_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Extended);
        var extendedDeadline = Deadline.AddMonths(2);
        model.ExtendedDeadlineAtUtc = extendedDeadline;

        // Act / Assert
        model.IsOverdue(extendedDeadline.AddDays(1)).Should().BeTrue();
    }

    #endregion

    #region HasActiveRestriction

    [Fact]
    public void HasActiveRestriction_RestrictionReceived_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Received, DataSubjectRight.Restriction);

        // Act / Assert
        model.HasActiveRestriction.Should().BeTrue();
    }

    [Fact]
    public void HasActiveRestriction_RestrictionIdentityVerified_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.IdentityVerified, DataSubjectRight.Restriction);

        // Act / Assert
        model.HasActiveRestriction.Should().BeTrue();
    }

    [Fact]
    public void HasActiveRestriction_RestrictionInProgress_ShouldReturnTrue()
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.InProgress, DataSubjectRight.Restriction);

        // Act / Assert
        model.HasActiveRestriction.Should().BeTrue();
    }

    [Theory]
    [InlineData(DSRRequestStatus.Completed)]
    [InlineData(DSRRequestStatus.Rejected)]
    [InlineData(DSRRequestStatus.Expired)]
    [InlineData(DSRRequestStatus.Extended)]
    public void HasActiveRestriction_RestrictionTerminalOrExtended_ShouldReturnFalse(DSRRequestStatus status)
    {
        // Arrange
        var model = CreateModel(status, DataSubjectRight.Restriction);

        // Act / Assert
        model.HasActiveRestriction.Should().BeFalse();
    }

    [Theory]
    [InlineData(DataSubjectRight.Access)]
    [InlineData(DataSubjectRight.Rectification)]
    [InlineData(DataSubjectRight.Erasure)]
    [InlineData(DataSubjectRight.Portability)]
    [InlineData(DataSubjectRight.Objection)]
    public void HasActiveRestriction_NonRestrictionRight_ShouldReturnFalse(DataSubjectRight rightType)
    {
        // Arrange
        var model = CreateModel(DSRRequestStatus.Received, rightType);

        // Act / Assert
        model.HasActiveRestriction.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private static DSRRequestReadModel CreateModel(
        DSRRequestStatus status,
        DataSubjectRight rightType = DataSubjectRight.Access)
    {
        return new DSRRequestReadModel
        {
            Id = Guid.NewGuid(),
            SubjectId = "subject-1",
            RightType = rightType,
            Status = status,
            ReceivedAtUtc = Now,
            DeadlineAtUtc = Deadline,
            LastModifiedAtUtc = Now,
            Version = 1
        };
    }

    #endregion
}
