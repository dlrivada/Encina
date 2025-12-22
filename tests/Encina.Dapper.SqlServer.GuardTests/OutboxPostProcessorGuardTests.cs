using LanguageExt;
using Microsoft.Extensions.Logging;
using Encina.Dapper.SqlServer.Outbox;
using Encina.Messaging.Outbox;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="OutboxPostProcessor{TRequest, TResponse}"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class OutboxPostProcessorGuardTests
{
    private sealed record TestRequest(string Data) : IRequest<string>;

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when outboxStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOutboxStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        IOutboxStore outboxStore = null!;
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();

        // Act
        var act = () => new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("outboxStore");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var outboxStore = Substitute.For<IOutboxStore>();
        ILogger<OutboxPostProcessor<TestRequest, string>> logger = null!;

        // Act
        var act = () => new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Tests that Process throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Process_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var outboxStore = Substitute.For<IOutboxStore>();
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();
        var processor = new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);
        var context = Substitute.For<IRequestContext>();
        Either<MediatorError, string> result = Right<MediatorError, string>("success");

        // Act
        var act = async () => await processor.Process(null!, context, result, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    /// <summary>
    /// Tests that Process throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Process_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var outboxStore = Substitute.For<IOutboxStore>();
        var logger = Substitute.For<ILogger<OutboxPostProcessor<TestRequest, string>>>();
        var processor = new OutboxPostProcessor<TestRequest, string>(outboxStore, logger);
        var request = new TestRequest("test");
        Either<MediatorError, string> result = Right<MediatorError, string>("success");

        // Act
        var act = async () => await processor.Process(request, null!, result, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }
}
