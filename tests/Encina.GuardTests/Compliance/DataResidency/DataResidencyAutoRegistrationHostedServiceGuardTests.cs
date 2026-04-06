using Encina.Compliance.DataResidency;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="DataResidencyAutoRegistrationHostedService"/> verifying constructor null checks.
/// </summary>
public class DataResidencyAutoRegistrationHostedServiceGuardTests
{
    private readonly DataResidencyAutoRegistrationDescriptor _descriptor = new([]);
    private readonly IOptions<DataResidencyOptions> _options = Options.Create(new DataResidencyOptions());
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    [Fact]
    public void Constructor_NullDescriptor_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyAutoRegistrationHostedService(
            null!, _options, _scopeFactory,
            NullLogger<DataResidencyAutoRegistrationHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("descriptor");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyAutoRegistrationHostedService(
            _descriptor, null!, _scopeFactory,
            NullLogger<DataResidencyAutoRegistrationHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyAutoRegistrationHostedService(
            _descriptor, _options, null!,
            NullLogger<DataResidencyAutoRegistrationHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyAutoRegistrationHostedService(
            _descriptor, _options, _scopeFactory,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
