using Encina.Compliance.DataResidency;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class DefaultAdequacyDecisionProviderGuardTests
{
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultAdequacyDecisionProvider> _logger = NullLogger<DefaultAdequacyDecisionProvider>.Instance;

    public DefaultAdequacyDecisionProviderGuardTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var act = () => new DefaultAdequacyDecisionProvider(null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultAdequacyDecisionProvider(_options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void HasAdequacy_NullRegion_ShouldThrow()
    {
        var sut = new DefaultAdequacyDecisionProvider(_options, _logger);
        Action act = () => sut.HasAdequacy(null!);
        Should.Throw<ArgumentNullException>(act);
    }
}
