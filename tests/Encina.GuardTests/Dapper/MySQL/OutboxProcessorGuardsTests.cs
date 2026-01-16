using System.Data;
using Microsoft.Extensions.Logging;
using Encina.Dapper.MySQL;
using Encina.Dapper.MySQL.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.GuardTests.Dapper.MySQL;

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
        var logger = Substitute.For<ILogger<OutboxProcessor>>();
        var options = new OutboxOptions();

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, logger, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        ILogger<OutboxProcessor> logger = null!;
        var options = new OutboxOptions();

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, logger, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<OutboxProcessor>>();
        OutboxOptions options = null!;

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, logger, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }
}
