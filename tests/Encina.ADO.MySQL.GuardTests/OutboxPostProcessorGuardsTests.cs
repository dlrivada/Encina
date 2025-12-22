using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Encina.ADO.MySQL.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.ADO.MySQL.GuardTests;

/// <summary>
/// Guard tests for <see cref="OutboxPostProcessor{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class OutboxPostProcessorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when outboxStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOutboxStore_ThrowsArgumentNullException()
    {
        // Arrange
        IOutboxStore outboxStore = null!;
        var logger = NullLogger<OutboxPostProcessor<TestRequest, string>>.Instance;

        // Act & Assert
        var act = () => new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("outboxStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var outboxStore = Substitute.For<IOutboxStore>();
        ILogger<OutboxPostProcessor<TestRequest, string>> logger = null!;

        // Act & Assert
        var act = () => new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Test request for guard tests.
    /// </summary>
    private sealed record TestRequest : IRequest<string>;
}
