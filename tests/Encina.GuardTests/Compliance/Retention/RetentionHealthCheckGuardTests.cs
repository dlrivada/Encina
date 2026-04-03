using Encina.Compliance.Retention.Health;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionHealthCheck"/> constructor null parameter handling.
/// </summary>
public sealed class RetentionHealthCheckGuardTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ILogger<RetentionHealthCheck> _logger = NullLogger<RetentionHealthCheck>.Instance;

    [Fact]
    public void Constructor_NullServiceProvider_DoesNotThrow()
    {
        // RetentionHealthCheck does not guard serviceProvider parameter
        var act = () => new RetentionHealthCheck(null!, _logger);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // RetentionHealthCheck does not guard logger parameter
        var act = () => new RetentionHealthCheck(_serviceProvider, null!);

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var act = () => new RetentionHealthCheck(_serviceProvider, _logger);

        Should.NotThrow(act);
    }
}
