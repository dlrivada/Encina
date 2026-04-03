using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Dapper.SqlServer;

/// <summary>
/// Contract tests for <see cref="ReadWriteRoutingPipelineBehavior{TRequest, TResponse}"/>
/// verifying that the pipeline behavior correctly routes requests to the appropriate database intent.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ReadWriteSeparation")]
public sealed class ReadWriteRoutingPipelineBehaviorContractTests
{
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>> _commandLogger;
    private readonly ILogger<ReadWriteRoutingPipelineBehavior<TestQuery, string>> _queryLogger;

    public ReadWriteRoutingPipelineBehaviorContractTests()
    {
        _commandLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();
        _queryLogger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestQuery, string>>>();
    }

    [Fact]
    public void ReadWriteRoutingPipelineBehavior_ImplementsIPipelineBehavior()
    {
        // Assert
        typeof(IPipelineBehavior<TestCommand, string>).IsAssignableFrom(
            typeof(ReadWriteRoutingPipelineBehavior<TestCommand, string>)).ShouldBeTrue(
            "ReadWriteRoutingPipelineBehavior must implement IPipelineBehavior");
    }

    [Fact]
    public void ReadWriteRoutingPipelineBehavior_IsSealed()
    {
        // Assert
        typeof(ReadWriteRoutingPipelineBehavior<TestCommand, string>).IsSealed.ShouldBeTrue(
            "ReadWriteRoutingPipelineBehavior should be sealed");
    }

    [Fact]
    public async Task Handle_CommandRequest_RoutesToWriteDatabase()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var request = new TestCommand("test-value");
        var context = CreateRequestContext();
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> next = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return new ValueTask<Either<EncinaError, string>>(
                Either<EncinaError, string>.Right("command-result"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Command should succeed");
        capturedIntent.ShouldBe(DatabaseIntent.Write, "Commands should route to Write database");
    }

    [Fact]
    public async Task Handle_QueryRequest_RoutesToReadDatabase()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery("test-filter");
        var context = CreateRequestContext();
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> next = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return new ValueTask<Either<EncinaError, string>>(
                Either<EncinaError, string>.Right("query-result"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Query should succeed");
        capturedIntent.ShouldBe(DatabaseIntent.Read, "Queries should route to Read database");
    }

    [Fact]
    public async Task Handle_ReturnsNextResult()
    {
        // Arrange
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var request = new TestCommand("value");
        var context = CreateRequestContext();
        var expectedResult = "expected-result";

        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(Either<EncinaError, string>.Right(expectedResult));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.Match(
            Right: val => val.ShouldBe(expectedResult),
            Left: err => throw new InvalidOperationException($"Unexpected error: {err.Message}"));
    }

    private static IRequestContext CreateRequestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");
        return context;
    }

    // Test types for the pipeline behavior
    public sealed record TestCommand(string Value) : ICommand<string>;
    public sealed record TestQuery(string Filter) : IQuery<string>;
}
