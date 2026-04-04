using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="PersonalDataAttribute"/> and <see cref="RestrictProcessingAttribute"/>
/// verifying default values and property configuration.
/// </summary>
public class PersonalDataAttributeTests
{
    [Fact]
    public void PersonalDataAttribute_DefaultValues()
    {
        var attr = new PersonalDataAttribute();

        attr.Category.ShouldBe(PersonalDataCategory.Other);
        attr.Erasable.ShouldBeTrue();
        attr.Portable.ShouldBeTrue();
        attr.LegalRetention.ShouldBeFalse();
        attr.RetentionReason.ShouldBeNull();
    }

    [Fact]
    public void PersonalDataAttribute_CustomValues()
    {
        var attr = new PersonalDataAttribute
        {
            Category = PersonalDataCategory.Financial,
            Erasable = false,
            Portable = false,
            LegalRetention = true,
            RetentionReason = "Tax compliance"
        };

        attr.Category.ShouldBe(PersonalDataCategory.Financial);
        attr.Erasable.ShouldBeFalse();
        attr.Portable.ShouldBeFalse();
        attr.LegalRetention.ShouldBeTrue();
        attr.RetentionReason.ShouldBe("Tax compliance");
    }

    [Fact]
    public void PersonalDataAttribute_AttributeUsage_AllowsSingleOnProperty()
    {
        var usage = typeof(PersonalDataAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.ShouldBe(AttributeTargets.Property);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void RestrictProcessingAttribute_DefaultValues()
    {
        var attr = new RestrictProcessingAttribute();

        attr.SubjectIdProperty.ShouldBeNull();
    }

    [Fact]
    public void RestrictProcessingAttribute_WithSubjectIdProperty()
    {
        var attr = new RestrictProcessingAttribute { SubjectIdProperty = "CustomerId" };

        attr.SubjectIdProperty.ShouldBe("CustomerId");
    }

    [Fact]
    public void RestrictProcessingAttribute_AttributeUsage_AllowsSingleOnClass()
    {
        var usage = typeof(RestrictProcessingAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }
}
