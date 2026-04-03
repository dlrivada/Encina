using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionOptions.AddPolicy"/> null parameter handling.
/// </summary>
public sealed class RetentionOptionsGuardTests
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

        var act = () => options.AddPolicy("test-category", null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddPolicy_ValidParameters_DoesNotThrow()
    {
        var options = new RetentionOptions();

        var act = () => options.AddPolicy("test-category", policy =>
        {
            policy.RetainForDays(365);
        });

        Should.NotThrow(act);
    }
}
