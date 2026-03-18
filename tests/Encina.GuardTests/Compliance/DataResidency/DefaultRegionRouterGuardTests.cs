using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class DefaultRegionRouterGuardTests
{
    private readonly IResidencyPolicyService _policyService = Substitute.For<IResidencyPolicyService>();
    private readonly IRegionContextProvider _contextProvider = Substitute.For<IRegionContextProvider>();
    private readonly ILogger<DefaultRegionRouter> _logger = NullLogger<DefaultRegionRouter>.Instance;

    [Fact]
    public void Constructor_NullPolicy_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(null!, _contextProvider, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("residencyPolicyService");
    }

    [Fact]
    public void Constructor_NullContextProvider_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(_policyService, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("regionContextProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultRegionRouter(_policyService, _contextProvider, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
