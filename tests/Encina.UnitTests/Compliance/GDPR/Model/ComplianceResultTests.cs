using Encina.Compliance.GDPR;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.GDPR.Model;

/// <summary>
/// Unit tests for <see cref="ComplianceResult"/>.
/// </summary>
public class ComplianceResultTests
{
    [Fact]
    public void Compliant_ShouldBeCompliantWithNoErrorsOrWarnings()
    {
        // Act
        var result = ComplianceResult.Compliant();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void CompliantWithWarnings_ShouldBeCompliantWithWarnings()
    {
        // Act
        var result = ComplianceResult.CompliantWithWarnings("Retention near limit", "Optional safeguards missing");

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().HaveCount(2);
        result.Warnings.Should().Contain("Retention near limit");
    }

    [Fact]
    public void NonCompliant_WithErrors_ShouldNotBeCompliant()
    {
        // Act
        var result = ComplianceResult.NonCompliant("Missing consent", "No lawful basis");

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Missing consent");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void NonCompliant_WithErrorsAndWarnings_ShouldHaveBoth()
    {
        // Act
        var result = ComplianceResult.NonCompliant(
            ["Critical error"],
            ["Non-critical warning"]);

        // Assert
        result.IsCompliant.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public void CompliantWithWarnings_NoArgs_ShouldBeCompliantWithEmptyWarnings()
    {
        // Act
        var result = ComplianceResult.CompliantWithWarnings();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
    }
}
