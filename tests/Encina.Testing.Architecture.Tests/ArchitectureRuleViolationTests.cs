namespace Encina.Testing.Architecture.Tests;

/// <summary>
/// Tests for <see cref="ArchitectureRuleViolation"/>.
/// </summary>
public sealed class ArchitectureRuleViolationTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        const string ruleName = "TestRule";
        const string message = "Test violation message";

        // Act
        var violation = new ArchitectureRuleViolation(ruleName, message);

        // Assert
        violation.RuleName.ShouldBe(ruleName);
        violation.Message.ShouldBe(message);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var violation1 = new ArchitectureRuleViolation("Rule", "Message");
        var violation2 = new ArchitectureRuleViolation("Rule", "Message");
        var violation3 = new ArchitectureRuleViolation("DifferentRule", "Message");

        // Assert
        violation1.ShouldBe(violation2);
        violation1.ShouldNotBe(violation3);
    }

    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        var violation1 = new ArchitectureRuleViolation("Rule", "Message");
        var violation2 = new ArchitectureRuleViolation("Rule", "Message");

        // Assert
        violation1.GetHashCode().ShouldBe(violation2.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsRuleNameAndMessage()
    {
        // Arrange
        var violation = new ArchitectureRuleViolation("TestRule", "Test message");

        // Act
        var str = violation.ToString();

        // Assert
        str.ShouldContain("TestRule");
        str.ShouldContain("Test message");
    }
}
