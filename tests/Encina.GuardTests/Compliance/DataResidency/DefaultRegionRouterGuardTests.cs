using Encina.Compliance.DataResidency;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class DefaultRegionRouterGuardTests
{
    private readonly IDataResidencyPolicy _policy = Substitute.For<IDataResidencyPolicy>();
    private readonly IRegionContextProvider _contextProvider = Substitute.For<IRegionContextProvider>();
    private readonly ILogger<DefaultRegionRouter> _logger = NullLogger<DefaultRegionRouter>.Instance;

    [Fact]
    public void Constructor_NullPolicy_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(null!, _contextProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("residencyPolicy");
    }

    [Fact]
    public void Constructor_NullContextProvider_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(_policy, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("regionContextProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(_policy, _contextProvider, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
