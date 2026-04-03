using Encina.Dapper.PostgreSQL.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.ContractTests.Dapper.PostgreSQL;

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
        typeof(IPipelineBehavior<TestCommand, string>).IsAssignableFrom(
            typeof(ReadWriteRoutingPipelineBehavior<TestCommand, string>)).ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CommandRequest_RoutesToWriteDatabase()
    {
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(_commandLogger);
        var request = new TestCommand("test-value");
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-id");
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> next = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return new ValueTask<Either<EncinaError, string>>(
                Either<EncinaError, string>.Right("command-result"));
        };

        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        capturedIntent.ShouldBe(DatabaseIntent.Write);
    }

    [Fact]
    public async Task Handle_QueryRequest_RoutesToReadDatabase()
    {
        var behavior = new ReadWriteRoutingPipelineBehavior<TestQuery, string>(_queryLogger);
        var request = new TestQuery("test-filter");
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-id");
        DatabaseIntent? capturedIntent = null;

        RequestHandlerCallback<string> next = () =>
        {
            capturedIntent = DatabaseRoutingContext.CurrentIntent;
            return new ValueTask<Either<EncinaError, string>>(
                Either<EncinaError, string>.Right("query-result"));
        };

        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        capturedIntent.ShouldBe(DatabaseIntent.Read);
    }

    public sealed record TestCommand(string Value) : ICommand<string>;
    public sealed record TestQuery(string Filter) : IQuery<string>;
}
