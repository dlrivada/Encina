using System.Data;
using LanguageExt;
using Encina.Dapper.SqlServer;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/>.
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
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(connection);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connection");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<Either<MediatorError, string>>("result");

        // Act
        var act = async () => await behavior.Handle(null!, context, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest("test");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult<Either<MediatorError, string>>("result");

        // Act
        var act = async () => await behavior.Handle(request, null!, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    /// <summary>
    /// Tests that Handle throws ArgumentNullException when next is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);
        var request = new TestRequest("test");
        var context = Substitute.For<IRequestContext>();

        // Act
        var act = async () => await behavior.Handle(request, context, null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("next");
    }
}
