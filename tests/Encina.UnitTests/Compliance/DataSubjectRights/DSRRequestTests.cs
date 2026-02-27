using Encina.Compliance.DataSubjectRights;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="DSRRequest"/> factory method and record behavior.
/// </summary>
public class DSRRequestTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_ShouldSetReceivedStatus()
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, DateTimeOffset.UtcNow);

        // Assert
        request.Status.Should().Be(DSRRequestStatus.Received);
    }

    [Fact]
    public void Create_ShouldCalculate30DayDeadline()
    {
        // Arrange
        var receivedAt = new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero);

        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Erasure, receivedAt);

        // Assert
        request.DeadlineAtUtc.Should().Be(receivedAt.AddDays(30));
    }

    [Fact]
    public void Create_ShouldSetIdAndSubjectId()
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Portability, DateTimeOffset.UtcNow);

        // Assert
        request.Id.Should().Be("req-001");
        request.SubjectId.Should().Be("subject-1");
    }

    [Fact]
    public void Create_ShouldSetRightType()
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Objection, DateTimeOffset.UtcNow);

        // Assert
        request.RightType.Should().Be(DataSubjectRight.Objection);
    }

    [Fact]
    public void Create_WithRequestDetails_ShouldSetDetails()
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, DateTimeOffset.UtcNow, "I want my data");

        // Assert
        request.RequestDetails.Should().Be("I want my data");
    }

    [Fact]
    public void Create_WithoutRequestDetails_ShouldBeNull()
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, DateTimeOffset.UtcNow);

        // Assert
        request.RequestDetails.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldLeaveOptionalPropertiesNull()
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, DateTimeOffset.UtcNow);

        // Assert
        request.CompletedAtUtc.Should().BeNull();
        request.ExtensionReason.Should().BeNull();
        request.ExtendedDeadlineAtUtc.Should().BeNull();
        request.RejectionReason.Should().BeNull();
        request.VerifiedAtUtc.Should().BeNull();
        request.ProcessedByUserId.Should().BeNull();
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
    public void Create_AllRightTypes_ShouldSucceed(DataSubjectRight rightType)
    {
        // Act
        var request = DSRRequest.Create("req-001", "subject-1", rightType, DateTimeOffset.UtcNow);

        // Assert
        request.RightType.Should().Be(rightType);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var receivedAt = new DateTimeOffset(2026, 2, 27, 12, 0, 0, TimeSpan.Zero);

        // Act
        var request1 = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, receivedAt);
        var request2 = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, receivedAt);

        // Assert
        request1.Should().Be(request2);
    }

    [Fact]
    public void With_ShouldCreateModifiedCopy()
    {
        // Arrange
        var request = DSRRequest.Create("req-001", "subject-1", DataSubjectRight.Access, DateTimeOffset.UtcNow);

        // Act
        var updated = request with { Status = DSRRequestStatus.Completed };

        // Assert
        updated.Status.Should().Be(DSRRequestStatus.Completed);
        request.Status.Should().Be(DSRRequestStatus.Received); // Original unchanged
    }

    #endregion
}
