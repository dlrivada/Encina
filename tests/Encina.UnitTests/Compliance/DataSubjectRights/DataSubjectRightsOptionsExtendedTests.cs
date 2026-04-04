using System.Reflection;

using Encina.Compliance.DataSubjectRights;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Extended unit tests for <see cref="DataSubjectRightsOptions"/> covering all properties,
/// defaults, and collection behaviors.
/// </summary>
public class DataSubjectRightsOptionsExtendedTests
{
    [Fact]
    public void Defaults_AllPropertiesHaveExpectedValues()
    {
        var options = new DataSubjectRightsOptions();

        options.RestrictionEnforcementMode.ShouldBe(DSREnforcementMode.Block);
        options.AddHealthCheck.ShouldBeFalse();
        options.AutoRegisterFromAttributes.ShouldBeTrue();
        options.PublishNotifications.ShouldBeTrue();
        options.DefaultDeadlineDays.ShouldBe(30);
        options.MaxExtensionDays.ShouldBe(60);
        options.TrackAuditTrail.ShouldBeTrue();
        options.AssembliesToScan.ShouldNotBeNull();
        options.AssembliesToScan.Count.ShouldBe(0);
        options.DefaultErasableCategories.ShouldNotBeNull();
        options.DefaultErasableCategories.Count.ShouldBe(0);
        options.DefaultPortableCategories.ShouldNotBeNull();
        options.DefaultPortableCategories.Count.ShouldBe(0);
    }

    [Fact]
    public void AssembliesToScan_CanBePopulated()
    {
        var options = new DataSubjectRightsOptions();
        options.AssembliesToScan.Add(typeof(DataSubjectRightsOptions).Assembly);

        options.AssembliesToScan.Count.ShouldBe(1);
    }

    [Fact]
    public void DefaultErasableCategories_CanBePopulated()
    {
        var options = new DataSubjectRightsOptions();
        options.DefaultErasableCategories.Add(PersonalDataCategory.Contact);
        options.DefaultErasableCategories.Add(PersonalDataCategory.Identity);

        options.DefaultErasableCategories.Count.ShouldBe(2);
        options.DefaultErasableCategories.ShouldContain(PersonalDataCategory.Contact);
    }

    [Fact]
    public void DefaultPortableCategories_CanBePopulated()
    {
        var options = new DataSubjectRightsOptions();
        options.DefaultPortableCategories.Add(PersonalDataCategory.Financial);

        options.DefaultPortableCategories.Count.ShouldBe(1);
    }

    [Fact]
    public void PublishNotifications_CanBeDisabled()
    {
        var options = new DataSubjectRightsOptions { PublishNotifications = false };

        options.PublishNotifications.ShouldBeFalse();
    }

    [Fact]
    public void TrackAuditTrail_CanBeDisabled()
    {
        var options = new DataSubjectRightsOptions { TrackAuditTrail = false };

        options.TrackAuditTrail.ShouldBeFalse();
    }

    [Fact]
    public void DefaultDeadlineDays_CanBeCustomized()
    {
        var options = new DataSubjectRightsOptions { DefaultDeadlineDays = 45 };

        options.DefaultDeadlineDays.ShouldBe(45);
    }

    [Fact]
    public void MaxExtensionDays_CanBeCustomized()
    {
        var options = new DataSubjectRightsOptions { MaxExtensionDays = 30 };

        options.MaxExtensionDays.ShouldBe(30);
    }
}
