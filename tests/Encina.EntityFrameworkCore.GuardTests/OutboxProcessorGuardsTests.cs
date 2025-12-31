using Microsoft.Extensions.Logging;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Outbox;
using NSubstitute;
using Shouldly;

namespace Encina.EntityFrameworkCore.GuardTests;

/// <summary>
/// Guard tests for <see cref="OutboxProcessor"/> to verify null parameter handling.
/// </summary>
public class OutboxProcessorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var options = new OutboxOptions();
        var logger = Substitute.For<ILogger<OutboxProcessor>>();

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        OutboxOptions options = null!;
        var logger = Substitute.For<ILogger<OutboxProcessor>>();

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new OutboxOptions();
        ILogger<OutboxProcessor> logger = null!;

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }
}
