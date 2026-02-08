using System.Data;

using Encina.Messaging.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.GuardTests.Database;

/// <summary>
/// Guard clause tests for <see cref="ConnectionWarmupHostedService"/>.
/// </summary>
public sealed class ConnectionWarmupHostedServiceGuardsTests
{
    [Fact]
    public void Constructor_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ConnectionWarmupHostedService(null!, 5, NullLogger<ConnectionWarmupHostedService>.Instance));
        ex.ParamName.ShouldBe("connectionFactory");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ConnectionWarmupHostedService(() => Substitute.For<IDbConnection>(), 5, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ZeroWarmUpCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ConnectionWarmupHostedService(
                () => Substitute.For<IDbConnection>(),
                0,
                NullLogger<ConnectionWarmupHostedService>.Instance));
    }

    [Fact]
    public void Constructor_NegativeWarmUpCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ConnectionWarmupHostedService(
                () => Substitute.For<IDbConnection>(),
                -1,
                NullLogger<ConnectionWarmupHostedService>.Instance));
    }
}
