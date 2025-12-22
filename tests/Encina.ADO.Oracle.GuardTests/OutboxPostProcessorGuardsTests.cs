using Microsoft.Extensions.Logging;
using Encina.ADO.Oracle.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.ADO.Oracle.GuardTests;

/// <summary>
/// Guard tests for <see cref="OutboxPostProcessor{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class OutboxPostProcessorGuardsTests
{
    internal sealed class TestRequest : IRequest<string>;

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when outboxStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOutboxStore_ThrowsArgumentNullException()
    {
        // Arrange
        IOutboxStore outboxStore = null!;
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();

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
    /// Verifies that Process throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Process_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var outboxStore = Substitute.For<IOutboxStore>();
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();
        var processor = new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        var result = LanguageExt.Prelude.Right<MediatorError, string>("success");

        // Act & Assert
        var act = async () => await processor.Process(request, context, result, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Verifies that Process throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Process_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var outboxStore = Substitute.For<IOutboxStore>();
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();
        var processor = new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);

        var request = new TestRequest();
        IRequestContext context = null!;
        var result = LanguageExt.Prelude.Right<MediatorError, string>("success");

        // Act & Assert
        var act = async () => await processor.Process(request, context, result, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }
}
