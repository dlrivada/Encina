using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// Unit tests for <see cref="RequireConsentAttribute"/>.
/// </summary>
public class RequireConsentAttributeTests
{
    #region Constructor

    [Fact]
    public void Constructor_SinglePurpose_ShouldSetPurposes()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing);

        // Assert
        attr.Purposes.Should().HaveCount(1);
        attr.Purposes.Should().Contain(ConsentPurposes.Marketing);
    }

    [Fact]
    public void Constructor_MultiplePurposes_ShouldSetAllPurposes()
    {
        // Act
        var attr = new RequireConsentAttribute(
            ConsentPurposes.Marketing,
            ConsentPurposes.Analytics,
            ConsentPurposes.Personalization);

        // Assert
        attr.Purposes.Should().HaveCount(3);
        attr.Purposes.Should().Contain(ConsentPurposes.Marketing);
        attr.Purposes.Should().Contain(ConsentPurposes.Analytics);
        attr.Purposes.Should().Contain(ConsentPurposes.Personalization);
    }

    [Fact]
    public void Constructor_EmptyPurposes_ShouldThrow()
    {
        // Act
        var act = () => new RequireConsentAttribute();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("purposes");
    }

    [Fact]
    public void Constructor_NullPurposes_ShouldThrow()
    {
        // Act
        var act = () => new RequireConsentAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Optional Properties

    [Fact]
    public void SubjectIdProperty_DefaultNull_ShouldBeNull()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing);

        // Assert
        attr.SubjectIdProperty.Should().BeNull();
    }

    [Fact]
    public void SubjectIdProperty_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing)
        {
            SubjectIdProperty = "CustomerId"
        };

        // Assert
        attr.SubjectIdProperty.Should().Be("CustomerId");
    }

    [Fact]
    public void ErrorMessage_DefaultNull_ShouldBeNull()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing);

        // Assert
        attr.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ErrorMessage_WhenSet_ShouldReturnValue()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing)
        {
            ErrorMessage = "Consent required for marketing"
        };

        // Assert
        attr.ErrorMessage.Should().Be("Consent required for marketing");
    }

    #endregion

    #region AttributeUsage

    [Fact]
    public void AttributeUsage_ShouldTargetClasses()
    {
        // Act
        var usage = typeof(RequireConsentAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        usage.ValidOn.Should().Be(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }

    #endregion
}
