using Encina.Compliance.GDPR;
using Shouldly;

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
        result.IsCompliant.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void CompliantWithWarnings_ShouldBeCompliantWithWarnings()
    {
        // Act
        var result = ComplianceResult.CompliantWithWarnings("Retention near limit", "Optional safeguards missing");

        // Assert
        result.IsCompliant.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Warnings.Count.ShouldBe(2);
        result.Warnings.ShouldContain("Retention near limit");
    }

    [Fact]
    public void NonCompliant_WithErrors_ShouldNotBeCompliant()
    {
        // Act
        var result = ComplianceResult.NonCompliant("Missing consent", "No lawful basis");

        // Assert
        result.IsCompliant.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldContain("Missing consent");
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void NonCompliant_WithErrorsAndWarnings_ShouldHaveBoth()
    {
        // Act
        var result = ComplianceResult.NonCompliant(
            ["Critical error"],
            ["Non-critical warning"]);

        // Assert
        result.IsCompliant.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Warnings.Count.ShouldBe(1);
    }

    [Fact]
    public void CompliantWithWarnings_NoArgs_ShouldBeCompliantWithEmptyWarnings()
    {
        // Act
        var result = ComplianceResult.CompliantWithWarnings();

        // Assert
        result.IsCompliant.ShouldBeTrue();
        result.Warnings.ShouldBeEmpty();
    }
}
