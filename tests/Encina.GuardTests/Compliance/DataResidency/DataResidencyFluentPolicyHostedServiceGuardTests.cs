using Encina.Compliance.DataResidency;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="DataResidencyFluentPolicyHostedService"/> verifying constructor null checks.
/// </summary>
public class DataResidencyFluentPolicyHostedServiceGuardTests
{
    private readonly DataResidencyFluentPolicyDescriptor _descriptor = new([]);
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    [Fact]
    public void Constructor_NullDescriptor_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyFluentPolicyHostedService(
            null!, _scopeFactory,
            NullLogger<DataResidencyFluentPolicyHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("descriptor");
    }

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyFluentPolicyHostedService(
            _descriptor, null!,
            NullLogger<DataResidencyFluentPolicyHostedService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DataResidencyFluentPolicyHostedService(
            _descriptor, _scopeFactory,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
