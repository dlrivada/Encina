using Encina.Compliance.Retention;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

public class RetentionOptionsExtendedTests
{
    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        var options = new RetentionOptions();

        options.DefaultRetentionPeriod.Should().BeNull();
        options.AlertBeforeExpirationDays.Should().Be(30);
        options.PublishNotifications.Should().BeTrue();
        options.AddHealthCheck.Should().BeFalse();
        options.EnableAutomaticEnforcement.Should().BeTrue();
        options.EnforcementInterval.Should().Be(TimeSpan.FromMinutes(60));
        options.EnforcementMode.Should().Be(RetentionEnforcementMode.Warn);
        options.AutoRegisterFromAttributes.Should().BeTrue();
        options.AssembliesToScan.Should().BeEmpty();
        options.ConfiguredPolicies.Should().BeEmpty();
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

        options.ConfiguredPolicies.Should().HaveCount(1);
        options.ConfiguredPolicies[0].DataCategory.Should().Be("user-profiles");
        options.ConfiguredPolicies[0].RetentionPeriod.Should().Be(TimeSpan.FromDays(365));
        options.ConfiguredPolicies[0].AutoDelete.Should().BeTrue();
        options.ConfiguredPolicies[0].Reason.Should().Be("GDPR storage limitation");
    }

    [Fact]
    public void AddPolicy_RetainForYears_SetsCorrectPeriod()
    {
        var options = new RetentionOptions();

        options.AddPolicy("audit-logs", policy =>
        {
            policy.RetainForYears(7);
        });

        options.ConfiguredPolicies[0].RetentionPeriod.Should().Be(TimeSpan.FromDays(7 * 365));
    }

    [Fact]
    public void AddPolicy_RetainFor_SetsCustomPeriod()
    {
        var options = new RetentionOptions();

        options.AddPolicy("temp-data", policy =>
        {
            policy.RetainFor(TimeSpan.FromHours(48));
        });

        options.ConfiguredPolicies[0].RetentionPeriod.Should().Be(TimeSpan.FromHours(48));
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

        options.ConfiguredPolicies[0].AutoDelete.Should().BeFalse();
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

        options.ConfiguredPolicies[0].LegalBasis.Should().Be("Tax law requirement");
    }

    [Fact]
    public void AddPolicy_Chaining_WorksCorrectly()
    {
        var options = new RetentionOptions();

        options
            .AddPolicy("data-1", p => p.RetainForDays(30))
            .AddPolicy("data-2", p => p.RetainForDays(60));

        options.ConfiguredPolicies.Should().HaveCount(2);
    }

    [Fact]
    public void AddPolicy_NullDataCategory_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions();

        var act = () => options.AddPolicy(null!, _ => { });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPolicy_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions();

        var act = () => options.AddPolicy("test", null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
