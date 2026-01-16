using Encina.Testing.Architecture;
using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Tests for <see cref="ArchitectureVerificationResult"/>.
/// </summary>
public sealed class ArchitectureVerificationResultTests
{
    [Fact]
    public void Constructor_WithEmptyViolations_IsSuccess()
    {
        // Arrange
        var violations = Array.Empty<ArchitectureRuleViolation>();

        // Act
        var result = new ArchitectureVerificationResult(violations);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Violations.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithViolations_IsFailure()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("Rule1", "Violation message 1"),
            new ArchitectureRuleViolation("Rule2", "Violation message 2")
        };

        // Act
        var result = new ArchitectureVerificationResult(violations);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Violations.Count.ShouldBe(2);
    }

    [Fact]
    public void Violations_IsReadOnly()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("Rule1", "Message1")
        };

        // Act
        var result = new ArchitectureVerificationResult(violations);

        // Assert
        var list = result.Violations.ShouldBeAssignableTo<IList<ArchitectureRuleViolation>>();
        list.IsReadOnly.ShouldBeTrue();
    }
}
