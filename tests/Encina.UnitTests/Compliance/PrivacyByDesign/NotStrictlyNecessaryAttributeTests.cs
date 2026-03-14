using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="NotStrictlyNecessaryAttribute"/>.
/// </summary>
public class NotStrictlyNecessaryAttributeTests
{
    [Fact]
    public void DefaultSeverity_ShouldBeWarning()
    {
        // Act
        var attribute = new NotStrictlyNecessaryAttribute { Reason = "Test reason" };

        // Assert
        attribute.Severity.Should().Be(MinimizationSeverity.Warning);
    }

    [Fact]
    public void Reason_WhenSet_ShouldStoreValue()
    {
        // Act
        var attribute = new NotStrictlyNecessaryAttribute { Reason = "Analytics only" };

        // Assert
        attribute.Reason.Should().Be("Analytics only");
    }

    [Fact]
    public void Severity_WhenSetToViolation_ShouldStoreValue()
    {
        // Act
        var attribute = new NotStrictlyNecessaryAttribute
        {
            Reason = "Should never be populated",
            Severity = MinimizationSeverity.Violation
        };

        // Assert
        attribute.Severity.Should().Be(MinimizationSeverity.Violation);
    }

    [Fact]
    public void Severity_WhenSetToInfo_ShouldStoreValue()
    {
        // Act
        var attribute = new NotStrictlyNecessaryAttribute
        {
            Reason = "Tracked but not actionable",
            Severity = MinimizationSeverity.Info
        };

        // Assert
        attribute.Severity.Should().Be(MinimizationSeverity.Info);
    }

    [Fact]
    public void AttributeUsage_ShouldTargetPropertyOnly()
    {
        // Arrange
        var usage = typeof(NotStrictlyNecessaryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.Should().Be(AttributeTargets.Property);
    }

    [Fact]
    public void AttributeUsage_ShouldNotAllowMultiple()
    {
        // Arrange
        var usage = typeof(NotStrictlyNecessaryAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.AllowMultiple.Should().BeFalse();
    }
}
