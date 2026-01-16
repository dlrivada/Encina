using Encina.Testing.Architecture;
using Shouldly;

namespace Encina.UnitTests.Testing.Architecture;

/// <summary>
/// Tests for <see cref="ArchitectureRuleException"/>.
/// </summary>
public sealed class ArchitectureRuleExceptionTests
{
    [Fact]
    public void Constructor_WithSingleViolation_FormatsMessageCorrectly()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("TestRule", "Test violation message")
        };

        // Act
        var exception = new ArchitectureRuleException(violations);

        // Assert
        exception.Message.ShouldContain("1 violation");
        exception.Message.ShouldContain("TestRule");
        exception.Message.ShouldContain("Test violation message");
    }

    [Fact]
    public void Constructor_WithMultipleViolations_FormatsMessageCorrectly()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("Rule1", "Message 1"),
            new ArchitectureRuleViolation("Rule2", "Message 2"),
            new ArchitectureRuleViolation("Rule3", "Message 3")
        };

        // Act
        var exception = new ArchitectureRuleException(violations);

        // Assert
        exception.Message.ShouldContain("3 violation(s)");
        exception.Message.ShouldContain("Rule1");
        exception.Message.ShouldContain("Rule2");
        exception.Message.ShouldContain("Rule3");
        exception.Message.ShouldContain("[1]");
        exception.Message.ShouldContain("[2]");
        exception.Message.ShouldContain("[3]");
    }

    [Fact]
    public void Violations_ContainsAllViolations()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("Rule1", "Message 1"),
            new ArchitectureRuleViolation("Rule2", "Message 2")
        };

        // Act
        var exception = new ArchitectureRuleException(violations);

        // Assert
        exception.Violations.Count.ShouldBe(2);
        exception.Violations.ShouldContain(v => v.RuleName == "Rule1");
        exception.Violations.ShouldContain(v => v.RuleName == "Rule2");
    }

    [Fact]
    public void Violations_IsReadOnly()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("Rule1", "Message 1")
        };

        // Act
        var exception = new ArchitectureRuleException(violations);

        // Assert
        var list = (IList<ArchitectureRuleViolation>)exception.Violations;
        list.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void Exception_IsOfTypeException()
    {
        // Arrange
        var violations = new[]
        {
            new ArchitectureRuleViolation("Rule1", "Message 1")
        };

        // Act
        var exception = new ArchitectureRuleException(violations);

        // Assert
        exception.ShouldBeAssignableTo<Exception>();
    }
}
