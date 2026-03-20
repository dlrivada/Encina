using Encina.Compliance.AIAct;
using Encina.Compliance.AIAct.Model;
using FluentAssertions;
using LanguageExt;

namespace Encina.UnitTests.Compliance.AIAct;

/// <summary>
/// Unit tests for <see cref="DefaultHumanOversightEnforcer"/>.
/// </summary>
public class DefaultHumanOversightEnforcerTests
{
    private readonly DefaultHumanOversightEnforcer _sut = new();

    // -- RequiresHumanReviewAsync --

    [Fact]
    public async Task RequiresHumanReviewAsync_WithAttribute_ShouldReturnTrue()
    {
        // Arrange
        var request = new SampleOversightRequest();

        // Act
        var result = await _sut.RequiresHumanReviewAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var requires = (bool)result;
        requires.Should().BeTrue();
    }

    [Fact]
    public async Task RequiresHumanReviewAsync_WithoutAttribute_ShouldReturnFalse()
    {
        // Arrange
        var request = new SampleNoAttributeRequest();

        // Act
        var result = await _sut.RequiresHumanReviewAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var requires = (bool)result;
        requires.Should().BeFalse();
    }

    // -- RecordHumanDecisionAsync --

    [Fact]
    public async Task RecordHumanDecisionAsync_ValidDecision_ShouldSucceed()
    {
        // Arrange
        var decision = CreateDecision();

        // Act
        var result = await _sut.RecordHumanDecisionAsync(decision);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    // -- HasHumanApprovalAsync --

    [Fact]
    public async Task HasHumanApprovalAsync_RecordedDecision_ShouldReturnTrue()
    {
        // Arrange
        var decision = CreateDecision();
        await _sut.RecordHumanDecisionAsync(decision);

        // Act
        var result = await _sut.HasHumanApprovalAsync(decision.DecisionId);

        // Assert
        result.IsRight.Should().BeTrue();
        var exists = (bool)result;
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task HasHumanApprovalAsync_UnknownDecision_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.HasHumanApprovalAsync(Guid.NewGuid());

        // Assert
        result.IsRight.Should().BeTrue();
        var exists = (bool)result;
        exists.Should().BeFalse();
    }

    // -- Helper --

    private static HumanDecisionRecord CreateDecision() => new()
    {
        DecisionId = Guid.NewGuid(),
        SystemId = "test-system",
        ReviewerId = "reviewer-1",
        ReviewedAtUtc = DateTimeOffset.UtcNow,
        Decision = "approved",
        Rationale = "All checks passed"
    };
}
