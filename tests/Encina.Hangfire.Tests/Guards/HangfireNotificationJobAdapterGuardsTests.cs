using Encina.Hangfire;
using Microsoft.Extensions.Logging;

namespace Encina.Hangfire.Tests.Guards;

/// <summary>
/// Guard clause tests for HangfireNotificationJobAdapter.
/// Verifies null parameter handling and defensive programming.
/// </summary>
public class HangfireNotificationJobAdapterGuardsTests
{
    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator mediator = null!;
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<HangfireTestNotification>>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HangfireNotificationJobAdapter<HangfireTestNotification>(mediator, logger));

        exception.ParamName.Should().Be("mediator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        ILogger<HangfireNotificationJobAdapter<HangfireTestNotification>> logger = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HangfireNotificationJobAdapter<HangfireTestNotification>(mediator, logger));

        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<HangfireTestNotification>>>();
        var adapter = new HangfireNotificationJobAdapter<HangfireTestNotification>(mediator, logger);
        HangfireTestNotification notification = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            adapter.PublishAsync(notification));

        exception.ParamName.Should().Be("notification");
    }

}

// Test types
public sealed record HangfireTestNotification(string Message) : INotification;
