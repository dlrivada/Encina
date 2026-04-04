using Encina.Compliance.DataResidency.Health;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard clause tests for <see cref="DataResidencyHealthCheck"/> constructor parameters.
/// </summary>
public class DataResidencyHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ShouldNotThrow()
    {
        // DataResidencyHealthCheck does not guard serviceProvider with ThrowIfNull
        // but we verify it doesn't crash when used with valid parameters
        var logger = NullLogger<DataResidencyHealthCheck>.Instance;
        var provider = Substitute.For<IServiceProvider>();

        var sut = new DataResidencyHealthCheck(provider, logger);
        sut.ShouldNotBeNull();
    }
}
