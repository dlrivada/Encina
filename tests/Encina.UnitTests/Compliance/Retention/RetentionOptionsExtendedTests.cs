using Encina.Compliance.Retention;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

public class RetentionOptionsExtendedTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new RetentionOptions();

        options.DefaultRetentionPeriod.ShouldBeNull();
        options.AlertBeforeExpirationDays.ShouldBe(30);
        options.PublishNotifications.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
        options.EnableAutomaticEnforcement.ShouldBeTrue();
        options.EnforcementInterval.ShouldBe(TimeSpan.FromMinutes(60));
        options.EnforcementMode.ShouldBe(RetentionEnforcementMode.Warn);
        options.AutoRegisterFromAttributes.ShouldBeTrue();
        options.AssembliesToScan.ShouldBeEmpty();
        options.ConfiguredPolicies.ShouldBeEmpty();
    }

    [Fact]
    public void AddPolicy_BasicConfiguration_AddsPolicyDescriptor()
    {
        var options = new RetentionOptions();

        options.AddPolicy("user-profiles", policy =>
        {
            policy.RetainForDays(365);
            policy.WithAutoDelete();
            policy.WithReason("GDPR storage limitation");
        });

        options.ConfiguredPolicies.Count.ShouldBe(1);
        options.ConfiguredPolicies[0].DataCategory.ShouldBe("user-profiles");
        options.ConfiguredPolicies[0].RetentionPeriod.ShouldBe(TimeSpan.FromDays(365));
        options.ConfiguredPolicies[0].AutoDelete.ShouldBeTrue();
        options.ConfiguredPolicies[0].Reason.ShouldBe("GDPR storage limitation");
    }

    [Fact]
    public void AddPolicy_RetainForYears_SetsCorrectPeriod()
    {
        var options = new RetentionOptions();

        options.AddPolicy("audit-logs", policy =>
        {
            policy.RetainForYears(7);
        });

        options.ConfiguredPolicies[0].RetentionPeriod.ShouldBe(TimeSpan.FromDays(7 * 365));
    }

    [Fact]
    public void AddPolicy_RetainFor_SetsCustomPeriod()
    {
        var options = new RetentionOptions();

        options.AddPolicy("temp-data", policy =>
        {
            policy.RetainFor(TimeSpan.FromHours(48));
        });

        options.ConfiguredPolicies[0].RetentionPeriod.ShouldBe(TimeSpan.FromHours(48));
    }

    [Fact]
    public void AddPolicy_WithAutoDeleteFalse_DisablesAutoDelete()
    {
        var options = new RetentionOptions();

        options.AddPolicy("legal-data", policy =>
        {
            policy.RetainForYears(10);
            policy.WithAutoDelete(false);
        });

        options.ConfiguredPolicies[0].AutoDelete.ShouldBeFalse();
    }

    [Fact]
    public void AddPolicy_WithLegalBasis_SetsLegalBasis()
    {
        var options = new RetentionOptions();

        options.AddPolicy("tax-records", policy =>
        {
            policy.RetainForYears(7);
            policy.WithLegalBasis("Tax law requirement");
        });

        options.ConfiguredPolicies[0].LegalBasis.ShouldBe("Tax law requirement");
    }

    [Fact]
    public void AddPolicy_Chaining_WorksCorrectly()
    {
        var options = new RetentionOptions();

        options
            .AddPolicy("data-1", p => p.RetainForDays(30))
            .AddPolicy("data-2", p => p.RetainForDays(60));

        options.ConfiguredPolicies.Count.ShouldBe(2);
    }

    [Fact]
    public void AddPolicy_NullDataCategory_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions();

        var act = () => options.AddPolicy(null!, _ => { });

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddPolicy_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions();

        var act = () => options.AddPolicy("test", null!);

        Should.Throw<ArgumentNullException>(act);
    }
}
