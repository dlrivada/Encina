using Encina.Compliance.Consent;
using Shouldly;

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
        attr.Purposes.Count.ShouldBe(1);
        attr.Purposes.ShouldContain(ConsentPurposes.Marketing);
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
        attr.Purposes.Count.ShouldBe(3);
        attr.Purposes.ShouldContain(ConsentPurposes.Marketing);
        attr.Purposes.ShouldContain(ConsentPurposes.Analytics);
        attr.Purposes.ShouldContain(ConsentPurposes.Personalization);
    }

    [Fact]
    public void Constructor_EmptyPurposes_ShouldThrow()
    {
        // Act
        var act = () => new RequireConsentAttribute();

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("purposes");
    }

    [Fact]
    public void Constructor_NullPurposes_ShouldThrow()
    {
        // Act
        var act = () => new RequireConsentAttribute(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region Optional Properties

    [Fact]
    public void SubjectIdProperty_DefaultNull_ShouldBeNull()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing);

        // Assert
        attr.SubjectIdProperty.ShouldBeNull();
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
        attr.SubjectIdProperty.ShouldBe("CustomerId");
    }

    [Fact]
    public void ErrorMessage_DefaultNull_ShouldBeNull()
    {
        // Act
        var attr = new RequireConsentAttribute(ConsentPurposes.Marketing);

        // Assert
        attr.ErrorMessage.ShouldBeNull();
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
        attr.ErrorMessage.ShouldBe("Consent required for marketing");
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
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    #endregion
}
