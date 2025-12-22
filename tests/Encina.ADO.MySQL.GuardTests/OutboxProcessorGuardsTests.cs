using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Encina.ADO.MySQL.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.ADO.MySQL.GuardTests;

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
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions();

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, logger, options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
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
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;
        OutboxOptions options = null!;

        // Act & Assert
        var act = () => new OutboxProcessor(serviceProvider, logger, options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }
}
