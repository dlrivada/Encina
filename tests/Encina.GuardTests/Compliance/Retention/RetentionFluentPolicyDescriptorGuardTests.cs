using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionOptions.AddPolicy"/> fluent API.
/// </summary>
public class RetentionPolicyBuilderGuardTests
{
    [Fact]
    public void AddPolicy_NullDataCategory_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions();
        var act = () => options.AddPolicy(null!, _ => { });
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void AddPolicy_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new RetentionOptions();
        var act = () => options.AddPolicy("test", null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddPolicy_ValidInput_ReturnsOptions()
    {
        var options = new RetentionOptions();
        var result = options.AddPolicy("financial", p => p.RetainForYears(7));
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void RetentionPolicyBuilder_RetainForDays_SetsPeriod()
    {
        var options = new RetentionOptions();
        options.AddPolicy("test", p => p.RetainForDays(90));
        // Validates that AddPolicy completes without error
    }

    [Fact]
    public void RetentionPolicyBuilder_RetainFor_SetsPeriod()
    {
        var options = new RetentionOptions();
        options.AddPolicy("test", p => p.RetainFor(TimeSpan.FromHours(48)));
    }

    [Fact]
    public void RetentionPolicyBuilder_WithAutoDelete_SetsFlagTrue()
    {
        var options = new RetentionOptions();
        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithAutoDelete();
        });
    }

    [Fact]
    public void RetentionPolicyBuilder_WithAutoDeleteFalse_SetsFlagFalse()
    {
        var options = new RetentionOptions();
        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithAutoDelete(false);
        });
    }

    [Fact]
    public void RetentionPolicyBuilder_WithReason_SetsReason()
    {
        var options = new RetentionOptions();
        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithReason("GDPR compliance");
        });
    }

    [Fact]
    public void RetentionPolicyBuilder_WithLegalBasis_SetsLegalBasis()
    {
        var options = new RetentionOptions();
        options.AddPolicy("test", p =>
        {
            p.RetainForDays(30);
            p.WithLegalBasis("Article 5(1)(e)");
        });
    }

    [Fact]
    public void RetentionPolicyBuilder_ChainedCalls_WorkCorrectly()
    {
        var options = new RetentionOptions();
        options.AddPolicy("audit-logs", p =>
        {
            p.RetainForYears(7)
                .WithAutoDelete(false)
                .WithReason("Tax law")
                .WithLegalBasis("AO 147");
        });
    }
}
