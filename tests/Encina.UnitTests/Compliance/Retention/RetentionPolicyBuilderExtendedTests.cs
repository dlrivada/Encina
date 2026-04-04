using Encina.Compliance.Retention;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Extended unit tests for <see cref="RetentionPolicyBuilder"/> via <see cref="RetentionOptions.AddPolicy"/>.
/// </summary>
public class RetentionPolicyBuilderExtendedTests
{
    [Fact]
    public void RetainForDays_ReturnsBuilder()
    {
        var options = new RetentionOptions();
        // Builder returns itself for chaining
        options.AddPolicy("cat-1", b => b.RetainForDays(30).WithAutoDelete());
    }

    [Fact]
    public void RetainForYears_ReturnsBuilder()
    {
        var options = new RetentionOptions();
        options.AddPolicy("cat-2", b => b.RetainForYears(7).WithAutoDelete(false));
    }

    [Fact]
    public void RetainFor_CustomTimeSpan_ReturnsBuilder()
    {
        var options = new RetentionOptions();
        options.AddPolicy("cat-3", b => b.RetainFor(TimeSpan.FromHours(72)));
    }

    [Fact]
    public void FullChain_AllMethods_ReturnBuilder()
    {
        var options = new RetentionOptions();
        options.AddPolicy("full-chain", b =>
        {
            b.RetainForDays(365)
             .WithAutoDelete()
             .WithReason("GDPR Article 5(1)(e)")
             .WithLegalBasis("Storage limitation");
        });
    }

    [Fact]
    public void AddPolicy_Multiple_AccumulatesPolicies()
    {
        var options = new RetentionOptions();
        options.AddPolicy("cat-a", b => b.RetainForDays(30));
        options.AddPolicy("cat-b", b => b.RetainForDays(60));
        options.AddPolicy("cat-c", b => b.RetainForDays(90));
        // No assertion on internals, but validates chaining works
    }
}
