using Encina.Dapper.MySQL.ReadWriteSeparation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.ReadWriteSeparation;

/// <summary>
/// Guard tests for <see cref="ReadWriteRoutingPipelineBehavior{TRequest, TResponse}"/>
/// to verify null parameter handling.
/// </summary>
public class ReadWriteRoutingPipelineBehaviorGuardTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ReadWriteRoutingPipelineBehavior<TestCommand, string>(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_ValidLogger_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();

        // Act & Assert
        Should.NotThrow(() => new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger));
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Should not reach handler");

        // Act & Assert
        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger);
        var request = new TestCommand("test");
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Should not reach handler");

        // Act & Assert
        var act = async () => await behavior.Handle(request, null!, next, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>>();
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger);
        var request = new TestCommand("test");
        var context = Substitute.For<IRequestContext>();

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, null!, CancellationToken.None);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("nextStep");
    }

    public sealed record TestCommand(string Value) : ICommand<string>;
}
