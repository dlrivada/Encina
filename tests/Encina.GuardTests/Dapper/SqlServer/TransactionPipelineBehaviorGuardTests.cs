using System.Data;
using global::Encina.Dapper.SqlServer;
using LanguageExt;

namespace Encina.GuardTests.Dapper.SqlServer;

/// <summary>
/// Guard clause tests for <see cref="global::Encina.Messaging.TransactionPipelineBehavior{TRequest, TResponse}"/>.
/// Dapper providers use the Messaging TransactionPipelineBehavior which works with IDbConnection.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class TransactionPipelineBehaviorGuardTests
{
    private sealed record TestRequest(string Data) : IRequest<string>;

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;

        // Act
        var act = () => new global::Encina.Messaging.TransactionPipelineBehavior<TestRequest, string>(connection);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new global::Encina.Messaging.TransactionPipelineBehavior<TestRequest, string>(connection);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<Either<global::Encina.EncinaError, string>>("result");

        // Act
        var act = () => behavior.Handle(null!, context, next, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);

        // Assert
        ex.ParamName.ShouldBe("request");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new global::Encina.Messaging.TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest("test");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<Either<global::Encina.EncinaError, string>>("result");

        // Act
        var act = () => behavior.Handle(request, null!, next, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);

        // Assert
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when next is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new global::Encina.Messaging.TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        // Act
        var act = () => behavior.Handle(request, context, null!, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);

        // Assert
        ex.ParamName.ShouldBe("nextStep");
    }
}
