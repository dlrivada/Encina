using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class DefaultDataResidencyPolicyGuardTests
{
    private readonly IResidencyPolicyStore _policyStore = Substitute.For<IResidencyPolicyStore>();
    private readonly IOptions<DataResidencyOptions> _options;
    private readonly ILogger<DefaultDataResidencyPolicy> _logger = NullLogger<DefaultDataResidencyPolicy>.Instance;

    public DefaultDataResidencyPolicyGuardTests()
    {
        _options = Substitute.For<IOptions<DataResidencyOptions>>();
        _options.Value.Returns(new DataResidencyOptions());
    }

    #region Constructor Guards

    [Fact]
    public void Constructor_NullPolicyStore_ShouldThrow()
    {
        var act = () => new DefaultDataResidencyPolicy(null!, _options, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("policyStore");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var act = () => new DefaultDataResidencyPolicy(_policyStore, null!, _logger);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultDataResidencyPolicy(_policyStore, _options, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Method Guards

    [Fact]
    public void IsAllowedAsync_NullDataCategory_ShouldThrow()
    {
        var sut = new DefaultDataResidencyPolicy(_policyStore, _options, _logger);
        var act = () => sut.IsAllowedAsync(null!, RegionRegistry.DE).AsTask();
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void IsAllowedAsync_EmptyDataCategory_ShouldThrow()
    {
        var sut = new DefaultDataResidencyPolicy(_policyStore, _options, _logger);
        var act = () => sut.IsAllowedAsync("", RegionRegistry.DE).AsTask();
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void IsAllowedAsync_NullRegion_ShouldThrow()
    {
        var sut = new DefaultDataResidencyPolicy(_policyStore, _options, _logger);
        var act = () => sut.IsAllowedAsync("data", null!).AsTask();
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GetAllowedRegionsAsync_NullDataCategory_ShouldThrow()
    {
        var sut = new DefaultDataResidencyPolicy(_policyStore, _options, _logger);
        var act = () => sut.GetAllowedRegionsAsync(null!).AsTask();
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void GetAllowedRegionsAsync_EmptyDataCategory_ShouldThrow()
    {
        var sut = new DefaultDataResidencyPolicy(_policyStore, _options, _logger);
        var act = () => sut.GetAllowedRegionsAsync("").AsTask();
        Should.Throw<ArgumentException>(act);
    }

    #endregion
}
