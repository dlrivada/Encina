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
    public void Constructor_WithNullEncina_ThrowsArgumentNullException()
    {
        // Arrange
        IEncina encina = null!;
        var logger = Substitute.For<ILogger<QuartzNotificationJob<QuartzTestNotification>>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzNotificationJob<QuartzTestNotification>(encina, logger));

        exception.ParamName.ShouldBe("encina");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        ILogger<QuartzNotificationJob<QuartzTestNotification>> logger = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzNotificationJob<QuartzTestNotification>(encina, logger));

        exception.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task Execute_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<QuartzTestNotification>>>();
        var job = new QuartzNotificationJob<QuartzTestNotification>(encina, logger);
        IJobExecutionContext context = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            job.Execute(context));

        exception.ParamName.ShouldBe("context");
    }

}

// Test types
public sealed record QuartzTestNotification(string Message) : INotification;
