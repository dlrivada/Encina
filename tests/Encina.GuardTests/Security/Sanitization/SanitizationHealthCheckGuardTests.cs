using Encina.Security.Sanitization.Health;
using FluentAssertions;

namespace Encina.GuardTests.Security.Sanitization;

/// <summary>
/// Guard tests for <see cref="SanitizationHealthCheck"/> to verify null parameter handling.
/// </summary>
public sealed class SanitizationHealthCheckGuardTests
{
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new SanitizationHealthCheck(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }
}
