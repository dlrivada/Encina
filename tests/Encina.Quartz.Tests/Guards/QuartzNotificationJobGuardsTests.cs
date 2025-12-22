using Encina.Quartz;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz.Tests.Guards;

/// <summary>
/// Guard clause tests for QuartzNotificationJob.
/// Verifies null parameter handling and defensive programming.
/// </summary>
public class QuartzNotificationJobGuardsTests
{
    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Arrange
        IMediator mediator = null!;
        var logger = Substitute.For<ILogger<QuartzNotificationJob<QuartzTestNotification>>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzNotificationJob<QuartzTestNotification>(mediator, logger));

        exception.ParamName.Should().Be("mediator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        ILogger<QuartzNotificationJob<QuartzTestNotification>> logger = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzNotificationJob<QuartzTestNotification>(mediator, logger));

        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task Execute_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<QuartzTestNotification>>>();
        var job = new QuartzNotificationJob<QuartzTestNotification>(mediator, logger);
        IJobExecutionContext context = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            job.Execute(context));

        exception.ParamName.Should().Be("context");
    }

}

// Test types
public sealed record QuartzTestNotification(string Message) : INotification;
