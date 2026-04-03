using Encina.Security.ABAC;
using Encina.Security.ABAC.Health;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Health;

/// <summary>
/// Guard clause tests for <see cref="ABACHealthCheck"/>.
/// </summary>
public class ABACHealthCheckGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullPap_ThrowsArgumentNullException()
    {
        var act = () => new ABACHealthCheck(null!, Substitute.For<IServiceProvider>());
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("pap");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new ABACHealthCheck(Substitute.For<IPolicyAdministrationPoint>(), null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region Constants

    [Fact]
    public void DefaultName_IsEncinaAbac()
    {
        ABACHealthCheck.DefaultName.ShouldBe("encina-abac");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        var tags = ABACHealthCheck.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("security");
        tags.ShouldContain("abac");
        tags.ShouldContain("ready");
    }

    #endregion
}
