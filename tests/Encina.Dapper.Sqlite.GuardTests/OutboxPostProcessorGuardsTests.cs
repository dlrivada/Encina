using Microsoft.Extensions.Logging;
using Encina.Dapper.Sqlite.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.Dapper.Sqlite.GuardTests;

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
        var logger = Substitute.For<ILogger<OutboxPostProcessor<OutboxPostProcessorTestRequest, OutboxPostProcessorTestResponse>>>();

        // Act & Assert
        var act = () => new OutboxPostProcessor<OutboxPostProcessorTestRequest, OutboxPostProcessorTestResponse>(outboxStore, logger);
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
        ILogger<OutboxPostProcessor<OutboxPostProcessorTestRequest, OutboxPostProcessorTestResponse>> logger = null!;

        // Act & Assert
        var act = () => new OutboxPostProcessor<OutboxPostProcessorTestRequest, OutboxPostProcessorTestResponse>(outboxStore, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}

/// <summary>
/// Test request for OutboxPostProcessor guard tests.
/// </summary>
public sealed record OutboxPostProcessorTestRequest : IRequest<OutboxPostProcessorTestResponse>;

/// <summary>
/// Test response for OutboxPostProcessor guard tests.
/// </summary>
public sealed record OutboxPostProcessorTestResponse;
