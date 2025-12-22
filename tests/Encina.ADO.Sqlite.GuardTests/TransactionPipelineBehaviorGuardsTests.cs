using System.Data;
using LanguageExt;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Sqlite.GuardTests;

/// <summary>
/// Guard tests for <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public class TransactionPipelineBehaviorGuardsTests
{
    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connection is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        IDbConnection connection = null!;

        // Act & Assert
        var act = () => new TransactionPipelineBehavior<TestRequest, string>(connection);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        TestRequest request = null!;
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<MediatorError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when context is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        var request = new TestRequest();
        IRequestContext context = null!;
        RequestHandlerCallback<string> nextStep = () => ValueTask.FromResult<Either<MediatorError, string>>("test");

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, nextStep, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    /// <summary>
    /// Verifies that Handle throws ArgumentNullException when next is null.
    /// </summary>
    [Fact]
    public async Task Handle_NullNext_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        var behavior = new TransactionPipelineBehavior<TestRequest, string>(connection);

        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = null!;

        // Act & Assert
        var act = async () => await behavior.Handle(request, context, next, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("next");
    }

    /// <summary>
    /// Test request for testing.
    /// </summary>
    private sealed record TestRequest : IRequest<string>;
}
