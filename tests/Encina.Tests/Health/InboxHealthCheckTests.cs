using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class InboxHealthCheckTests
{
    private readonly IInboxStore _store;
    private readonly InboxHealthCheck _healthCheck;

    public InboxHealthCheckTests()
    {
        _store = Substitute.For<IInboxStore>();
        _healthCheck = new InboxHealthCheck(_store);
    }

    [Fact]
    public void Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new InboxHealthCheck(null!));
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Assert
        _healthCheck.Name.ShouldBe("encina-inbox");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Assert
        _healthCheck.Tags.ShouldContain("ready");
        _healthCheck.Tags.ShouldContain("database");
        _healthCheck.Tags.ShouldContain("messaging");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreAccessible_ReturnsHealthy()
    {
        // Arrange
        _store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesDataInResult()
    {
        // Arrange
        _store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldContainKey("expired_sample");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStoreThrows_ReturnsUnhealthy()
    {
        // Arrange
        _store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Connection failed"));

        // Act
        var result = await _healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
        result.Description!.ShouldContain("Connection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _store.GetExpiredMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _healthCheck.CheckHealthAsync(cts.Token);

        // Assert
        await _store.Received(1).GetExpiredMessagesAsync(1, cts.Token);
    }
}
