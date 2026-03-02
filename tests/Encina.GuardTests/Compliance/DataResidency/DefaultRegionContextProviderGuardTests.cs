using Encina.Compliance.DataResidency;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class DefaultRegionContextProviderGuardTests
{
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultRegionContextProvider> _logger = NullLogger<DefaultRegionContextProvider>.Instance;

    public DefaultRegionContextProviderGuardTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var act = () => new DefaultRegionContextProvider(null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultRegionContextProvider(_options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
