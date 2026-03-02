using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class DefaultCrossBorderTransferValidatorGuardTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultCrossBorderTransferValidator> _logger = NullLogger<DefaultCrossBorderTransferValidator>.Instance;

    public DefaultCrossBorderTransferValidatorGuardTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
    }

    #region Constructor Guards

    [Fact]
    public void Constructor_NullAdequacyProvider_ShouldThrow()
    {
        var act = () => new DefaultCrossBorderTransferValidator(null!, _options, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("adequacyProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var act = () => new DefaultCrossBorderTransferValidator(_adequacyProvider, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards

    [Fact]
    public void ValidateTransferAsync_NullSource_ShouldThrow()
    {
        var sut = new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, _logger);
        var act = () => sut.ValidateTransferAsync(null!, RegionRegistry.FR, "data").AsTask();
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("source");
    }

    [Fact]
    public void ValidateTransferAsync_NullDestination_ShouldThrow()
    {
        var sut = new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, _logger);
        var act = () => sut.ValidateTransferAsync(RegionRegistry.DE, null!, "data").AsTask();
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("destination");
    }

    [Fact]
    public void ValidateTransferAsync_NullDataCategory_ShouldThrow()
    {
        var sut = new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, _logger);
        var act = () => sut.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.FR, null!).AsTask();
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void ValidateTransferAsync_EmptyDataCategory_ShouldThrow()
    {
        var sut = new DefaultCrossBorderTransferValidator(_adequacyProvider, _options, _logger);
        var act = () => sut.ValidateTransferAsync(RegionRegistry.DE, RegionRegistry.FR, "").AsTask();
        Should.Throw<ArgumentException>(act);
    }

    #endregion
}
