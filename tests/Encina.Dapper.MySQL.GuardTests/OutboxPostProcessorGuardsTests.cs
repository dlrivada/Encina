using Microsoft.Extensions.Logging;
using Encina.Dapper.MySQL.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.Dapper.MySQL.GuardTests;

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
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, TestResponse>>>();

        // Act & Assert
        var act = () => new OutboxPostProcessor<TestRequest, TestResponse>(outboxStore, logger);
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
        ILogger<OutboxPostProcessor<TestRequest, TestResponse>> logger = null!;

        // Act & Assert
        var act = () => new OutboxPostProcessor<TestRequest, TestResponse>(outboxStore, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
}
