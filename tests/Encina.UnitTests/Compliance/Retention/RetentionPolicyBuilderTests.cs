using Encina.Compliance.Retention;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionPolicyBuilder"/> fluent API.
/// </summary>
public sealed class RetentionPolicyBuilderTests
{
    [Fact]
    public void RetainForDays_SetsCorrectPeriod()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", p => p.RetainForDays(90));

        options.ConfiguredPolicies.Should().HaveCount(1);
        options.ConfiguredPolicies[0].RetentionPeriod.Should().Be(TimeSpan.FromDays(90));
    }

    [Fact]
    public void RetainForYears_SetsCorrectPeriod()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", p => p.RetainForYears(7));

        options.ConfiguredPolicies[0].RetentionPeriod.Should().Be(TimeSpan.FromDays(7 * 365));
    }

    [Fact]
    public void RetainFor_SetsCustomPeriod()
    {
        var options = new RetentionOptions();
        var customPeriod = TimeSpan.FromHours(48);

        options.AddPolicy("test", p => p.RetainFor(customPeriod));

        options.ConfiguredPolicies[0].RetentionPeriod.Should().Be(customPeriod);
    }

    [Fact]
    public void WithAutoDelete_DefaultTrue_SetsAutoDelete()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithAutoDelete();
        });

        options.ConfiguredPolicies[0].AutoDelete.Should().BeTrue();
    }

    [Fact]
    public void WithAutoDelete_ExplicitFalse_ClearsAutoDelete()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithAutoDelete(false);
        });

        options.ConfiguredPolicies[0].AutoDelete.Should().BeFalse();
    }

    [Fact]
    public void WithReason_SetsReason()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithReason("GDPR compliance");
        });

        options.ConfiguredPolicies[0].Reason.Should().Be("GDPR compliance");
    }

    [Fact]
    public void WithLegalBasis_SetsLegalBasis()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithLegalBasis("Article 5(1)(e)");
        });

        options.ConfiguredPolicies[0].LegalBasis.Should().Be("Article 5(1)(e)");
    }

    [Fact]
    public void FluentChaining_AllMethods_SetsAllProperties()
    {
        var options = new RetentionOptions();

        options.AddPolicy("financial", p =>
        {
            p.RetainForYears(10);
            p.WithAutoDelete();
            p.WithReason("Tax law requirement");
            p.WithLegalBasis("German AO section 147");
        });

        var descriptor = options.ConfiguredPolicies[0];
        descriptor.DataCategory.Should().Be("financial");
        descriptor.RetentionPeriod.Should().Be(TimeSpan.FromDays(3650));
        descriptor.AutoDelete.Should().BeTrue();
        descriptor.Reason.Should().Be("Tax law requirement");
        descriptor.LegalBasis.Should().Be("German AO section 147");
    }

    [Fact]
    public void AddPolicy_Chaining_ReturnsOptions()
    {
        var options = new RetentionOptions();

        var result = options
            .AddPolicy("cat1", p => p.RetainForDays(30))
            .AddPolicy("cat2", p => p.RetainForDays(60));

        result.Should().BeSameAs(options);
        options.ConfiguredPolicies.Should().HaveCount(2);
    }

    [Fact]
    public void DefaultValues_WhenNoMethodsCalled_ReturnsDefaults()
    {
        var options = new RetentionOptions();

        options.AddPolicy("test", _ => { });

        var descriptor = options.ConfiguredPolicies[0];
        descriptor.RetentionPeriod.Should().Be(TimeSpan.FromDays(365));
        descriptor.AutoDelete.Should().BeTrue();
        descriptor.Reason.Should().BeNull();
        descriptor.LegalBasis.Should().BeNull();
    }
}
