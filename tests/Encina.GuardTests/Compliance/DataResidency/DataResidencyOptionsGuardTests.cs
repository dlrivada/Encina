using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="DataResidencyOptions"/> fluent configuration methods.
/// </summary>
public class DataResidencyOptionsGuardTests
{
    [Fact]
    public void AddPolicy_NullDataCategory_ShouldThrow()
    {
        var options = new DataResidencyOptions();
        var act = () => options.AddPolicy(null!, _ => { });
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void AddPolicy_NullConfigure_ShouldThrow()
    {
        var options = new DataResidencyOptions();
        var act = () => options.AddPolicy("data", null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }
}
